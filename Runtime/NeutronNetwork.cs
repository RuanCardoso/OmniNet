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
#if NEUTRON_MULTI_THREADED
using System.Collections.Concurrent;
#else
using System.Collections.Generic;
using System.Linq;
#endif
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using static Neutron.Core.Enums;
using LocalPhysicsMode = Neutron.Core.Enums.LocalPhysicsMode;
using MessageType = Neutron.Core.Enums.MessageType;

namespace Neutron.Core
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-0x64)]
    [RequireComponent(typeof(ActionDispatcher))]
    public class NeutronNetwork : MonoBehaviour
    {
        private const byte SETTINGS_SIZE = 50;

        [Header("Enums")]
        [SerializeField] private LocalPhysicsMode physicsMode = LocalPhysicsMode.Physics3D;
        [SerializeField] private EncodingType encoding = EncodingType.ASCII;

        [Header("Others")]
        [SerializeField][Range(0, 10)][Label("FPS Update Rate")] private int fpsUpdateRate = 4;
        [SerializeField] private bool consoleInput;
        [SerializeField] private bool dontDestroy = false;
        [SerializeField] private bool loadNextScene = false;
        private Encoding _encoding = Encoding.ASCII;

        public static float framerate = 0f;
        public static float cpuMs = 0f;
        private static int frameCount = 0;
        private static float deltaTime = 0f;

        private static readonly Dictionary<(ushort, ushort, bool, byte, ObjectType), NeutronIdentity> identities = new(); // [Identity Id, Player Id, IsServer, Scene Id, Object Type]
        private static readonly Dictionary<int, Action<ByteStream, bool>> handlers = new();
        private static readonly UdpServer udpServer = new();
        private static readonly UdpClient udpClient = new();

        #region Events
        public static event Action<bool, IPEndPoint, ByteStream> OnConnected;
        #endregion

        internal static NeutronNetwork Instance { get; private set; }

        #region Properties
        internal Encoding Encoding => _encoding;
#if UNITY_EDITOR
        internal static Scene Scene { get; private set; }
        internal static PhysicsScene PhysicsScene { get; private set; }
        internal static PhysicsScene2D PhysicsScene2D { get; private set; }
#endif
        public static Player Player => udpClient.Player;
        internal static int Port { get; private set; }
        internal static ushort NetworkId { get; } = ushort.MaxValue;

        public static bool IsBind => udpServer.IsConnected;
#if !UNITY_SERVER || UNITY_EDITOR
        public static int Id => udpClient.Id;
        public static bool IsConnected => udpClient.IsConnected;
#else
        public static int Id => NetworkId;
        public static bool IsConnected => udpServer.IsConnected;
#endif
        public static ActionDispatcher Dispatcher => Instance.dispatcher;
        #endregion

        #region Fields
        [Header("Timers")]
        [InfoBox("Ping Time impacts clock sync between client and server.", EInfoBoxType.Warning)]
        [SerializeField][Range(0.01f, 5)] private float pingTime = 1f;
        [SerializeField][Range(0.1f, 5)] private float connectionTime = 1f;
        [Header("Socket")]
        [SerializeField] private int port = 5055;
        [SerializeField][Range(1, ushort.MaxValue)] private int byteStreams = 128;
        [SerializeField][Range(byte.MaxValue, ushort.MaxValue)] internal int windowSize = byte.MaxValue;
        [SerializeField][Range(1, 1500)] internal int udpPacketSize = 64 * 2;
        // defines
        [Header("Pre-Processor's")]
        [SerializeField] private bool agressiveRelay = false;
        [SerializeField][ReadOnly] private bool multiThreaded = false;
        [SerializeField][ReadOnly] private string[] defined;
        #endregion

        internal static double timeAsDouble;

        [SerializeField][HideInInspector] private LocalSettings[] allPlatformSettings = new LocalSettings[SETTINGS_SIZE];
        [Header("Plataforms")][SerializeField] internal LocalSettings platformSettings;

        private ActionDispatcher dispatcher;
        private readonly CancellationTokenSource tokenSource = new();

        internal static WaitForSeconds WAIT_FOR_CONNECT;
        internal static WaitForSeconds WAIT_FOR_PING;

#if NEUTRON_MULTI_THREADED
        private static readonly ConcurrentDictionary<int, RemoteCache> remoteCache = new();
#else
        private static readonly Dictionary<(byte remoteId, ushort identityId, byte instanceId, ushort playerId, byte sceneId, ObjectType objectType), RemoteCache> remoteCache = new(); // [Remote Id, Identity Id, Instance Id, Player Id, Scene Id, Object Type]
#endif

        public static IFormatterResolver Formatter { get; private set; }
        public static MessagePackSerializerOptions AddResolver(IFormatterResolver resolver = null, [CallerMemberName] string _ = "")
        {
            if (_ != "Awake")
                Logger.PrintError($"{nameof(AddResolver)} must be called from Awake!");
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
            ByteStream.streams = new(byteStreams);
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
            Port = port;
            var remoteEndPoint = new UdpEndPoint(IPAddress.Any, Port);
#if UNITY_SERVER || UNITY_EDITOR
            udpServer.Bind(remoteEndPoint);
            udpServer.CreateServerPlayer(NetworkId);
#endif
#if !UNITY_SERVER || UNITY_EDITOR
            udpClient.Bind(new UdpEndPoint(IPAddress.Any, NeutronHelper.GetFreePort()));
            udpClient.Connect(new UdpEndPoint(IPAddress.Parse(lHost.Ip), remoteEndPoint.GetPort()));
#endif
#if UNITY_EDITOR
            if (IsBind)
            {
                Scene = SceneManager.CreateScene("Server[Only Editor]", new CreateSceneParameters((UnityEngine.SceneManagement.LocalPhysicsMode)physicsMode));
                PhysicsScene = Scene.GetPhysicsScene();
                PhysicsScene2D = Scene.GetPhysicsScene2D();
            }
#endif

            #region Define Encoding
            _encoding = encoding switch
            {
                EncodingType.UTF8 => Encoding.UTF8,
                EncodingType.UTF7 => Encoding.UTF7,
                EncodingType.UTF32 => Encoding.UTF32,
                EncodingType.ASCII => Encoding.ASCII,
                EncodingType.Unicode => Encoding.Unicode,
                _ => Encoding.ASCII,
            };
            #endregion
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

        private void Start() => Invoke(nameof(Main), 1f);
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
            if (IsBind)
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
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.B)) udpClient.Disconnect();
#endif
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

        [ContextMenu("Neutron/Set Preprocessor", false)]
        [Button("Set Pre-Processor's", EButtonEnableMode.Editor)]
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

            NeutronHelper.SetDefines(MULTI_THREADED_DEFINE, AGRESSIVE_RELAY_DEFINE);
        }

        private void Reset() => OnValidate();
        private void OnValidate()
        {
            defined = NeutronHelper.GetDefines(out _).ToArray();
#if UNITY_SERVER
            BuildTarget buildTarget = BuildTarget.LinuxHeadlessSimulation;
#else
            BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
#endif
            int index = (int)buildTarget;
            allPlatformSettings ??= new LocalSettings[SETTINGS_SIZE];
            if (allPlatformSettings.Length == SETTINGS_SIZE)
            {
                if (allPlatformSettings.IsInBounds(index))
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
            var key = (identity.id, identity.playerId, identity.isItFromTheServer, identity.sceneId, identity.objectType);
            if (!identities.TryAdd(key, identity))
                Logger.PrintError($"the identity already exists -> {key}");
        }

        internal static NeutronIdentity GetIdentity(ushort identityId, ushort playerId, bool isServer, byte sceneId, ObjectType objType)
        {
            if (!identities.TryGetValue((identityId, playerId, isServer, sceneId, objType), out NeutronIdentity identity))
                Logger.PrintWarning($"Indentity not found! -> [IsServer]={isServer}");
            return identity;
        }

        public static void AddHandler<T>(Action<ByteStream, bool> handler) where T : ISerializable, new()
        {
            T instance = new();
            if (!handlers.TryAdd(instance.Id, handler))
                Logger.PrintError($"Handler for {instance.Id} already exists!");
        }

        private static void Intern_Send(ByteStream byteStream, ushort id, bool fromServer, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode)
        {
            if (fromServer) udpServer.Send(byteStream, channel, target, subTarget, cacheMode, id);
            else udpClient.Send(byteStream, channel, target, subTarget, cacheMode);
        }

        private static void Intern_Send(ByteStream byteStream, UdpEndPoint remoteEndPoint, bool fromServer, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode)
        {
            if (fromServer) udpServer.Send(byteStream, channel, target, subTarget, cacheMode, remoteEndPoint);
            else udpClient.Send(byteStream, channel, target, subTarget, cacheMode);
        }

        internal static void OnMessage(ByteStream RECV_STREAM, MessageType messageType, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode, UdpEndPoint remoteEndPoint, bool isServer)
        {
            switch (messageType)
            {
                case MessageType.Connect:
                    if (isServer) Logger.Print($"The endpoint {remoteEndPoint} has been established.");
                    OnConnected?.Invoke(isServer, new IPEndPoint(new IPAddress(remoteEndPoint.GetIPAddress()), remoteEndPoint.GetPort()), RECV_STREAM);
                    break;
                case MessageType.RemoteStatic:
                case MessageType.RemoteScene:
                case MessageType.RemotePlayer:
                case MessageType.RemoteDynamic:
                    {
                        ObjectType objectType = NeutronHelper.GetObjectType(messageType);
                        ushort fromId = RECV_STREAM.ReadUShort();
                        ushort toId = RECV_STREAM.ReadUShort();
                        byte sceneId = RECV_STREAM.ReadByte();
                        ushort identityId = RECV_STREAM.ReadUShort();
                        byte remoteId = RECV_STREAM.ReadByte();
                        byte instanceId = RECV_STREAM.ReadByte();

                        ByteStream message = ByteStream.Get();
                        message.WriteRemainingBytes(RECV_STREAM);
                        ushort PLAYER_ID_OF_IDENTITY = messageType == MessageType.RemoteStatic || messageType == MessageType.RemoteScene ? NeutronHelper.GetPlayerId(isServer) : toId;

                        #region Cache System
                        if (isServer)
                        {
#if UNITY_SERVER || UNITY_EDITOR
                            switch (cacheMode)
                            {
                                case CacheMode.Append:
                                    break;
                                case CacheMode.Overwrite:
                                    {
                                        var key = (remoteId, identityId, instanceId, toId, sceneId, objectType);
                                        if (remoteCache.TryGetValue(key, out RemoteCache cache))
                                            cache.SetData(message.Buffer, message.BytesWritten);
                                        else
                                        {
                                            byte[] data = new byte[Instance.udpPacketSize];
                                            Buffer.BlockCopy(message.Buffer, 0, data, 0, message.BytesWritten);
                                            RemoteCache remoteCache = new(data, message.BytesWritten, fromId, toId, sceneId, identityId, remoteId, instanceId, messageType, channel, objectType);
                                            if (!NeutronNetwork.remoteCache.TryAdd(key, remoteCache))
                                                Logger.PrintError("Could not create cache, hash key already exists?");
                                        }
                                    }
                                    break;
                            }
#endif
                        }
                        #endregion

                        #region Process RPC
                        switch (NeutronHelper.GetSubTarget(isServer, subTarget))
                        {
                            case SubTarget.Server:
                                {
                                    NeutronIdentity identity = GetIdentity(identityId, PLAYER_ID_OF_IDENTITY, isServer, sceneId, objectType);
                                    if (identity != null)
                                    {
                                        Action<ByteStream, ushort, ushort, RemoteStats> rpc = identity.GetRpc(instanceId, remoteId);
                                        rpc?.Invoke(RECV_STREAM, fromId, toId, new RemoteStats(NeutronTime.Time, RECV_STREAM.BytesRemaining));
                                    }
                                    else
                                        Logger.PrintWarning($"The identity has been destroyed or does not exist! -> [IsServer]={isServer} -> [{identityId}, {PLAYER_ID_OF_IDENTITY}, {isServer}]");
                                    break;
                                }
                        }
                        #endregion

                        #region Send
                        ushort fromPort = (ushort)remoteEndPoint.GetPort();
                        if (isServer && fromPort != Port)
                            Remote(remoteId, sceneId, identityId, instanceId, fromId, toId, isServer, message, messageType, channel, target, SubTarget.None, cacheMode);
                        #endregion
                    }
                    break;
                case MessageType.OnSerializeStatic:
                case MessageType.OnSerializeScene:
                case MessageType.OnSerializePlayer:
                case MessageType.OnSerializeDynamic:
                    {
                        ushort identityId = RECV_STREAM.ReadUShort();
                        ushort playerId = RECV_STREAM.ReadUShort();
                        byte instanceId = RECV_STREAM.ReadByte();
                        byte sceneId = RECV_STREAM.ReadByte();

                        ByteStream message = ByteStream.Get();
                        message.WriteRemainingBytes(RECV_STREAM);
                        ushort PLAYER_ID_OF_IDENTITY = messageType == MessageType.OnSerializeStatic || messageType == MessageType.OnSerializeScene ? NeutronHelper.GetPlayerId(isServer) : playerId;

                        #region Process OnSerializeView
                        switch (NeutronHelper.GetSubTarget(isServer, subTarget))
                        {
                            case SubTarget.Server:
                                {
                                    NeutronIdentity identity = GetIdentity(identityId, PLAYER_ID_OF_IDENTITY, isServer, sceneId, NeutronHelper.GetObjectType(messageType));
                                    if (identity != null)
                                        identity.GetNeutronObject(instanceId).OnSerializeView(RECV_STREAM, false, new RemoteStats(NeutronTime.Time, RECV_STREAM.BytesRemaining));
                                    else
                                        Logger.PrintWarning($"The identity has been destroyed or does not exist! -> [IsServer]={isServer} -> [{identityId}, {PLAYER_ID_OF_IDENTITY}, {isServer}]");
                                    break;
                                }
                        }
                        #endregion

                        #region Send
                        ushort fromPort = (ushort)remoteEndPoint.GetPort();
                        if (isServer && fromPort != Port)
                            OnSerializeView(message, identityId, instanceId, fromPort, sceneId, isServer, messageType, channel, target, SubTarget.None, cacheMode);
                        #endregion
                    }
                    break;
                case MessageType.OnSyncBaseStatic:
                case MessageType.OnSyncBaseScene:
                case MessageType.OnSyncBasePlayer:
                case MessageType.OnSyncBaseDynamic:
                    {
                        byte varId = RECV_STREAM.ReadByte();
                        ushort identityId = RECV_STREAM.ReadUShort();
                        ushort playerId = RECV_STREAM.ReadUShort();
                        byte instanceId = RECV_STREAM.ReadByte();
                        byte sceneId = RECV_STREAM.ReadByte();

                        ByteStream message = ByteStream.Get();
                        message.WriteRemainingBytes(RECV_STREAM);
                        ushort PLAYER_ID_OF_IDENTITY = messageType == MessageType.OnSyncBaseStatic || messageType == MessageType.OnSyncBaseScene ? NeutronHelper.GetPlayerId(isServer) : playerId;

                        #region Process OnSyncBase
                        switch (NeutronHelper.GetSubTarget(isServer, subTarget))
                        {
                            case SubTarget.Server:
                                {
                                    NeutronIdentity identity = GetIdentity(identityId, PLAYER_ID_OF_IDENTITY, isServer, sceneId, NeutronHelper.GetObjectType(messageType));
                                    if (identity != null)
                                        identity.GetNeutronObject(instanceId).OnSyncBase?.Invoke(varId, RECV_STREAM);
                                    else
                                        Logger.PrintWarning($"The identity has been destroyed or does not exist! -> [IsServer]={isServer} -> [{identityId}, {PLAYER_ID_OF_IDENTITY}, {isServer}]");
                                    break;
                                }
                        }
                        #endregion

                        #region Send
                        ushort fromPort = (ushort)remoteEndPoint.GetPort();
                        if (isServer && fromPort != Port)
                            OnSyncBase(message, varId, identityId, instanceId, fromPort, sceneId, isServer, messageType, channel, target, SubTarget.None, cacheMode);
                        #endregion
                    }
                    break;
                case MessageType.GetCache:
                    {
                        CacheType cacheType = (CacheType)RECV_STREAM.ReadByte();
                        byte id = RECV_STREAM.ReadByte();
                        bool ownerCache = RECV_STREAM.ReadBool();
                        ushort fromPort = (ushort)remoteEndPoint.GetPort();

                        switch (cacheType)
                        {
                            case CacheType.Remote:
                                {
                                    var caches = remoteCache.Where(x => x.Key.remoteId == id);
                                    if (caches.Count() != 0)
                                    {
                                        foreach (var ICache in caches)
                                        {
                                            RemoteCache cache = ICache.Value;
                                            if (ownerCache && cache.fromId == fromPort)
                                                continue;
                                            var message = ByteStream.Get();
                                            message.Write(cache.Buffer);
                                            #region Send
                                            if (isServer && fromPort != Port)
                                                Remote(cache.rpcId, cache.sceneId, cache.identityId, cache.instanceId, cache.fromId, cache.toId, isServer, message, cache.messageType, cache.channel, Target.Me, SubTarget.None, CacheMode.None);
                                            #endregion
                                        }
                                    }
                                    else Logger.PrintError("There is no cached data!");
                                }
                                break;
                        }
                    }
                    break;
            }
        }

        internal static void Remote(byte id, byte sceneId, ushort identity, byte instanceId, ushort fromId, ushort toId, bool fromServer, ByteStream msg, MessageType msgType, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode)
        {
            ByteStream message = ByteStream.Get(msgType);
            message.Write(fromId);
            message.Write(toId);
            message.Write(sceneId);
            message.Write(identity);
            message.Write(id);
            message.Write(instanceId);
            message.Write(msg);
            msg.Release();
            Intern_Send(message, toId, fromServer, channel, target, subTarget, cacheMode);
            message.Release();
        }

        internal static void OnSerializeView(ByteStream msg, ushort identity, byte instanceId, ushort playerId, byte sceneId, bool fromServer, MessageType msgType, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode)
        {
            ByteStream message = ByteStream.Get(msgType);
            message.Write(identity);
            message.Write(playerId);
            message.Write(instanceId);
            message.Write(sceneId);
            message.Write(msg);
            msg.Release();
            Intern_Send(message, playerId, fromServer, channel, target, subTarget, cacheMode);
            message.Release();
        }

        internal static void OnSyncBase(ByteStream msg, byte varId, ushort identity, byte instanceId, ushort playerId, byte sceneId, bool fromServer, MessageType msgType, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode)
        {
            ByteStream message = ByteStream.Get(msgType);
            message.Write(varId);
            message.Write(identity);
            message.Write(playerId);
            message.Write(instanceId);
            message.Write(sceneId);
            message.Write(msg);
            msg.Release();
            Intern_Send(message, playerId, fromServer, channel, target, subTarget, cacheMode);
            message.Release();
        }

        public static void GetCache(CacheType cacheType, bool ownerCache, byte cacheId, ushort playerId, bool fromServer, Channel channel)
        {
            ByteStream message = ByteStream.Get(MessageType.GetCache);
            message.Write((byte)cacheType);
            message.Write(cacheId);
            message.Write(ownerCache);
            Intern_Send(message, playerId, fromServer, channel, Target.Me, SubTarget.None, CacheMode.None);
            message.Release();
        }

        internal static void GlobalMessage(byte id, bool fromServer, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode)
        {
            ByteStream message = ByteStream.Get(MessageType.GlobalMessage);
            message.Write(id);
            Intern_Send(message, NeutronHelper.GetPlayerId(fromServer), fromServer, channel, target, subTarget, cacheMode);
            message.Release();
        }

        internal static void GlobalMessage(byte id, ushort playerId, bool fromServer, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode)
        {
            ByteStream message = ByteStream.Get(MessageType.GlobalMessage);
            message.Write(id);
            Intern_Send(message, playerId, fromServer, channel, target, subTarget, cacheMode);
            message.Release();
        }

        public static Player GetPlayer(ushort playerId, bool isServer = true) => isServer ? udpServer.GetClient(playerId).Player : udpClient.Player;

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