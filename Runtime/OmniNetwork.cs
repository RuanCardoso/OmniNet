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
using LocalPhysicsMode = Omni.Core.Enums.LocalPhysicsMode;
using MessageType = Omni.Core.Enums.MessageType;
using MessagePack;
using MessagePack.Resolvers;
using MessagePack.Unity.Extension;
using MessagePack.Unity;
using Omni.Core.Cryptography;
using System.Security.Cryptography;

namespace Omni.Core
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(NetworkMonitor))]
    [RequireComponent(typeof(OmniDispatcher))]
    [RequireComponent(typeof(PlatformSettings))]
    public class OmniNetwork : MonoBehaviour
    {
        [Header("Others")]
        [SerializeField] private LocalPhysicsMode physicsMode = LocalPhysicsMode.Physics3D;
        [SerializeField][MaxValue(10)] private uint fpsUpdateRate = 4;

        private static int frameCount = 0;
        private static float deltaTime = 0f;

        private static readonly UdpServer udpServer = new();
        private static readonly UdpClient udpClient = new();

        internal static OmniNetwork Instance { get; private set; }

        #region Properties
        public static float Framerate { get; private set; }
        public static float CpuTimeMs { get; private set; }
#if UNITY_EDITOR
        internal static Scene Scene { get; private set; }
        internal static PhysicsScene PhysicsScene { get; private set; }
        internal static PhysicsScene2D PhysicsScene2D { get; private set; }
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
        [SerializeField][Range(0.1f, 2f)][Label("Ping Frequency")] private float pingTime = 1f; // seconds
        [SerializeField][Range(0.1f, 5f)][Label("Recon Frequency")] private float reconnectionTime = 1f; // seconds
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
            DataIOHandler.bsPool = new DataIOHandlerPool(ServerSettings.bSPoolSize);

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
            //OnConnected += OmniNetwork_OnConnected;

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
            var ip = IPAddress.Parse(ClientSettings.hosts[0].host);
            udpClient.Connect(new UdpEndPoint(ip, remoteEndPoint.GetPort()), tokenSource.Token);
#endif

#if UNITY_EDITOR
            if (IsBind)
            {
                Scene = SceneManager.CreateScene("Server[Editor Only]", new CreateSceneParameters((UnityEngine.SceneManagement.LocalPhysicsMode)physicsMode));
                PhysicsScene = Scene.GetPhysicsScene();
                PhysicsScene2D = Scene.GetPhysicsScene2D();
            }
#endif
        }

        private void GenerateAuthKeys()
        {
            // Generate AES Key
            using (Aes aes = Aes.Create())
            {
                aes.GenerateKey();
                AuthStorage.AesKey = aes.Key;
            }
#if UNITY_SERVER || UNITY_EDITOR
            RSACryptography.GetRSAKeys(out var sPrivateKey, out var sPublicKey);
            AuthStorage.RSA.Server.PrivateKey = sPrivateKey;
            AuthStorage.RSA.Server.PublicKey = sPublicKey;
#endif
#if !UNITY_SERVER || UNITY_EDITOR
            RSACryptography.GetRSAKeys(out var cPrivateKey, out var cPublicKey);
            AuthStorage.RSA.Client.PrivateKey = cPrivateKey;
            AuthStorage.RSA.Client.PublicKey = cPublicKey;
#endif

            string Ruan = "Ruan Cardoso";
            byte[] dataCrypted = RSACryptography.Encrypt(Encoding.UTF8.GetBytes(Ruan), AuthStorage.RSA.Server.PublicKey);
            byte[] dataDecrypted = RSACryptography.Decrypt(dataCrypted, AuthStorage.RSA.Server.PrivateKey);
            OmniLogger.Print($"RSA Cryptography: {Ruan} -> {Encoding.UTF8.GetString(dataDecrypted)}");
        }

        private void Start()
        {
            //GenerateAuthKeys();
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

        private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
#if UNITY_EDITOR
            GlobalEventHandler.FireSceneLoaded(scene, loadSceneMode, PlatformOp.Editor);
#elif UNITY_SERVER
            GlobalEventHandler.FireSceneLoaded(scene, loadSceneMode, PlatformOp.Server);
#else
            GlobalEventHandler.FireSceneLoaded(scene, loadSceneMode, PlatformOp.Client);
#endif
#if UNITY_SERVER && !UNITY_EDITOR
            OmniLogger.Print($"The scene X was loaded -> {scene.name}");
#endif
        }

        private void OnSceneUnloaded(Scene scene)
        {
#if UNITY_EDITOR
            GlobalEventHandler.FireSceneUnloaded(scene, PlatformOp.Editor);
#elif UNITY_SERVER
            GlobalEventHandler.FireSceneUnloaded(scene, PlatformOp.Server);
#else                          
            GlobalEventHandler.FireSceneUnloaded(scene, PlatformOp.Client);
#endif
#if UNITY_SERVER && !UNITY_EDITOR
            OmniLogger.Print($"The scene X was unloaded -> {scene.name}");
#endif
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

        private static void SendDataViaSocket(DataIOHandler IOHandler, ushort id, bool fromServer, DataDeliveryMode deliveryMode, DataTarget target, DataProcessingOption processingOption, DataCachingOption cachingOption)
        {
            ThrowErrorIfWrongSocket(fromServer);
            if (fromServer && IsBind)
            {
                udpServer.Send(IOHandler, deliveryMode, target, processingOption, cachingOption, id);
            }
            else
            {
                udpClient.Send(IOHandler, deliveryMode, target, processingOption, cachingOption);
            }
        }

        internal static void OnMessage(DataIOHandler _IOHandler_, MessageType messageType, DataDeliveryMode deliveryMode, DataTarget target, DataProcessingOption processingOption, DataCachingOption cachingOption, UdpEndPoint remoteEndPoint, bool isServer)
        {
            var selfType = OmniHelper.GetObjectType(messageType);
            // Let's process packets with maximum performance!
            // Data must be read in the same order in which they are written.
            switch (messageType)
            {
                case MessageType.SendMessage:
                    {
                        var fromId = _IOHandler_.ReadUShort();
                        GlobalEventHandler.FireMessageReceived(_IOHandler_, fromId, isServer);
                    }
                    break;
                case MessageType.Ping:
                    {
                        if (isServer)
                        {
                            ServerEventHandler.FirePlayerPingReceived(GetPlayerFromServer((ushort)remoteEndPoint.GetPort()));
                        }
                        else
                        {
                            ClientEventHandler.FirePingReceived();
                        }
                    }
                    break;
                case MessageType.Connect:
                    {
                        if (isServer)
                        {
                            ServerEventHandler.FirePlayerConnected(GetPlayerFromServer((ushort)remoteEndPoint.GetPort()));
                            OmniLogger.Print($"Info: The endpoint {remoteEndPoint} has been successfully established on the server.");
                        }
                        else
                        {
                            ClientEventHandler.FireConnected();
                        }
                    }
                    break;
                case MessageType.Disconnect:
                    {
                        if (isServer)
                        {
                            ServerEventHandler.FirePlayerDisconnected(GetPlayerFromServer((ushort)remoteEndPoint.GetPort()));
                            OmniLogger.Print($"Info: The endpoint {remoteEndPoint} has been successfully established on the server.");
                        }
                        else
                        {
                            ClientEventHandler.FireDisconnected();
                        }
                    }
                    break;
                case MessageType.Remote: // Global
                    {
                        var sourceId = _IOHandler_.ReadUShort();
                        var destinationId = _IOHandler_.ReadUShort();
                        var remoteId = _IOHandler_.ReadByte();
                        var instanceId = _IOHandler_.ReadByte();

                        DataIOHandler IOHandler = DataIOHandler.Get();
                        IOHandler.WriteRemainingBytes(_IOHandler_);

                        #region Cache System
                        if (isServer)
                        {
#if UNITY_SERVER || UNITY_EDITOR
                            var _ = (remoteId, instanceId, sourceId);
                            OmniHelper.CreateCache(Dictionaries.RemoteGlobalDataCache, _, cachingOption, IOHandler, sourceId, destinationId, 0, 0, remoteId, instanceId, default, deliveryMode, default);
#endif
                        }
                        #endregion

                        #region Process RPC
                        DataProcessingOption processedSubTarget = OmniHelper.GetSubTarget(isServer, processingOption);
                        switch (processedSubTarget)
                        {
                            case DataProcessingOption.ProcessOnServer:
                                {
                                    var rpc = OmniBehaviour.GetRpc(remoteId, instanceId, isServer);
                                    rpc?.Invoke(_IOHandler_, sourceId, destinationId, isServer, new RemoteStats(OmniTime.Time, _IOHandler_.BytesRemaining));
                                    break;
                                }
                        }
                        #endregion

                        #region Send
                        ushort fromPort = (ushort)remoteEndPoint.GetPort();
                        if (isServer && fromPort != Port)
                        {
                            Remote(remoteId, instanceId, sourceId, destinationId, isServer, IOHandler, deliveryMode, target, DataProcessingOption.DoNotProcessOnServer, cachingOption);
                        }
                        #endregion

                    }
                    break;
                case MessageType.RemoteStatic:
                case MessageType.RemoteScene:
                case MessageType.RemotePlayer:
                case MessageType.RemoteDynamic:
                    {
                        var fromId = _IOHandler_.ReadUShort();
                        var toId = _IOHandler_.ReadUShort();
                        var sceneId = _IOHandler_.ReadByte();
                        var identityId = _IOHandler_.ReadUShort();
                        var remoteId = _IOHandler_.ReadByte();
                        var instanceId = _IOHandler_.ReadByte();

                        DataIOHandler IOHandler = DataIOHandler.Get();
                        IOHandler.WriteRemainingBytes(_IOHandler_);
                        ushort pIdInIdentity = messageType == MessageType.RemoteStatic || messageType == MessageType.RemoteScene ? OmniHelper.GetPlayerId(isServer) : toId;

                        #region Cache System
                        if (isServer)
                        {
#if UNITY_SERVER || UNITY_EDITOR
                            var _ = (remoteId, identityId, instanceId, fromId, sceneId, selfType);
                            OmniHelper.CreateCache(Dictionaries.RemoteDataCache, _, cachingOption, IOHandler, fromId, toId, sceneId, identityId, remoteId, instanceId, messageType, deliveryMode, selfType);
#endif
                        }
                        #endregion

                        #region Process RPC
                        switch (OmniHelper.GetSubTarget(isServer, processingOption))
                        {
                            case DataProcessingOption.ProcessOnServer:
                                {
                                    OmniIdentity identity = GetIdentity(identityId, pIdInIdentity, isServer, sceneId, selfType);
                                    if (identity != null)
                                    {
                                        var rpc = identity.GetRpc(instanceId, remoteId);
                                        rpc?.Invoke(_IOHandler_, fromId, toId, new RemoteStats(OmniTime.Time, _IOHandler_.BytesRemaining));
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
                            Remote(remoteId, sceneId, identityId, instanceId, fromId, toId, isServer, IOHandler, messageType, deliveryMode, target, DataProcessingOption.DoNotProcessOnServer, cachingOption);
                        #endregion
                    }
                    break;
                case MessageType.OnSerializeStatic:
                case MessageType.OnSerializeScene:
                case MessageType.OnSerializePlayer:
                case MessageType.OnSerializeDynamic:
                    {
                        var identityId = _IOHandler_.ReadUShort();
                        var playerId = _IOHandler_.ReadUShort();
                        var instanceId = _IOHandler_.ReadByte();
                        var sceneId = _IOHandler_.ReadByte();

                        DataIOHandler IOHandler = DataIOHandler.Get();
                        IOHandler.WriteRemainingBytes(_IOHandler_);
                        ushort pIdInIdentity = messageType == MessageType.OnSerializeStatic || messageType == MessageType.OnSerializeScene ? OmniHelper.GetPlayerId(isServer) : playerId;

                        #region Cache System
                        if (isServer)
                        {
#if UNITY_SERVER || UNITY_EDITOR
                            var _ = (identityId, instanceId, playerId, sceneId, selfType);
                            OmniHelper.CreateCache(Dictionaries.SerializeDataCache, _, cachingOption, IOHandler, playerId, playerId, sceneId, identityId, 0, instanceId, messageType, deliveryMode, selfType);
#endif
                        }
                        #endregion

                        #region Process OnSerializeView
                        switch (OmniHelper.GetSubTarget(isServer, processingOption))
                        {
                            case DataProcessingOption.ProcessOnServer:
                                {
                                    OmniIdentity identity = GetIdentity(identityId, pIdInIdentity, isServer, sceneId, selfType);
                                    if (identity != null)
                                    {
                                        identity.GetOmniObject(instanceId).OnSerializeView(_IOHandler_, false, new RemoteStats(OmniTime.Time, _IOHandler_.BytesRemaining));
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
                            OnSerializeView(IOHandler, identityId, instanceId, playerId, sceneId, isServer, messageType, deliveryMode, target, DataProcessingOption.DoNotProcessOnServer, cachingOption);
                        #endregion
                    }
                    break;
                case MessageType.OnSyncBaseStatic:
                case MessageType.OnSyncBaseScene:
                case MessageType.OnSyncBasePlayer:
                case MessageType.OnSyncBaseDynamic:
                    {
                        var fieldId = _IOHandler_.ReadByte();
                        var identityId = _IOHandler_.ReadUShort();
                        var playerId = _IOHandler_.ReadUShort();
                        var instanceId = _IOHandler_.ReadByte();
                        var sceneId = _IOHandler_.ReadByte();

                        DataIOHandler IOHandler = DataIOHandler.Get();
                        IOHandler.WriteRemainingBytes(_IOHandler_);
                        ushort pIdInIdentity = messageType == MessageType.OnSyncBaseStatic || messageType == MessageType.OnSyncBaseScene ? OmniHelper.GetPlayerId(isServer) : playerId;

                        #region Cache System
                        if (isServer)
                        {
#if UNITY_SERVER || UNITY_EDITOR
                            var _ = (fieldId, identityId, instanceId, playerId, sceneId, selfType);
                            OmniHelper.CreateCache(Dictionaries.SyncDataCache, _, cachingOption, IOHandler, playerId, playerId, sceneId, identityId, fieldId, instanceId, messageType, deliveryMode, selfType);
#endif
                        }
                        #endregion

                        #region Process OnSyncBase
                        switch (OmniHelper.GetSubTarget(isServer, processingOption))
                        {
                            case DataProcessingOption.ProcessOnServer:
                                {
                                    OmniIdentity identity = GetIdentity(identityId, pIdInIdentity, isServer, sceneId, selfType);
                                    if (identity != null)
                                    {
                                        identity.GetOmniObject(instanceId).OnSyncBase?.Invoke(fieldId, _IOHandler_);
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
                            OnSyncBase(IOHandler, fieldId, identityId, instanceId, playerId, sceneId, isServer, messageType, deliveryMode, target, DataProcessingOption.DoNotProcessOnServer, cachingOption);
                        #endregion
                    }
                    break;
                case MessageType.GetCache:
                    {
                        var cacheType = (DataStorageType)_IOHandler_.ReadByte();
                        var id = _IOHandler_.ReadByte();
                        var ownerCache = _IOHandler_.ReadBool();
                        var fromPort = (ushort)remoteEndPoint.GetPort();

                        switch (cacheType)
                        {
                            case DataStorageType.Remote:
                                {
                                    OmniHelper.GetCache(Dictionaries.RemoteDataCache, x => x.Key.remoteId == id, (data, IOHandler) =>
                                    {
                                        Remote(data.rpcId, data.sceneId, data.identityId, data.instanceId, data.fromId, data.toId, isServer, IOHandler, data.messageType, data.deliveryMode, DataTarget.Self, DataProcessingOption.DoNotProcessOnServer, DataCachingOption.None, fromPort);
                                    }, ownerCache, fromPort, isServer);
                                }
                                break;
                            case DataStorageType.GlobalRemote:
                                {
                                    OmniHelper.GetCache(Dictionaries.RemoteGlobalDataCache, x => x.Key.remoteId == id, (data, IOHandler) =>
                                    {
                                        Remote(data.rpcId, data.instanceId, data.fromId, data.toId, isServer, IOHandler, data.deliveryMode, DataTarget.Self, DataProcessingOption.DoNotProcessOnServer, DataCachingOption.None, fromPort);
                                    }, ownerCache, fromPort, isServer);
                                }
                                break;
                            case DataStorageType.OnSerializeView:
                                {
                                    OmniHelper.GetCache(Dictionaries.SerializeDataCache, default, (data, IOHandler) =>
                                    {
                                        OnSerializeView(IOHandler, data.identityId, data.instanceId, data.toId, data.sceneId, isServer, data.messageType, data.deliveryMode, DataTarget.Self, DataProcessingOption.DoNotProcessOnServer, DataCachingOption.None, fromPort);
                                    }, ownerCache, fromPort, isServer, false);
                                }
                                break;
                            case DataStorageType.NetworkVars:
                                {
                                    OmniHelper.GetCache(Dictionaries.SyncDataCache, x => x.Key.varId == id, (data, IOHandler) =>
                                    {
                                        OnSyncBase(IOHandler, data.rpcId, data.identityId, data.instanceId, data.toId, data.sceneId, isServer, data.messageType, data.deliveryMode, DataTarget.Self, DataProcessingOption.DoNotProcessOnServer, DataCachingOption.None, fromPort);
                                    }, ownerCache, fromPort, isServer);
                                }
                                break;
                            case DataStorageType.GlobalMessage:
                                {
                                    OmniHelper.GetCache(Dictionaries.GlobalDataCache, x => x.Key.id == id, (data, IOHandler) =>
                                    {
                                        GlobalMessage(IOHandler, data.rpcId, data.toId, isServer, data.deliveryMode, DataTarget.Self, DataProcessingOption.DoNotProcessOnServer, DataCachingOption.None, fromPort);
                                    }, ownerCache, fromPort, isServer);
                                }
                                break;
                            case DataStorageType.LocalMessage:
                                {
                                    OmniHelper.GetCache(Dictionaries.LocalDataCache, x => x.Key.id == id, (cache, IOHandler) =>
                                    {
                                        LocalMessage(IOHandler, cache.rpcId, cache.identityId, cache.instanceId, cache.toId, cache.sceneId, isServer, cache.messageType, cache.deliveryMode, DataTarget.Self, DataProcessingOption.DoNotProcessOnServer, DataCachingOption.None, fromPort);
                                    }, ownerCache, fromPort, isServer);
                                }
                                break;
                        }
                    }
                    break;
                case MessageType.GlobalMessage:
                    {
                        var id = _IOHandler_.ReadByte();
                        var playerId = _IOHandler_.ReadUShort();

                        DataIOHandler IOHandler = DataIOHandler.Get();
                        IOHandler.WriteRemainingBytes(_IOHandler_);

                        #region Cache System
                        if (isServer)
                        {
#if UNITY_SERVER || UNITY_EDITOR
                            var _ = (id, playerId);
                            OmniHelper.CreateCache(Dictionaries.GlobalDataCache, _, cachingOption, IOHandler, playerId, playerId, 0, 0, id, 0, default, deliveryMode, default);
#endif
                        }
                        #endregion

                        #region Execute Message
                        switch (OmniHelper.GetSubTarget(isServer, processingOption))
                        {
                            case DataProcessingOption.ProcessOnServer:
                                {
                                    var handler = GetHandler(id);
                                    handler?.Invoke(_IOHandler_.ReadAsReadOnlyMemory(), playerId, isServer, new RemoteStats(OmniTime.Time, _IOHandler_.BytesRemaining));
                                }
                                break;
                        }
                        #endregion

                        #region Send
                        ushort fromPort = (ushort)remoteEndPoint.GetPort();
                        if (isServer && fromPort != Port)
                            GlobalMessage(IOHandler, id, playerId, isServer, deliveryMode, target, DataProcessingOption.DoNotProcessOnServer, cachingOption);
                        #endregion
                    }
                    break;
                case MessageType.LocalMessageStatic:
                case MessageType.LocalMessageScene:
                case MessageType.LocalMessagePlayer:
                case MessageType.LocalMessageDynamic:
                    {
                        var id = _IOHandler_.ReadByte();
                        var identityId = _IOHandler_.ReadUShort();
                        var playerId = _IOHandler_.ReadUShort();
                        var instanceId = _IOHandler_.ReadByte();
                        var sceneId = _IOHandler_.ReadByte();

                        DataIOHandler IOHandler = DataIOHandler.Get();
                        IOHandler.WriteRemainingBytes(_IOHandler_);
                        ushort pIdInIdentity = messageType == MessageType.LocalMessageStatic || messageType == MessageType.LocalMessageScene ? OmniHelper.GetPlayerId(isServer) : playerId;

                        #region Cache System
                        if (isServer)
                        {
#if UNITY_SERVER || UNITY_EDITOR
                            var _ = (id, identityId, instanceId, playerId, sceneId, selfType);
                            OmniHelper.CreateCache(Dictionaries.LocalDataCache, _, cachingOption, IOHandler, playerId, playerId, sceneId, identityId, id, instanceId, messageType, deliveryMode, selfType);
#endif
                        }
                        #endregion

                        #region Execute Message
                        switch (OmniHelper.GetSubTarget(isServer, processingOption))
                        {
                            case DataProcessingOption.ProcessOnServer:
                                {
                                    OmniIdentity identity = GetIdentity(identityId, pIdInIdentity, isServer, sceneId, OmniHelper.GetObjectType(messageType));
                                    if (identity != null)
                                    {
                                        OmniObject @this = identity.GetOmniObject(instanceId);
                                        var handler = @this.GetHandler(id);
                                        handler?.Invoke(_IOHandler_.ReadAsReadOnlyMemory(), playerId, isServer, new RemoteStats(OmniTime.Time, _IOHandler_.BytesRemaining));
                                    }
                                    else
                                    {
                                        OmniLogger.PrintError($"The identity has been destroyed or does not exist! -> [IsServer]={isServer} -> [{identityId}, {pIdInIdentity}, {isServer}]");
                                    }
                                }
                                break;
                        }
                        #endregion

                        #region Send
                        ushort fromPort = (ushort)remoteEndPoint.GetPort();
                        if (isServer && fromPort != Port)
                            LocalMessage(IOHandler, id, identityId, instanceId, playerId, sceneId, isServer, messageType, deliveryMode, target, DataProcessingOption.DoNotProcessOnServer, cachingOption);
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

        internal static void Remote(byte id, byte sceneId, ushort identity, byte instanceId, ushort fromId, ushort toId, bool fromServer, DataIOHandler _IOHandler_, MessageType msgType, DataDeliveryMode deliveryMode, DataTarget target, DataProcessingOption processingOption, DataCachingOption cachingOption, ushort senderId = 0)
        {
            DataIOHandler IOHandler = DataIOHandler.Get(msgType);
            IOHandler.Write(fromId);
            IOHandler.Write(toId);
            IOHandler.Write(sceneId);
            IOHandler.Write(identity);
            IOHandler.Write(id);
            IOHandler.Write(instanceId);
            IOHandler.Write(_IOHandler_);
            _IOHandler_.Release();
            SendDataViaSocket(IOHandler, OmniHelper.GetPlayerId(senderId, toId), fromServer, deliveryMode, target, processingOption, cachingOption);
            IOHandler.Release();
        }

        internal static void Remote(byte id, byte instanceId, ushort fromId, ushort toId, bool fromServer, DataIOHandler _IOHandler_, DataDeliveryMode deliveryMode, DataTarget target, DataProcessingOption processingOption, DataCachingOption cachingOption, ushort senderId = 0)
        {
            DataIOHandler IOHandler = DataIOHandler.Get(MessageType.Remote);
            IOHandler.Write(fromId);
            IOHandler.Write(toId);
            IOHandler.Write(id);
            IOHandler.Write(instanceId);
            IOHandler.Write(_IOHandler_);
            _IOHandler_.Release();
            SendDataViaSocket(IOHandler, OmniHelper.GetPlayerId(senderId, toId), fromServer, deliveryMode, target, processingOption, cachingOption);
            IOHandler.Release();
        }

        internal static void OnSerializeView(DataIOHandler _IOHandler_, ushort identity, byte instanceId, ushort playerId, byte sceneId, bool fromServer, MessageType msgType, DataDeliveryMode deliveryMode, DataTarget target, DataProcessingOption processingOption, DataCachingOption cachingOption, ushort senderId = 0)
        {
            DataIOHandler IOHandler = DataIOHandler.Get(msgType);
            IOHandler.Write(identity);
            IOHandler.Write(playerId);
            IOHandler.Write(instanceId);
            IOHandler.Write(sceneId);
            IOHandler.Write(_IOHandler_);
            _IOHandler_.Release();
            SendDataViaSocket(IOHandler, OmniHelper.GetPlayerId(senderId, playerId), fromServer, deliveryMode, target, processingOption, cachingOption);
            IOHandler.Release();
        }

        internal static void OnSyncBase(DataIOHandler _IOHandler_, byte varId, ushort identity, byte instanceId, ushort playerId, byte sceneId, bool fromServer, MessageType msgType, DataDeliveryMode deliveryMode, DataTarget target, DataProcessingOption processingOption, DataCachingOption cachingOption, ushort senderId = 0)
        {
            DataIOHandler IOHandler = DataIOHandler.Get(msgType);
            IOHandler.Write(varId);
            IOHandler.Write(identity);
            IOHandler.Write(playerId);
            IOHandler.Write(instanceId);
            IOHandler.Write(sceneId);
            IOHandler.Write(_IOHandler_);
            _IOHandler_.Release();
            SendDataViaSocket(IOHandler, OmniHelper.GetPlayerId(senderId, playerId), fromServer, deliveryMode, target, processingOption, cachingOption);
            IOHandler.Release();
        }

        public static void GetCache(DataStorageType cacheType, bool ownerCache, byte cacheId, bool fromServer, DataDeliveryMode deliveryMode) => GetCache(cacheType, ownerCache, cacheId, OmniHelper.GetPlayerId(fromServer), fromServer, deliveryMode);
        public static void GetCache(DataStorageType cacheType, bool ownerCache, byte cacheId, ushort playerId, bool fromServer, DataDeliveryMode deliveryMode)
        {
            DataIOHandler IOHandler = DataIOHandler.Get(MessageType.GetCache);
            IOHandler.Write((byte)cacheType);
            IOHandler.Write(cacheId);
            IOHandler.Write(ownerCache);
            SendDataViaSocket(IOHandler, playerId, fromServer, deliveryMode, DataTarget.Self, DataProcessingOption.DoNotProcessOnServer, DataCachingOption.None);
            IOHandler.Release();
        }

        internal static void GlobalMessage(DataIOHandler _IOHandler_, byte id, ushort playerId, bool fromServer, DataDeliveryMode deliveryMode, DataTarget target, DataProcessingOption processingOption, DataCachingOption cachingOption, ushort senderId = 0)
        {
            DataIOHandler IOHandler = DataIOHandler.Get(MessageType.GlobalMessage);
            IOHandler.Write(id);
            IOHandler.Write(playerId);
            IOHandler.Write(_IOHandler_);
            _IOHandler_.Release();
            SendDataViaSocket(IOHandler, OmniHelper.GetPlayerId(senderId, playerId), fromServer, deliveryMode, target, processingOption, cachingOption);
            IOHandler.Release();
        }

        internal static void LocalMessage(DataIOHandler _IOHandler_, byte id, ushort identity, byte instanceId, ushort playerId, byte sceneId, bool fromServer, MessageType msgType, DataDeliveryMode deliveryMode, DataTarget target, DataProcessingOption processingOption, DataCachingOption cachingOption, ushort senderId = 0)
        {
            DataIOHandler IOHandler = DataIOHandler.Get(msgType);
            IOHandler.Write(id);
            IOHandler.Write(identity);
            IOHandler.Write(playerId);
            IOHandler.Write(instanceId);
            IOHandler.Write(sceneId);
            IOHandler.Write(_IOHandler_);
            _IOHandler_.Release();
            SendDataViaSocket(IOHandler, OmniHelper.GetPlayerId(senderId, playerId), fromServer, deliveryMode, target, processingOption, cachingOption);
            IOHandler.Release();
        }

        public static void SendMessage(DataIOHandler _IOHandler_, ushort playerId, bool fromServer = false, DataDeliveryMode deliveryMode = DataDeliveryMode.Secured, DataTarget target = DataTarget.BroadcastExcludingSelf, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer)
        {
            DataIOHandler IOHandler = DataIOHandler.Get(MessageType.SendMessage);
            IOHandler.Write(playerId);
            IOHandler.Write(_IOHandler_);
            _IOHandler_.Release();
            SendDataViaSocket(IOHandler, playerId, fromServer, deliveryMode, target, processingOption, DataCachingOption.None);
            IOHandler.Release();
        }

        public static void SendMessage(DataIOHandler _IOHandler_, bool fromServer = false, DataDeliveryMode deliveryMode = DataDeliveryMode.Secured, DataTarget target = DataTarget.BroadcastExcludingSelf, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer)
        {
            SendMessage(_IOHandler_, OmniHelper.GetPlayerId(fromServer), fromServer, deliveryMode, target, processingOption);
        }

        public static OmniPlayer GetPlayerFromServer(ushort playerId)
        {
            OmniPlayer player = udpServer.GetClient(playerId).Player;
            if (player == null)
            {
                OmniLogger.PrintError("GetPlayerFromServer: Player not found for player ID " + playerId);
            }
            return player;
        }

        private static OmniPlayer GetPlayerFromClient(ushort playerId)
        {
            OmniPlayer player = udpClient.GetClient(playerId).Player;
            if (player == null)
            {
                OmniLogger.PrintError("GetPlayerFromClient: Player not found for player ID " + playerId);
            }
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