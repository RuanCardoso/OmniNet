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

using Newtonsoft.Json.Utilities;
using System;
#if OMNI_MULTI_THREADED
using System.Collections.Concurrent;
#else
using System.IO;
using System.Linq;
#endif
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
#if UNITY_EDITOR
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using static Omni.Core.Enums;
using static Omni.Core.PlatformSettings;
using EventType = Omni.Core.Enums.EventType;
using LocalPhysicsMode = Omni.Core.Enums.LocalPhysicsMode;
using MessageType = Omni.Core.Enums.MessageType;
using MessagePack;
using MessagePack.Resolvers;
using MessagePack.Unity.Extension;
using MessagePack.Unity;

namespace Omni.Core
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(OmniDispatcher))]
    [RequireComponent(typeof(PlatformSettings))]
    public class OmniNetwork : MonoBehaviour
    {
        [Header("Enums")]
        [SerializeField] private LocalPhysicsMode physicsMode = LocalPhysicsMode.Physics3D;
        [SerializeField] private EncodingType encoding = EncodingType.ASCII;

        [Header("Others")]
        [SerializeField][MaxValue(10)] private uint fpsUpdateRate = 4;
        private Encoding _encoding = Encoding.ASCII;

        private static int frameCount = 0;
        private static float deltaTime = 0f;

        private static readonly UdpServer udpServer = new();
        private static readonly UdpClient udpClient = new();

        #region Events
        public static event Action<bool, IPEndPoint, ByteStream> OnConnected;
        #endregion

        internal static OmniNetwork Instance { get; private set; }

        #region Properties
        public static float Framerate { get; private set; }
        public static float CpuTimeMs { get; private set; }
        internal Encoding Encoding => _encoding;
#if UNITY_EDITOR
        internal static Scene Scene { get; private set; }
        public static PhysicsScene PhysicsScene { get; private set; }
        public static PhysicsScene2D PhysicsScene2D { get; private set; }
#endif
        public static OmniPlayer Player => udpClient.Player;
        internal static int Port { get; private set; }
        internal static ushort NetworkId { get; } = ushort.MaxValue;
        internal static bool IsBind => udpServer.IsConnected;

#if !UNITY_SERVER || UNITY_EDITOR
        public static int Id => udpClient.Id;
        public static bool IsConnected => udpClient.IsConnected;
#else
        public static int Id => NetworkId;
        public static bool IsConnected => udpServer.IsConnected;
#endif
        public static OmniDispatcher Dispatcher => Instance.dispatcher;
        #endregion

        #region Fields
        [Header("Timers")]
        [InfoBox("Ping Time impacts clock sync between client and server.", EInfoBoxType.Warning)]
        [SerializeField][Range(0.01f, 60f)][Label("Ping")] private float pingTime = 1f; // seconds
        [SerializeField][Range(0.1f, 5f)][Label("Reconnection")] private float reconnectionTime = 1f; // seconds
        [SerializeField][Range(1f, 300f)][Label("Ping Sweep")] private float pingSweepTime = 1f; // seconds
        [SerializeField][Range(1f, 300f)][Label("Max Ping Request")] private double maxPingRequestTime = 60d; // seconds

        [Header("Pre-Processor's")]
        [SerializeField] private bool agressiveRelay = false;
        [SerializeField][ReadOnly] private bool multiThreaded = false;
        [SerializeField][ReadOnly] private string[] defines;
        #endregion

        private OmniDispatcher dispatcher;
        private readonly CancellationTokenSource tokenSource = new();

        internal static WaitForSeconds WAIT_FOR_CONNECT;
        internal static WaitForSeconds WAIT_FOR_PING;
        internal static WaitForSeconds WAIT_FOR_CHECK_REC_PING;

        static IFormatterResolver Formatter { get; set; }
        public static MessagePackSerializerOptions AddResolver(IFormatterResolver IFormatterResolver, [CallerMemberName] string methodName = "")
        {
            const string expectedMethodName = "Awake";

            if (methodName != expectedMethodName)
            {
                OmniLogger.PrintError($"{nameof(AddResolver)} must be called from {expectedMethodName}!");
                return MessagePackSerializer.DefaultOptions;
            }

            if (IFormatterResolver == null)
            {
                IFormatterResolver = CompositeResolver.Create(UnityBlitWithPrimitiveArrayResolver.Instance, UnityResolver.Instance, StandardResolver.Instance);
            }

            Formatter = CompositeResolver.Create(IFormatterResolver, Formatter);
            return MessagePackSerializer.DefaultOptions = MessagePackSerializerOptions.Standard.WithResolver(Formatter);
        }

        private void Awake()
        {
            Instance = this;
            ByteStream.bsPool = new ByteStreamPool(ServerSettings.bSPoolSize);

            _ = AddResolver(null);
            dispatcher = GetComponent<OmniDispatcher>();
            AotHelper.EnsureDictionary<string, object>();

            if (ServerSettings.dontDestroy)
            {
                DontDestroyOnLoad(gameObject);
            }

            // Wait for Seconds
            WAIT_FOR_CONNECT = new(reconnectionTime);
            WAIT_FOR_PING = new(pingTime);
            WAIT_FOR_CHECK_REC_PING = new(pingSweepTime);

            // Registers
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            OnConnected += OmniNetwork_OnConnected;

            // Framerate
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = ClientSettings.maxFramerate;

            // Bind and Connect
            Port = ServerSettings.port;
            var remoteEndPoint = new UdpEndPoint(IPAddress.Any, Port);
#if UNITY_SERVER || UNITY_EDITOR
            udpServer.Bind(remoteEndPoint);
            if (IsBind)
            {
                udpServer.Initialize(NetworkId);
                StartCoroutine(udpServer.CheckTheLastReceivedPing(maxPingRequestTime));
            }
#endif

#if !UNITY_SERVER || UNITY_EDITOR
            udpClient.Bind(new UdpEndPoint(IPAddress.Any, OmniHelper.GetFreePort()));
            udpClient.Connect(new UdpEndPoint(IPAddress.Parse(ClientSettings.hosts[0].host), remoteEndPoint.GetPort()));
#endif

#if UNITY_EDITOR
            if (IsBind)
            {
                Scene = SceneManager.CreateScene("Server[Editor Only]", new CreateSceneParameters((UnityEngine.SceneManagement.LocalPhysicsMode)physicsMode));
                PhysicsScene = Scene.GetPhysicsScene();
                PhysicsScene2D = Scene.GetPhysicsScene2D();
            }
#endif
            _encoding = encoding switch
            {
                EncodingType.UTF8 => Encoding.UTF8,
                EncodingType.UTF7 => Encoding.UTF7,
                EncodingType.UTF32 => Encoding.UTF32,
                EncodingType.ASCII => Encoding.ASCII,
                EncodingType.Unicode => Encoding.Unicode,
                _ => Encoding.ASCII,
            };
        }

        private void Start()
        {
            Invoke(nameof(Main), 1.5f);
        }

        private void Update()
        {
            // Atualizar o contador de tempo e quadros
            deltaTime += Time.unscaledDeltaTime;
            frameCount++;

            // Calcular a taxa de quadros e o tempo de CPU quando for necessário
            if (deltaTime > 1f / fpsUpdateRate)
            {
                Framerate = frameCount / deltaTime;
                CpuTimeMs = deltaTime / frameCount * 1000f;

                // Reiniciar os contadores
                deltaTime = 0f;
                frameCount = 0;
            }
        }

#if UNITY_EDITOR
        private void FixedUpdate()
        {
            if (IsBind)
            {
                if (physicsMode == LocalPhysicsMode.Physics3D)
                {
                    PhysicsScene.Simulate(Time.fixedDeltaTime);
                }
                else if (physicsMode == LocalPhysicsMode.Physics2D)
                {
                    PhysicsScene2D.Simulate(Time.fixedDeltaTime);
                }
            }
        }
#endif

        private void OmniNetwork_OnConnected(bool isServer, IPEndPoint endPoint, ByteStream parameters)
        {
            if (ServerSettings.loadNextScene)
            {
                LoadNextScene(isServer);
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
#if UNITY_SERVER && !UNITY_EDITOR
            OmniLogger.Print($"The scene X was loaded -> {scene.name}");
#endif
        }

        private void OnSceneUnloaded(Scene scene)
        {
#if UNITY_SERVER && !UNITY_EDITOR
            OmniLogger.Print($"The scene X was unloaded -> {scene.name}");
#endif
        }

        private void LoadNextScene(bool isServer)
        {
            int currentIndex = SceneManager.GetActiveScene().buildIndex;
            int nextIndex = currentIndex + 1;

#if UNITY_EDITOR
            if (!isServer)
            {
                LoadScene(nextIndex);
                UnsubscribeOnConnected();
            }
#else
            LoadScene(nextIndex);
            UnsubscribeOnConnected();
#endif
        }

        private void LoadScene(int sceneIndex)
        {
            SceneManager.LoadScene(sceneIndex, LoadSceneMode.Additive);
        }

        private void UnsubscribeOnConnected()
        {
            OnConnected -= OmniNetwork_OnConnected;
        }

        private void Main()
        {
            InitializeConsole();
            CheckGarbageCollectorSettings();
            CheckApiModeSettings();
        }

        private void InitializeConsole()
        {
#if UNITY_SERVER && !UNITY_EDITOR
            Console.Clear();
            if (ServerSettings.hasCommands)
            {
                OmniConsole.Initialize(tokenSource.Token, this);
            }
#endif
        }

        private void CheckGarbageCollectorSettings()
        {
            if (!GarbageCollector.isIncremental)
            {
                OmniLogger.Print("Consider enabling \"Incremental Garbage Collection\" for improved performance. This option can maximize performance by efficiently managing memory usage during runtime.");
            }
        }

        private void CheckApiModeSettings()
        {
#if !NETSTANDARD2_1
            OmniLogger.Print("Consider changing the API Mode to \".NET Standard 2.1\" for improved performance and compatibility. .NET Standard 2.1 offers enhanced features, performance optimizations, and broader library support, resulting in better performance and increased functionality for your application.");
#endif
#if !ENABLE_IL2CPP && !UNITY_EDITOR
            OmniLogger.Print("Consider changing the API Mode to \"IL2CPP\" for optimal performance. IL2CPP provides enhanced performance and security by converting your code into highly optimized C++ during the build process.");
#endif
        }

#if UNITY_EDITOR
        [ContextMenu("Omni/Set Preprocessor", false)]
        [Button("Set Pre-Processor's", EButtonEnableMode.Editor)]
        private void SetDefines()
        {
            OmniDef multiThreadedDefine = new OmniDef
            {
                define = "OMNI_MULTI_THREADED",
                enabled = multiThreaded
            };

            OmniDef aggressiveRelayDefine = new OmniDef
            {
                define = "OMNI_AGRESSIVE_RELAY",
                enabled = agressiveRelay
            };

            OmniHelper.SetDefines(multiThreadedDefine, aggressiveRelayDefine);
        }

        private void Reset() => OnValidate();
        private void OnValidate() => defines = GetOmniDefines();

        private string[] GetOmniDefines()
        {
            return OmniHelper.GetDefines(out _)
                .Where(x => x.StartsWith("OMNI_"))
                .ToArray();
        }
#endif
        internal static void AddIdentity(OmniIdentity identity)
        {
            var key = (identity.id, identity.playerId, identity.isItFromTheServer, identity.sceneId, identity.objectType);
            if (Dictionaries.Identities.TryGetValue(key, out _))
            {
                OmniLogger.PrintError($"The identity already exists: ID={identity.id}, PlayerID={identity.playerId}, IsFromServer={identity.isItFromTheServer}, SceneID={identity.sceneId}, ObjectType={identity.objectType}");
                return;
            }

            Dictionaries.Identities.TryAdd(key, identity);
        }

        private static OmniIdentity GetIdentity(ushort identityId, ushort playerId, bool isServer, byte sceneId, ObjectType objType)
        {
            if (!Dictionaries.Identities.TryGetValue((identityId, playerId, isServer, sceneId, objType), out var identity))
            {
                OmniLogger.PrintError($"Identity not found! -> ID={identityId}, PlayerID={playerId}, IsServer={isServer}, SceneID={sceneId}, ObjectType={objType}");
                return null;
            }

            if (identity == null)
            {
                OmniLogger.PrintError($"Identity is null! -> ID={identityId}, PlayerID={playerId}, IsServer={isServer}, SceneID={sceneId}, ObjectType={objType}");
            }

            return identity;
        }

        private static Action<ReadOnlyMemory<byte>, ushort, bool, RemoteStats> GetHandler(byte id)
        {
#pragma warning disable IDE0046
            if (Dictionaries.Handlers.TryGetValue(id, out var handler))
            {
                return handler;
            }
#pragma warning restore IDE0046

            return null;
        }

        public static byte AddHandler<T>(Action<ReadOnlyMemory<byte>, ushort, bool, RemoteStats> handler) where T : IMessage, new()
        {
            T instance = new();
            if (!Dictionaries.Handlers.TryAdd(instance.Id, handler))
            {
                OmniLogger.PrintError($"Error: Failed to add a handler for ID={instance.Id}.");
                OmniLogger.PrintError("Please make sure the handler for this ID does not already exist.");
                return instance.Id;
            }

            using (MemoryStream mStream = new())
            {
                try
                {
                    MessagePackSerializer.Serialize(mStream, instance);
                }
                catch (Exception ex)
                {
                    OmniLogger.PrintError($"Error: Failed to serialize {typeof(T).Name}: {ex.Message} {ex.InnerException.Message}");
                    OmniLogger.PrintError("Hint: It may be necessary to generate Ahead-of-Time (AOT) code and register the type resolver.");
                }
            }

            return instance.Id;
        }

        private static void ThrowErrorIfWrongSocket(bool fromServer)
        {
#if UNITY_SERVER && !UNITY_EDITOR
    if (!fromServer)
        throw new InvalidOperationException("The server cannot send data through the client socket. Make sure to use the server socket for sending data from the server side.");
#elif !UNITY_SERVER && !UNITY_EDITOR
    if (fromServer)
        throw new InvalidOperationException("The client cannot send data through the server socket. Make sure to use the client socket for sending data from the client side.");
#endif
        }

        private static void SendDataViaSocket(ByteStream byteStream, ushort id, bool fromServer, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode)
        {
            ThrowErrorIfWrongSocket(fromServer);
            if (fromServer && IsBind)
            {
                udpServer.Send(byteStream, channel, target, subTarget, cacheMode, id);
            }
            else
            {
                udpClient.Send(byteStream, channel, target, subTarget, cacheMode);
            }
        }

        internal static void OnMessage(ByteStream parameters, MessageType messageType, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode, UdpEndPoint remoteEndPoint, bool isServer)
        {
            var selfType = OmniHelper.GetObjectType(messageType);
            // Let's process packets with maximum performance!
            // Data must be read in the same order in which they are written.
            switch (messageType)
            {
                case MessageType.Connect:
                    {
                        if (isServer)
                        {
                            OmniLogger.Print($"Info: The endpoint {remoteEndPoint} has been successfully established on the server.");
                        }

                        var ipAddress = new IPAddress(remoteEndPoint.GetIPAddress());
                        var remoteIPEndPoint = new IPEndPoint(ipAddress, remoteEndPoint.GetPort());
                        OnConnected?.Invoke(isServer, remoteIPEndPoint, parameters);
                        break;
                    }
                case MessageType.Remote: // Global
                    {
                        var sourceId = parameters.ReadUShort();
                        var destinationId = parameters.ReadUShort();
                        var remoteId = parameters.ReadByte();
                        var instanceId = parameters.ReadByte();

                        ByteStream message = ByteStream.Get();
                        message.WriteRemainingBytes(parameters);

                        #region Cache System
                        if (isServer)
                        {
#if UNITY_SERVER || UNITY_EDITOR
                            var _ = (remoteId, instanceId, sourceId);
                            OmniHelper.CreateCache(Dictionaries.RemoteGlobalDataCache, _, cacheMode, message, sourceId, destinationId, 0, 0, remoteId, instanceId, default, channel, default);
#endif
                        }
                        #endregion

                        #region Process RPC
                        SubTarget processedSubTarget = OmniHelper.GetSubTarget(isServer, subTarget);
                        switch (processedSubTarget)
                        {
                            case SubTarget.Server:
                                {
                                    var rpc = OmniBehaviour.GetRpc(remoteId, instanceId, isServer);
                                    rpc?.Invoke(parameters, sourceId, destinationId, isServer, new RemoteStats(OmniTime.Time, parameters.BytesRemaining));
                                    break;
                                }
                        }
                        #endregion

                        #region Send
                        ushort fromPort = (ushort)remoteEndPoint.GetPort();
                        if (isServer && fromPort != Port)
                        {
                            Remote(remoteId, instanceId, sourceId, destinationId, isServer, message, channel, target, SubTarget.None, cacheMode);
                        }
                        #endregion

                    }
                    break;
                case MessageType.RemoteStatic:
                case MessageType.RemoteScene:
                case MessageType.RemotePlayer:
                case MessageType.RemoteDynamic:
                    {
                        var fromId = parameters.ReadUShort();
                        var toId = parameters.ReadUShort();
                        var sceneId = parameters.ReadByte();
                        var identityId = parameters.ReadUShort();
                        var remoteId = parameters.ReadByte();
                        var instanceId = parameters.ReadByte();

                        ByteStream message = ByteStream.Get();
                        message.WriteRemainingBytes(parameters);
                        ushort pIdInIdentity = messageType == MessageType.RemoteStatic || messageType == MessageType.RemoteScene ? OmniHelper.GetPlayerId(isServer) : toId;

                        #region Cache System
                        if (isServer)
                        {
#if UNITY_SERVER || UNITY_EDITOR
                            var _ = (remoteId, identityId, instanceId, fromId, sceneId, selfType);
                            OmniHelper.CreateCache(Dictionaries.RemoteDataCache, _, cacheMode, message, fromId, toId, sceneId, identityId, remoteId, instanceId, messageType, channel, selfType);
#endif
                        }
                        #endregion

                        #region Process RPC
                        switch (OmniHelper.GetSubTarget(isServer, subTarget))
                        {
                            case SubTarget.Server:
                                {
                                    OmniIdentity identity = GetIdentity(identityId, pIdInIdentity, isServer, sceneId, selfType);
                                    if (identity != null)
                                    {
                                        var rpc = identity.GetRpc(instanceId, remoteId);
                                        rpc?.Invoke(parameters, fromId, toId, new RemoteStats(OmniTime.Time, parameters.BytesRemaining));
                                    }
                                    else
                                    {
                                        OmniLogger.PrintError($"The identity has been destroyed or does not exist! -> [IsServer]={isServer} -> [{identityId}, {pIdInIdentity}, {isServer}]");
                                    }
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
                        var identityId = parameters.ReadUShort();
                        var playerId = parameters.ReadUShort();
                        var instanceId = parameters.ReadByte();
                        var sceneId = parameters.ReadByte();

                        ByteStream message = ByteStream.Get();
                        message.WriteRemainingBytes(parameters);
                        ushort PLAYER_ID_OF_IDENTITY = messageType == MessageType.OnSerializeStatic || messageType == MessageType.OnSerializeScene ? OmniHelper.GetPlayerId(isServer) : playerId;

                        #region Cache System
                        if (isServer)
                        {
#if UNITY_SERVER || UNITY_EDITOR
                            var _ = (identityId, instanceId, playerId, sceneId, selfType);
                            OmniHelper.CreateCache(Dictionaries.SerializeDataCache, _, cacheMode, message, playerId, playerId, sceneId, identityId, 0, instanceId, messageType, channel, selfType);
#endif
                        }
                        #endregion

                        #region Process OnSerializeView
                        switch (OmniHelper.GetSubTarget(isServer, subTarget))
                        {
                            case SubTarget.Server:
                                {
                                    OmniIdentity identity = GetIdentity(identityId, PLAYER_ID_OF_IDENTITY, isServer, sceneId, selfType);
                                    if (identity != null)
                                    {
                                        identity.GetOmniObject(instanceId).OnSerializeView(parameters, false, new RemoteStats(OmniTime.Time, parameters.BytesRemaining));
                                    }
                                    else
                                    {
                                        OmniLogger.PrintError($"The identity has been destroyed or does not exist! -> [IsServer]={isServer} -> [{identityId}, {PLAYER_ID_OF_IDENTITY}, {isServer}]");
                                    }
                                    break;
                                }
                        }
                        #endregion

                        #region Send
                        ushort fromPort = (ushort)remoteEndPoint.GetPort();
                        if (isServer && fromPort != Port)
                            OnSerializeView(message, identityId, instanceId, playerId, sceneId, isServer, messageType, channel, target, SubTarget.None, cacheMode);
                        #endregion
                    }
                    break;
                case MessageType.OnSyncBaseStatic:
                case MessageType.OnSyncBaseScene:
                case MessageType.OnSyncBasePlayer:
                case MessageType.OnSyncBaseDynamic:
                    {
                        var fieldId = parameters.ReadByte();
                        var identityId = parameters.ReadUShort();
                        var playerId = parameters.ReadUShort();
                        var instanceId = parameters.ReadByte();
                        var sceneId = parameters.ReadByte();

                        ByteStream message = ByteStream.Get();
                        message.WriteRemainingBytes(parameters);
                        ushort PLAYER_ID_OF_IDENTITY = messageType == MessageType.OnSyncBaseStatic || messageType == MessageType.OnSyncBaseScene ? OmniHelper.GetPlayerId(isServer) : playerId;

                        #region Cache System
                        if (isServer)
                        {
#if UNITY_SERVER || UNITY_EDITOR
                            var _ = (fieldId, identityId, instanceId, playerId, sceneId, selfType);
                            OmniHelper.CreateCache(Dictionaries.SyncDataCache, _, cacheMode, message, playerId, playerId, sceneId, identityId, fieldId, instanceId, messageType, channel, selfType);
#endif
                        }
                        #endregion

                        #region Process OnSyncBase
                        switch (OmniHelper.GetSubTarget(isServer, subTarget))
                        {
                            case SubTarget.Server:
                                {
                                    OmniIdentity identity = GetIdentity(identityId, PLAYER_ID_OF_IDENTITY, isServer, sceneId, selfType);
                                    if (identity != null)
                                    {
                                        identity.GetOmniObject(instanceId).OnSyncBase?.Invoke(fieldId, parameters);
                                    }
                                    else
                                    {
                                        OmniLogger.PrintError($"The identity has been destroyed or does not exist! -> [IsServer]={isServer} -> [{identityId}, {PLAYER_ID_OF_IDENTITY}, {isServer}]");
                                    }
                                    break;
                                }
                        }
                        #endregion

                        #region Send
                        ushort fromPort = (ushort)remoteEndPoint.GetPort();
                        if (isServer && fromPort != Port)
                            OnSyncBase(message, fieldId, identityId, instanceId, playerId, sceneId, isServer, messageType, channel, target, SubTarget.None, cacheMode);
                        #endregion
                    }
                    break;
                case MessageType.GetCache:
                    {
                        var cacheType = (CacheType)parameters.ReadByte();
                        var id = parameters.ReadByte();
                        var ownerCache = parameters.ReadBool();
                        var fromPort = (ushort)remoteEndPoint.GetPort();

                        switch (cacheType)
                        {
                            case CacheType.Remote:
                                {
                                    OmniHelper.GetCache(Dictionaries.RemoteDataCache, x => x.Key.remoteId == id, (data, message) =>
                                    {
                                        Remote(data.rpcId, data.sceneId, data.identityId, data.instanceId, data.fromId, data.toId, isServer, message, data.messageType, data.channel, Target.Me, SubTarget.None, CacheMode.None, fromPort);
                                    }, ownerCache, fromPort, isServer);
                                }
                                break;
                            case CacheType.GlobalRemote:
                                {
                                    OmniHelper.GetCache(Dictionaries.RemoteGlobalDataCache, x => x.Key.remoteId == id, (data, message) =>
                                    {
                                        Remote(data.rpcId, data.instanceId, data.fromId, data.toId, isServer, message, data.channel, Target.Me, SubTarget.None, CacheMode.None, fromPort);
                                    }, ownerCache, fromPort, isServer);
                                }
                                break;
                            case CacheType.OnSerialize:
                                {
                                    OmniHelper.GetCache(Dictionaries.SerializeDataCache, default, (data, message) =>
                                    {
                                        OnSerializeView(message, data.identityId, data.instanceId, data.toId, data.sceneId, isServer, data.messageType, data.channel, Target.Me, SubTarget.None, CacheMode.None, fromPort);
                                    }, ownerCache, fromPort, isServer, false);
                                }
                                break;
                            case CacheType.OnSync:
                                {
                                    OmniHelper.GetCache(Dictionaries.SyncDataCache, x => x.Key.varId == id, (data, message) =>
                                    {
                                        OnSyncBase(message, data.rpcId, data.identityId, data.instanceId, data.toId, data.sceneId, isServer, data.messageType, data.channel, Target.Me, SubTarget.None, CacheMode.None, fromPort);
                                    }, ownerCache, fromPort, isServer);
                                }
                                break;
                            case CacheType.GlobalMessage:
                                {
                                    OmniHelper.GetCache(Dictionaries.GlobalDataCache, x => x.Key.id == id, (data, message) =>
                                    {
                                        GlobalMessage(message, data.rpcId, data.toId, isServer, data.channel, Target.Me, SubTarget.None, CacheMode.None, fromPort);
                                    }, ownerCache, fromPort, isServer);
                                }
                                break;
                            case CacheType.LocalMessage:
                                {
                                    OmniHelper.GetCache(Dictionaries.LocalDataCache, x => x.Key.id == id, (cache, message) =>
                                    {
                                        LocalMessage(message, cache.rpcId, cache.identityId, cache.instanceId, cache.toId, cache.sceneId, isServer, cache.messageType, cache.channel, Target.Me, SubTarget.None, CacheMode.None, fromPort);
                                    }, ownerCache, fromPort, isServer);
                                }
                                break;
                        }
                    }
                    break;
                case MessageType.GlobalMessage:
                    {
                        var id = parameters.ReadByte();
                        var playerId = parameters.ReadUShort();

                        ByteStream message = ByteStream.Get();
                        message.WriteRemainingBytes(parameters);

                        #region Cache System
                        if (isServer)
                        {
#if UNITY_SERVER || UNITY_EDITOR
                            var _ = (id, playerId);
                            OmniHelper.CreateCache(Dictionaries.GlobalDataCache, _, cacheMode, message, playerId, playerId, 0, 0, id, 0, default, channel, default);
#endif
                        }
                        #endregion

                        #region Execute Message
                        switch (OmniHelper.GetSubTarget(isServer, subTarget))
                        {
                            case SubTarget.Server:
                                {
                                    var handler = GetHandler(id);
                                    handler?.Invoke(parameters.ReadAsReadOnlyMemory(), playerId, isServer, new RemoteStats(OmniTime.Time, parameters.BytesRemaining));
                                }
                                break;
                        }
                        #endregion

                        #region Send
                        ushort fromPort = (ushort)remoteEndPoint.GetPort();
                        if (isServer && fromPort != Port)
                            GlobalMessage(message, id, playerId, isServer, channel, target, SubTarget.None, cacheMode);
                        #endregion
                    }
                    break;
                case MessageType.LocalMessageStatic:
                case MessageType.LocalMessageScene:
                case MessageType.LocalMessagePlayer:
                case MessageType.LocalMessageDynamic:
                    {
                        var id = parameters.ReadByte();
                        var identityId = parameters.ReadUShort();
                        var playerId = parameters.ReadUShort();
                        var instanceId = parameters.ReadByte();
                        var sceneId = parameters.ReadByte();

                        ByteStream message = ByteStream.Get();
                        message.WriteRemainingBytes(parameters);
                        ushort PLAYER_ID_OF_IDENTITY = messageType == MessageType.LocalMessageStatic || messageType == MessageType.LocalMessageScene ? OmniHelper.GetPlayerId(isServer) : playerId;

                        #region Cache System
                        if (isServer)
                        {
#if UNITY_SERVER || UNITY_EDITOR
                            var _ = (id, identityId, instanceId, playerId, sceneId, selfType);
                            OmniHelper.CreateCache(Dictionaries.LocalDataCache, _, cacheMode, message, playerId, playerId, sceneId, identityId, id, instanceId, messageType, channel, selfType);
#endif
                        }
                        #endregion

                        #region Execute Message
                        switch (OmniHelper.GetSubTarget(isServer, subTarget))
                        {
                            case SubTarget.Server:
                                {
                                    OmniIdentity identity = GetIdentity(identityId, PLAYER_ID_OF_IDENTITY, isServer, sceneId, OmniHelper.GetObjectType(messageType));
                                    if (identity != null)
                                    {
                                        OmniObject @this = identity.GetOmniObject(instanceId);
                                        var handler = @this.GetHandler(id);
                                        handler?.Invoke(parameters.ReadAsReadOnlyMemory(), playerId, isServer, new RemoteStats(OmniTime.Time, parameters.BytesRemaining));
                                    }
                                    else
                                    {
                                        OmniLogger.PrintError($"The identity has been destroyed or does not exist! -> [IsServer]={isServer} -> [{identityId}, {PLAYER_ID_OF_IDENTITY}, {isServer}]");
                                    }
                                }
                                break;
                        }
                        #endregion

                        #region Send
                        ushort fromPort = (ushort)remoteEndPoint.GetPort();
                        if (isServer && fromPort != Port)
                            LocalMessage(message, id, identityId, instanceId, playerId, sceneId, isServer, messageType, channel, target, SubTarget.None, cacheMode);
                        #endregion
                    }
                    break;
            }
        }

        public static bool IsMine(ushort playerId) => Id == playerId;
        public static bool Interval(ref double lastTime, double frequency, bool localTime = true)
        {
            if (localTime)
            {
                if (OmniTime.LocalTime - lastTime >= frequency)
                {
                    lastTime = OmniTime.LocalTime;
                    return true;
                }
                else return false;
            }
            else
            {
                if (OmniTime.Time - lastTime >= frequency)
                {
                    lastTime = OmniTime.Time;
                    return true;
                }
                else return false;
            }
        }

        internal static void Remote(byte id, byte sceneId, ushort identity, byte instanceId, ushort fromId, ushort toId, bool fromServer, ByteStream msg, MessageType msgType, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode, ushort senderId = 0)
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
            SendDataViaSocket(message, OmniHelper.GetPlayerId(senderId, toId), fromServer, channel, target, subTarget, cacheMode);
            message.Release();
        }

        internal static void Remote(byte id, byte instanceId, ushort fromId, ushort toId, bool fromServer, ByteStream msg, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode, ushort senderId = 0)
        {
            ByteStream message = ByteStream.Get(MessageType.Remote);
            message.Write(fromId);
            message.Write(toId);
            message.Write(id);
            message.Write(instanceId);
            message.Write(msg);
            msg.Release();
            SendDataViaSocket(message, OmniHelper.GetPlayerId(senderId, toId), fromServer, channel, target, subTarget, cacheMode);
            message.Release();
        }

        internal static void OnSerializeView(ByteStream msg, ushort identity, byte instanceId, ushort playerId, byte sceneId, bool fromServer, MessageType msgType, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode, ushort senderId = 0)
        {
            ByteStream message = ByteStream.Get(msgType);
            message.Write(identity);
            message.Write(playerId);
            message.Write(instanceId);
            message.Write(sceneId);
            message.Write(msg);
            msg.Release();
            SendDataViaSocket(message, OmniHelper.GetPlayerId(senderId, playerId), fromServer, channel, target, subTarget, cacheMode);
            message.Release();
        }

        internal static void OnSyncBase(ByteStream msg, byte varId, ushort identity, byte instanceId, ushort playerId, byte sceneId, bool fromServer, MessageType msgType, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode, ushort senderId = 0)
        {
            ByteStream message = ByteStream.Get(msgType);
            message.Write(varId);
            message.Write(identity);
            message.Write(playerId);
            message.Write(instanceId);
            message.Write(sceneId);
            message.Write(msg);
            msg.Release();
            SendDataViaSocket(message, OmniHelper.GetPlayerId(senderId, playerId), fromServer, channel, target, subTarget, cacheMode);
            message.Release();
        }

        public static void GetCache(CacheType cacheType, bool ownerCache, byte cacheId, bool fromServer, Channel channel) => GetCache(cacheType, ownerCache, cacheId, OmniHelper.GetPlayerId(fromServer), fromServer, channel);
        public static void GetCache(CacheType cacheType, bool ownerCache, byte cacheId, ushort playerId, bool fromServer, Channel channel)
        {
            ByteStream message = ByteStream.Get(MessageType.GetCache);
            message.Write((byte)cacheType);
            message.Write(cacheId);
            message.Write(ownerCache);
            SendDataViaSocket(message, playerId, fromServer, channel, Target.Me, SubTarget.None, CacheMode.None);
            message.Release();
        }

        internal static void GlobalMessage(ByteStream msg, byte id, ushort playerId, bool fromServer, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode, ushort senderId = 0)
        {
            ByteStream message = ByteStream.Get(MessageType.GlobalMessage);
            message.Write(id);
            message.Write(playerId);
            message.Write(msg);
            msg.Release();
            SendDataViaSocket(message, OmniHelper.GetPlayerId(senderId, playerId), fromServer, channel, target, subTarget, cacheMode);
            message.Release();
        }

        internal static void LocalMessage(ByteStream msg, byte id, ushort identity, byte instanceId, ushort playerId, byte sceneId, bool fromServer, MessageType msgType, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode, ushort senderId = 0)
        {
            ByteStream message = ByteStream.Get(msgType);
            message.Write(id);
            message.Write(identity);
            message.Write(playerId);
            message.Write(instanceId);
            message.Write(sceneId);
            message.Write(msg);
            msg.Release();
            SendDataViaSocket(message, OmniHelper.GetPlayerId(senderId, playerId), fromServer, channel, target, subTarget, cacheMode);
            message.Release();
        }

        internal static void FireEvent(ByteStream msg, EventType eventType, Target target = Target.All)
        {
#if UNITY_EDITOR || UNITY_SERVER
            ByteStream message = ByteStream.Get(MessageType.FireEvent);
            message.Write((byte)eventType);
            message.Write(msg);
            msg.Release();
            SendDataViaSocket(message, NetworkId, true, Channel.Reliable, target, SubTarget.None, CacheMode.None);
            message.Release();
#else
            OmniLogger.PrintError("Fire Event not work on client side!");
#endif
        }

        public static OmniPlayer GetPlayerFromServer(ushort playerId)
        {
            OmniPlayer player = udpServer.GetClient(playerId).Player;
            if (player == null)
                OmniLogger.PrintError("Player not found!");
            return player;
        }

        public static OmniPlayer GetPlayerFromClient(ushort playerId)
        {
            OmniPlayer player = udpClient.GetClient(playerId).Player;
            if (player == null)
                OmniLogger.PrintError("Player not found!");
            return player;
        }

        internal void OnApplicationQuit()
        {
            tokenSource.Cancel();
            using (tokenSource)
            {
                udpClient.Disconnect();
                udpClient.Close();
                udpServer.Close();
            }
        }
    }
}