/*===========================================================
    Author: Ruan Cardoso
    -
    Country: Brazil(Brasil)
    -
    Contact: cardoso.ruan050322@gmail.com
    -
    Support: neutron050322@gmail.com
    -
    Unity Minor Version: 2021.3 LTS
    -
    License: Open Source (MIT)
    ===========================================================*/

using MessagePack;
using MessagePack.Resolvers;
using MessagePack.Unity;
using MessagePack.Unity.Extension;
using Neutron.Resolvers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using static Dapper.SqlMapper;

namespace Neutron.Core
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-0x64)]
    [RequireComponent(typeof(ActionDispatcher))]
    public class NeutronNetwork : MonoBehaviour
    {
        private const byte SETTINGS_SIZE = 50;

        #region Framerate
        [SerializeField][Range(0, 10)] private int fpsUpdateRate = 4;
        public static float framerate = 0f;
        public static float cpuMs = 0f;
        private static int frameCount = 0;
        private static float deltaTime = 0f;
        #endregion

        private static readonly Dictionary<(ushort, ushort, bool, ObjectType), NeutronIdentity> identities = new(); // [identity id, playerId, isServer bool, objectType id]
        private static readonly Dictionary<int, Action<ByteStream, bool>> handlers = new();
        private static readonly UdpServer udpServer = new();
        private static readonly UdpClient udpClient = new();

        #region Events
        public static event Action<bool, IPEndPoint, ByteStream> OnConnected;
        #endregion

        #region Properties
#if UNITY_EDITOR
        internal static Scene ServerScene { get; private set; }
#endif
        internal static int Port { get; private set; }
        internal static ushort ServerId { get; } = ushort.MaxValue;
        internal static NeutronNetwork Instance { get; private set; }
        public static int Id => udpClient.Id;
        public static bool IsConnected => udpClient.IsConnected;
        public static ActionDispatcher Dispatcher => Instance.dispatcher;
        #endregion

        #region Fields
        [SerializeField][Range(byte.MaxValue, ushort.MaxValue)] internal int windowSize = byte.MaxValue;
        [SerializeField][Range(1, 1500)] internal int udpPacketSize = 64;
        [SerializeField]
#if !UNITY_SERVER
        [HideInInspector]
#endif
        private bool consoleInput;
        [SerializeField] private bool dontDestroy = false;
        [SerializeField] private bool loadNextScene = true;
        [SerializeField] private bool agressiveRelay = false;
        [SerializeField] private bool multiThreaded = false;
        #endregion

        internal static double timeAsDouble;

        [SerializeField][HideInInspector] private LocalSettings[] allPlatformSettings = new LocalSettings[SETTINGS_SIZE];
        [SerializeField] internal LocalSettings platformSettings;

        private ActionDispatcher dispatcher;
        private readonly CancellationTokenSource tokenSource = new();

        public static IFormatterResolver Formatter { get; private set; }
        public static MessagePackSerializerOptions AddResolver(IFormatterResolver resolver = null, [CallerMemberName] string _ = "")
        {
            if (_ != "Awake")
                Logger.PrintError($"AddResolver must be called from Awake");
            else
            {
                Formatter = resolver == null
                    ? (resolver = CompositeResolver.Create(NeutronRuntimeResolver.Instance, UnityBlitWithPrimitiveArrayResolver.Instance, UnityResolver.Instance, StandardResolver.Instance))
                    : (resolver = CompositeResolver.Create(resolver, Formatter));
                return MessagePackSerializer.DefaultOptions = MessagePackSerializerOptions.Standard.WithResolver(resolver);
            }
            return MessagePackSerializer.DefaultOptions;
        }

        private void Awake()
        {
            Instance = this;
            dispatcher = GetComponent<ActionDispatcher>();
            AddResolver(null);
            if (dontDestroy) DontDestroyOnLoad(this);
            ByteStream.streams = new();

            #region Registers
            OnConnected += NeutronNetwork_OnConnected;
            #endregion

            #region Framerate
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = platformSettings.maxFramerate;
            #endregion

            LocalSettings.Host lHost = platformSettings.hosts[0];
            Port = lHost.port;
            var remoteEndPoint = new UdpEndPoint(IPAddress.Any, Port);
#if UNITY_SERVER || UNITY_EDITOR
            udpServer.Bind(remoteEndPoint);
            udpServer.CreateServerPlayer(ServerId);
#endif
#if !UNITY_SERVER || UNITY_EDITOR
            udpClient.Bind(new UdpEndPoint(IPAddress.Any, Helper.GetFreePort()));
            udpClient.Connect(new UdpEndPoint(IPAddress.Parse(lHost.host), remoteEndPoint.GetPort()));
#endif
#if UNITY_EDITOR
            ServerScene = SceneManager.CreateScene("Server", new CreateSceneParameters(LocalPhysicsMode.None));
#endif
        }

        private void NeutronNetwork_OnConnected(bool isServer, IPEndPoint endPoint, ByteStream parameters)
        {
            if (loadNextScene)
            {
                int currentIndex = SceneManager.GetActiveScene().buildIndex;
                int nextIndex = currentIndex + 1;
#if UNITY_EDITOR
                if (!isServer)
                    SceneManager.LoadScene(nextIndex, LoadSceneMode.Additive);
#else
                SceneManager.LoadScene(nextIndex);
#endif
            }
        }

        private void Start() => Invoke(nameof(Main), 0.3f);
        private void Main()
        {
#if UNITY_SERVER && !UNITY_EDITOR
            Console.Clear();
            if (consoleInput) NeutronConsole.Initialize(tokenSource.Token, this);
#endif
            if (!GarbageCollector.isIncremental) Logger.PrintWarning("Tip: Enable \"Incremental GC\" for maximum performance!");
#if !NETSTANDARD2_1
            Logger.PrintWarning("Tip: Change API Mode to \".NET Standard 2.1\" for maximum performance!");
#endif
#if !ENABLE_IL2CPP && !UNITY_EDITOR
            Logger.PrintWarning("Tip: Change API Mode to \"IL2CPP\" for maximum performance!");
#endif
        }

        private void Update()
        {
            deltaTime += Time.unscaledDeltaTime;
            frameCount++;
            if (deltaTime > 1f / fpsUpdateRate)
            {
                framerate = frameCount / deltaTime;
                cpuMs = deltaTime / frameCount * 1000f;
                deltaTime = 0f;
                frameCount = 0;
            }
            timeAsDouble = Time.timeAsDouble;
        }

#if UNITY_EDITOR
        [ContextMenu("Request Script Compilation")]
        private void RequestScriptCompilation()
        {
            for (int i = 0; i < allPlatformSettings.Length; i++)
            {
                if (allPlatformSettings[i] != null)
                    allPlatformSettings[i].enabled = false;
                else continue;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.None);
            else Logger.PrintError("RequestScriptCompilation -> Failed");
        }

        private void Reset() => OnValidate();
        private void OnValidate()
        {
#if UNITY_SERVER
            BuildTarget buildTarget = BuildTarget.LinuxHeadlessSimulation;
#else
            BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
#endif
            int index = (int)buildTarget;
            allPlatformSettings ??= new LocalSettings[SETTINGS_SIZE];
            if (allPlatformSettings.Length == SETTINGS_SIZE)
            {
                if (allPlatformSettings.InBounds(index))
                {
                    if (allPlatformSettings[index] != null)
                    {
                        if (!allPlatformSettings[index].enabled)
                        {
                            string name = buildTarget.ToString();
#if UNITY_SERVER
                            name = "Server";
#endif
                            allPlatformSettings[index].enabled = true;
                            allPlatformSettings[index].name = name;
                        }

                        if (platformSettings != allPlatformSettings[index])
                            platformSettings = allPlatformSettings[index];
                    }
                    else RequestScriptCompilation();
                }
            }

            #region Defines
            List<string> defs = new();
            if (!multiThreaded) defs.Add("NEUTRON_MULTI_THREADED_REMOVED");
            else defs.Add("NEUTRON_MULTI_THREADED");
            if (!agressiveRelay) defs.Add("NEUTRON_AGRESSIVE_RELAY_REMOVED");
            else defs.Add("NEUTRON_AGRESSIVE_RELAY");
            Helper.SetDefine(defines: defs.ToArray());
            #endregion
        }
#endif

        internal static void AddIdentity(NeutronIdentity identity)
        {
            var key = (identity.id, identity.playerId, identity.isItFromTheServer, identity.objectType);
            if (!identities.TryAdd(key, identity))
                Logger.PrintError($"the identity already exists -> {key}");
        }

        internal static NeutronIdentity GetIdentity(ushort identityId, ushort playerId, bool isServer, ObjectType objType)
        {
            if (!identities.TryGetValue((identityId, playerId, isServer, objType), out NeutronIdentity identity))
                Logger.PrintWarning($"Indentity not found! -> [IsServer]={isServer}");
            return identity;
        }

        public static void AddHandler<T>(Action<ByteStream, bool> handler) where T : ISerializable, new()
        {
            T instance = new();
            if (!handlers.TryAdd(instance.Id, handler))
                Logger.PrintError($"Handler for {instance.Id} already exists!");
        }

        private static void Intern_Send(ByteStream byteStream, ushort playerId, Channel channel, Target target, SubTarget subTarget = SubTarget.None)
        {
            if (playerId != 0) udpServer.Send(byteStream, channel, target, subTarget, playerId);
            else udpClient.Send(byteStream, channel, target);
        }

        private static void Intern_Send(ByteStream byteStream, UdpEndPoint remoteEndPoint, Channel channel, Target target, SubTarget subTarget = SubTarget.None)
        {
            if (remoteEndPoint != null) udpServer.Send(byteStream, channel, target, subTarget, remoteEndPoint);
            else udpClient.Send(byteStream, channel, target);
        }

        internal static void OnMessage(ByteStream RECV_STREAM, MessageType messageType, Channel channel, Target target, UdpEndPoint remoteEndPoint, bool isServer)
        {
            switch (messageType)
            {
                case MessageType.Connect:
                    OnConnected?.Invoke(isServer, new IPEndPoint(new IPAddress(remoteEndPoint.GetIPAddress()), remoteEndPoint.GetPort()), RECV_STREAM);
                    break;
                case MessageType.RemoteStatic:
                case MessageType.RemoteScene:
                case MessageType.RemotePlayer:
                case MessageType.RemoteInstantiated:
                    {
                        ushort playerId = RECV_STREAM.ReadUShort();
                        ushort identityId = RECV_STREAM.ReadUShort();
                        byte rpcId = RECV_STREAM.ReadByte();
                        byte instanceId = RECV_STREAM.ReadByte();

                        ByteStream parameters = ByteStream.Get();
                        parameters.Write(RECV_STREAM, RECV_STREAM.Position, RECV_STREAM.BytesWritten);
                        ushort PLAYER_ID = playerId;

                        #region Convert the Types
                        ObjectType objectType = default;
                        switch (messageType)
                        {
                            case MessageType.RemoteStatic:
                                objectType = ObjectType.Static;
                                PLAYER_ID = isServer ? ServerId : (ushort)Id;
                                break;
                            case MessageType.RemoteScene:
                                objectType = ObjectType.Scene;
                                PLAYER_ID = isServer ? ServerId : (ushort)Id;
                                break;
                            case MessageType.RemotePlayer:
                                objectType = ObjectType.Player;
                                break;
                            case MessageType.RemoteInstantiated:
                                objectType = ObjectType.Instantiated;
                                break;
                        }
                        #endregion

                        #region Process the RPC
                        NeutronIdentity identity = GetIdentity(identityId, PLAYER_ID, isServer, objectType);
                        if (identity != null)
                        {
                            Action<ByteStream, bool, ushort> rpc = identity.GetRpc(instanceId, rpcId);
                            rpc?.Invoke(RECV_STREAM, isServer, playerId);
                        }
                        else
                            Logger.PrintWarning($"The identity has been destroyed or does not exist! -> [IsServer]={isServer} -> [{identityId}, {PLAYER_ID}, {isServer}, {objectType}]");
                        #endregion

                        #region Send
                        playerId = (ushort)remoteEndPoint.GetPort();
                        if (isServer && playerId != Port)
                            Remote(rpcId, identityId, instanceId, parameters, messageType, channel, target, SubTarget.None, playerId);
                        #endregion
                    }
                    break;
            }
        }

        internal static void Remote(byte id, ushort identity, byte instanceId, ByteStream parameters, MessageType msgType, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, ushort playerId = 0)
        {
            ByteStream remote = ByteStream.Get(msgType);
            remote.Write(playerId);
            remote.Write(identity);
            remote.Write(id);
            remote.Write(instanceId);
            remote.Write(parameters);
            parameters.Release();
            Intern_Send(remote, playerId, channel, target, subTarget);
            remote.Release();
        }

        internal void OnApplicationQuit()
        {
            tokenSource.Cancel();
            using (tokenSource)
            {
                udpClient.Close();
                udpServer.Close();
            }
        }
    }
}