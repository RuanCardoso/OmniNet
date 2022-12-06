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
using NaughtyAttributes;
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

namespace Neutron.Core
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-0x64)]
    [RequireComponent(typeof(ActionDispatcher))]
    public class NeutronNetwork : MonoBehaviour
    {
        private const byte SETTINGS_SIZE = 50;

        #region Framerate
        [Header("[SETTINGS]")]
        [SerializeField] private LocalPhysicsMode physicsMode = LocalPhysicsMode.Physics3D;
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
        internal static Scene Scene { get; private set; }
        internal static PhysicsScene PhysicsScene { get; private set; }
        internal static PhysicsScene2D PhysicsScene2D { get; private set; }
#endif
        internal static int Port { get; private set; }
        internal static ushort ServerId { get; } = ushort.MaxValue;
        internal static NeutronNetwork Instance { get; private set; }
        public static int Id => udpClient.Id;
        public static bool IsConnected => udpClient.IsConnected;
        public static ActionDispatcher Dispatcher => Instance.dispatcher;
        #endregion

        #region Fields
        [InfoBox("The ping time directly influences the synchronization of the clock between the client and the server.", EInfoBoxType.Warning)]
        [SerializeField][Range(0.01f, 5)] private float pingTime = 1f;
        [SerializeField][Range(0.1f, 5)] private float connectionTime = 1f;
        [SerializeField][Range(byte.MaxValue, ushort.MaxValue)] internal int windowSize = byte.MaxValue;
        [SerializeField][Range(1, 1500)] internal int udpPacketSize = 64;
        [SerializeField]
#if !UNITY_SERVER
        [HideInInspector]
#endif
        private bool consoleInput;
        [SerializeField] private bool dontDestroy = false;
        [SerializeField] private bool loadNextScene = true;
        // defines
        [Header("[DEFINES]")]
        [SerializeField] private bool agressiveRelay = false;
        [SerializeField] private bool multiThreaded = false;
        [SerializeField][ReadOnly] private string[] defined;
        #endregion

        internal static double timeAsDouble;

        [SerializeField][HideInInspector] private LocalSettings[] allPlatformSettings = new LocalSettings[SETTINGS_SIZE];
        [Header("[PLATFORMS]")][SerializeField] internal LocalSettings platformSettings;

        private ActionDispatcher dispatcher;
        private readonly CancellationTokenSource tokenSource = new();

        internal static WaitForSeconds WAIT_FOR_CONNECT;
        internal static WaitForSeconds WAIT_FOR_PING;

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
            if (dontDestroy) DontDestroyOnLoad(gameObject);
            ByteStream.streams = new();
            //***************************************************
            WAIT_FOR_CONNECT = new(connectionTime);
            WAIT_FOR_PING = new(pingTime);
            //***************************************************
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
            Scene = SceneManager.CreateScene("Server[Only Editor]", new CreateSceneParameters((UnityEngine.SceneManagement.LocalPhysicsMode)physicsMode));
            PhysicsScene = Scene.GetPhysicsScene();
            PhysicsScene2D = Scene.GetPhysicsScene2D();
#endif
        }

        private void NeutronNetwork_OnConnected(bool isServer, IPEndPoint endPoint, ByteStream parameters)
        {
            if (loadNextScene)
            {
                int currentIndex = SceneManager.GetActiveScene().buildIndex;
                int nextIndex = currentIndex + 1;
#if UNITY_EDITOR
                if (!isServer) SceneManager.LoadScene(nextIndex, LoadSceneMode.Additive);
#else
                SceneManager.LoadScene(nextIndex, LoadSceneMode.Additive);
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

#if UNITY_EDITOR
        private void FixedUpdate()
        {
            switch (physicsMode)
            {
                case LocalPhysicsMode.Physics3D:
                    PhysicsScene.Simulate(Time.fixedDeltaTime);
                    break;
                case LocalPhysicsMode.Physics2D:
                    PhysicsScene2D.Simulate(Time.fixedDeltaTime);
                    break;
            }
        }
#endif

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
        [ContextMenu("Neutron/Reload Scripts", false)]
        [Button("Reload Scripts", EButtonEnableMode.Editor)]
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

        [ContextMenu("Neutron/Set Defines", false)]
        [Button("Set Defines", EButtonEnableMode.Editor)]
        private void SetDefines()
        {
            NeutronDefine MULTI_THREADED_DEFINE = new()
            {
                define = "NEUTRON_MULTI_THREADED",
                enabled = multiThreaded
            };

            NeutronDefine AGRESSIVE_RELAY_DEFINE = new()
            {
                define = "NEUTRON_AGRESSIVE_RELAY",
                enabled = agressiveRelay
            };

            Helper.SetDefines(MULTI_THREADED_DEFINE, AGRESSIVE_RELAY_DEFINE);
        }

        private void Reset() => OnValidate();
        private void OnValidate()
        {
            defined = Helper.GetDefines(out _).ToArray();
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

        private static void Intern_Send(ByteStream byteStream, ushort id, bool fromServer, Channel channel, Target target, SubTarget subTarget)
        {
            if (fromServer) udpServer.Send(byteStream, channel, target, subTarget, id);
            else udpClient.Send(byteStream, channel, target);
        }

        private static void Intern_Send(ByteStream byteStream, UdpEndPoint remoteEndPoint, bool fromServer, Channel channel, Target target, SubTarget subTarget)
        {
            if (fromServer) udpServer.Send(byteStream, channel, target, subTarget, remoteEndPoint);
            else udpClient.Send(byteStream, channel, target);
        }

        internal static void OnMessage(ByteStream RECV_STREAM, MessageType messageType, Channel channel, Target target, UdpEndPoint remoteEndPoint, bool isServer)
        {
            switch (messageType)
            {
                case MessageType.Connect:
                    Logger.Print("Connected!");
                    OnConnected?.Invoke(isServer, new IPEndPoint(new IPAddress(remoteEndPoint.GetIPAddress()), remoteEndPoint.GetPort()), RECV_STREAM);
                    break;
                case MessageType.RemoteStatic:
                case MessageType.RemoteScene:
                case MessageType.RemotePlayer:
                case MessageType.RemoteInstantiated:
                    {
                        ushort fromId = RECV_STREAM.ReadUShort();
                        ushort toId = RECV_STREAM.ReadUShort();
                        ushort identityId = RECV_STREAM.ReadUShort();
                        byte rpcId = RECV_STREAM.ReadByte();
                        byte instanceId = RECV_STREAM.ReadByte();

                        ByteStream parameters = ByteStream.Get();
                        parameters.Write(RECV_STREAM, RECV_STREAM.Position, RECV_STREAM.BytesWritten);
                        ushort resolved_id = toId;

                        #region Convert the Types
                        ObjectType objectType = default;
                        switch (messageType)
                        {
                            case MessageType.RemoteStatic:
                                objectType = ObjectType.Static;
                                resolved_id = isServer ? ServerId : (ushort)Id;
                                break;
                            case MessageType.RemoteScene:
                                objectType = ObjectType.Scene;
                                resolved_id = isServer ? ServerId : (ushort)Id;
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
                        NeutronIdentity identity = GetIdentity(identityId, resolved_id, isServer, objectType);
                        if (identity != null)
                        {
                            Action<ByteStream, ushort, ushort, RemoteStats> rpc = identity.GetRpc(instanceId, rpcId);
                            rpc?.Invoke(RECV_STREAM, fromId, toId, new RemoteStats(NeutronTime.Time, RECV_STREAM.BytesRemaining));
                        }
                        else
                            Logger.PrintWarning($"The identity has been destroyed or does not exist! -> [IsServer]={isServer} -> [{identityId}, {resolved_id}, {isServer}, {objectType}]");
                        #endregion

                        #region Send
                        ushort fromPort = (ushort)remoteEndPoint.GetPort();
                        if (isServer && fromPort != Port)
                            Remote(rpcId, identityId, instanceId, fromId, toId, isServer, parameters, messageType, channel, target, SubTarget.None);
                        #endregion
                    }
                    break;
            }
        }

        internal static void Remote(byte id, ushort identity, byte instanceId, ushort fromId, ushort toId, bool fromServer, ByteStream parameters, MessageType msgType, Channel channel, Target target, SubTarget subTarget)
        {
            ByteStream remote = ByteStream.Get(msgType);
            remote.Write(fromId);
            remote.Write(toId);
            remote.Write(identity);
            remote.Write(id);
            remote.Write(instanceId);
            remote.Write(parameters);
            parameters.Release();
            Intern_Send(remote, toId, fromServer, channel, target, subTarget);
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