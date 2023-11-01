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
using System;
#if NEUTRON_MULTI_THREADED
using System.Collections.Concurrent;
#else
using System.Collections.Generic;
using System.IO;
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
using EventType = Neutron.Core.Enums.EventType;
using LocalPhysicsMode = Neutron.Core.Enums.LocalPhysicsMode;
using MessageType = Neutron.Core.Enums.MessageType;

namespace Neutron.Core
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-0x64)]
    [RequireComponent(typeof(ActionDispatcher))]
    public class NeutronNetwork : MonoBehaviour
    {
        // Constants
        private const byte SETTINGS_SIZE = 50;

        // Enums
        [Header("Enums")]
        [SerializeField] private LocalPhysicsMode physicsMode = LocalPhysicsMode.Physics3D;
        [SerializeField] private EncodingType encoding = EncodingType.ASCII;

        // Other Variables
        [Header("Others")]
        [SerializeField][MaxValue(10)] private uint fpsUpdateRate = 4;
        [SerializeField] private bool isConsoleInputEnabled;
        [SerializeField] private bool dontDestroy = false;
        [SerializeField] private bool loadNextScene = false;
        private Encoding _encoding = Encoding.ASCII;

        private static int frameCount = 0;
        private static float deltaTime = 0f;

        // Dictionaries
        private static readonly Dictionary<(ushort, ushort, bool, byte, ObjectType), NeutronIdentity> identities = new();
        private static readonly Dictionary<int, Action<ReadOnlyMemory<byte>, ushort, bool, RemoteStats>> handlers = new();
        private static readonly Dictionary<(byte remoteId, ushort identityId, byte instanceId, ushort playerId, byte sceneId, ObjectType objectType), NeutronCache> remoteCache = new();
        private static readonly Dictionary<(byte remoteId, byte instanceId, ushort playerId), NeutronCache> globalRemoteCache = new();
        private static readonly Dictionary<(ushort identityId, byte instanceId, ushort playerId, byte sceneId, ObjectType objectType), NeutronCache> serializeCache = new();
        private static readonly Dictionary<(byte varId, ushort identityId, byte instanceId, ushort playerId, byte sceneId, ObjectType objectType), NeutronCache> syncCache = new();
        private static readonly Dictionary<(byte id, ushort playerId), NeutronCache> globalCache = new();
        private static readonly Dictionary<(byte id, ushort identityId, byte instanceId, ushort playerId, byte sceneId, ObjectType objectType), NeutronCache> localCache = new();

        // Networking
        private static readonly UdpServer udpServer = new();
        private static readonly UdpClient udpClient = new();

        // Events
        #region Events
        public static event Action<bool, IPEndPoint, ByteStream> OnConnected;
        #endregion

        // Singleton Instance
        internal static NeutronNetwork Instance { get; private set; }

        #region Properties
        public static float Framerate { get; private set; }
        public static float CpuTimeMs { get; private set; }
        internal Encoding Encoding => _encoding;
#if UNITY_EDITOR
        internal static Scene Scene { get; private set; }
        public static PhysicsScene PhysicsScene { get; private set; }
        public static PhysicsScene2D PhysicsScene2D { get; private set; }
#endif
        public static Player Player => udpClient.Player;
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
        public static ActionDispatcher Dispatcher => Instance.dispatcher;
        #endregion

        #region Fields
        [Header("Timers")]
        [InfoBox("Ping Time impacts clock sync between client and server.", EInfoBoxType.Warning)]
        [SerializeField][Range(0.01f, 60f)][Label("Ping")] private float pingTime = 1f; // seconds
        [SerializeField][Range(0.1f, 5f)][Label("Reconnection")] private float reconnectionTime = 1f; // seconds
        [SerializeField][Range(1f, 300f)][Label("Ping Sweep")] private float pingSweepTime = 1f; // seconds
        [SerializeField][Range(1f, 300f)][Label("Max Ping Request")] private double maxPingRequestTime = 60d; // seconds

        [Header("Socket")]
        [SerializeField] private int port = 5055;
        [SerializeField][Range(1, ushort.MaxValue)] private int byteStreams = 128;
        [SerializeField][Range(byte.MaxValue, ushort.MaxValue)] internal int windowSize = byte.MaxValue;
        [SerializeField][Range(1, 1500)] internal int udpPacketSize = 64 * 2;

        // defines
        [Header("Pre-Processor's")]
        [SerializeField] private bool agressiveRelay = false;
        [SerializeField][ReadOnly] private bool multiThreaded = false;
        [SerializeField][ReadOnly] private string[] defines;
        #endregion

        //* Multi-Threading
        //internal static double timeAsDouble;

        [SerializeField]
        [HideInInspector]
        private LocalSettings[] allPlatformSettings = new LocalSettings[SETTINGS_SIZE];

        [Header("Plataforms")]
        [SerializeField] internal LocalSettings platformSettings;

        private ActionDispatcher dispatcher;
        private readonly CancellationTokenSource tokenSource = new();

        internal static WaitForSeconds WAIT_FOR_CONNECT;
        internal static WaitForSeconds WAIT_FOR_PING;
        internal static WaitForSeconds WAIT_FOR_CHECK_REC_PING;

        private static IFormatterResolver Formatter { get; set; }

        public static MessagePackSerializerOptions AddResolver(IFormatterResolver resolver, [CallerMemberName] string methodName = "")
        {
            const string expectedMethodName = "Awake";

            if (methodName != expectedMethodName)
            {
                Logger.PrintError($"{nameof(AddResolver)} must be called from {expectedMethodName}!");
                return MessagePackSerializer.DefaultOptions;
            }

            if (resolver == default)
            {
                resolver = CompositeResolver.Create(UnityBlitWithPrimitiveArrayResolver.Instance, UnityResolver.Instance, StandardResolver.Instance);
            }

            Formatter = CompositeResolver.Create(resolver, Formatter);

            return MessagePackSerializer.DefaultOptions = MessagePackSerializerOptions.Standard.WithResolver(resolver);
        }

        private void Awake()
        {
            Instance = this;
            dispatcher = GetComponent<ActionDispatcher>();
            AddResolver(default);
            if (dontDestroy)
            {
                DontDestroyOnLoad(gameObject);
            }
            ByteStream.streams = new(byteStreams);

            // Wait for Seconds
            WAIT_FOR_CONNECT = new(reconnectionTime);
            WAIT_FOR_PING = new(pingTime);
            WAIT_FOR_CHECK_REC_PING = new(pingSweepTime);

            // Registers
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            OnConnected += NeutronNetwork_OnConnected;

            // Framerate
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = platformSettings.maxFramerate;

            // Bind and Connect
            LocalSettings.Host lHost = platformSettings.hosts[0];
            Port = port;
            var remoteEndPoint = new UdpEndPoint(IPAddress.Any, Port);

#if UNITY_SERVER || UNITY_EDITOR
            udpServer.Bind(remoteEndPoint);
            if (IsBind)
            {
                udpServer.CreateServerPlayer(NetworkId);
                StartCoroutine(udpServer.CheckTheLastReceivedPing(maxPingRequestTime));
            }
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

            // Define Encoding
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
            // Inicia o método Main após um atraso de 1 segundo
            Invoke(nameof(Main), 1f);
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

            //* Seção de Multi-Threading
            //timeAsDouble = Time.timeAsDouble;
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

        private void NeutronNetwork_OnConnected(bool isServer, IPEndPoint endPoint, ByteStream parameters)
        {
            if (loadNextScene)
            {
                LoadNextScene(isServer);
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
#if UNITY_SERVER && !UNITY_EDITOR
           Logger.Log("The scene X was loaded.");
#endif
        }

        private void OnSceneUnloaded(Scene scene)
        {
#if UNITY_SERVER && !UNITY_EDITOR
            Logger.Log("The scene X was unloaded.");
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
            OnConnected -= NeutronNetwork_OnConnected;
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
            if (isConsoleInputEnabled)
            {
                NeutronConsole.Initialize(tokenSource.Token, this);
            }
#endif
        }

        private void CheckGarbageCollectorSettings()
        {
            if (!GarbageCollector.isIncremental)
            {
                Logger.PrintWarning("Consider enabling \"Incremental Garbage Collection\" for improved performance. This option can maximize performance by efficiently managing memory usage during runtime.");
            }
        }

        private void CheckApiModeSettings()
        {
#if !NETSTANDARD2_1
            Logger.PrintWarning("Consider changing the API Mode to \".NET Standard 2.1\" for improved performance and compatibility. .NET Standard 2.1 offers enhanced features, performance optimizations, and broader library support, resulting in better performance and increased functionality for your application.");
#endif
#if !ENABLE_IL2CPP && !UNITY_EDITOR
            Logger.PrintWarning("Consider changing the API Mode to \"IL2CPP\" for optimal performance. IL2CPP provides enhanced performance and security by converting your code into highly optimized C++ during the build process.");
#endif
        }

#if UNITY_EDITOR
        [ContextMenu("Neutron/Request Script Compilation", false)]
        [Button("Request Script Compilation", EButtonEnableMode.Editor)]
        private void RequestScriptCompilation()
        {
            foreach (var platformSettings in allPlatformSettings)
            {
                if (platformSettings != null)
                {
                    platformSettings.enabled = false;
                }
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.None);
            }
            else
            {
                Logger.PrintError("RequestScriptCompilation -> Failed");
            }
        }

        [ContextMenu("Neutron/Set Preprocessor", false)]
        [Button("Set Pre-Processor's", EButtonEnableMode.Editor)]
        private void SetDefines()
        {
            NeutronDefine multiThreadedDefine = new NeutronDefine
            {
                define = "NEUTRON_MULTI_THREADED",
                enabled = multiThreaded
            };

            NeutronDefine aggressiveRelayDefine = new NeutronDefine
            {
                define = "NEUTRON_AGRESSIVE_RELAY",
                enabled = agressiveRelay
            };

            NeutronHelper.SetDefines(multiThreadedDefine, aggressiveRelayDefine);
        }


        private void Reset() => OnValidate();
        private void OnValidate()
        {
            defines = GetNeutronDefines();

            BuildTarget activeBuildTarget = GetActiveBuildTarget();

            int buildTargetIndex = (int)activeBuildTarget;
            if (allPlatformSettings == null)
            {
                allPlatformSettings = new LocalSettings[SETTINGS_SIZE];
            }

            if (allPlatformSettings.Length != SETTINGS_SIZE)
                return;

            if (!allPlatformSettings.IsInBounds(buildTargetIndex))
                return;

            if (allPlatformSettings[buildTargetIndex] != null)
            {
                UpdatePlatformSettings(activeBuildTarget, buildTargetIndex);
            }
            else
            {
                RequestScriptCompilation();
            }
        }

        private string[] GetNeutronDefines()
        {
            return NeutronHelper.GetDefines(out _)
                .Where(x => x.StartsWith("NEUTRON_"))
                .ToArray();
        }

        private BuildTarget GetActiveBuildTarget()
        {
#if UNITY_SERVER
            return BuildTarget.LinuxHeadlessSimulation;
#else
            return EditorUserBuildSettings.activeBuildTarget;
#endif
        }

        private void UpdatePlatformSettings(BuildTarget buildTarget, int index)
        {
            LocalSettings currentSettings = allPlatformSettings[index];

            if (!currentSettings.enabled)
            {
                string name = buildTarget.ToString();
#if UNITY_SERVER
                name = "Server";
#endif
                currentSettings.enabled = true;
                currentSettings.name = name;
            }

            if (platformSettings != currentSettings)
            {
                platformSettings = currentSettings;
            }
        }

#endif
        internal static void AddIdentity(NeutronIdentity identity)
        {
            var key = (identity.id, identity.playerId, identity.isItFromTheServer, identity.sceneId, identity.objectType);
            if (identities.TryGetValue(key, out var existingIdentity))
            {
                Logger.PrintError($"The identity already exists: ID={identity.id}, PlayerID={identity.playerId}, IsFromServer={identity.isItFromTheServer}, SceneID={identity.sceneId}, ObjectType={identity.objectType}");
                return;
            }
            identities.TryAdd(key, identity);
        }

        private static NeutronIdentity GetIdentity(ushort identityId, ushort playerId, bool isServer, byte sceneId, ObjectType objType)
        {
            if (!identities.TryGetValue((identityId, playerId, isServer, sceneId, objType), out var identity))
            {
                Logger.PrintWarning($"Identity not found! -> ID={identityId}, PlayerID={playerId}, IsServer={isServer}, SceneID={sceneId}, ObjectType={objType}");
                return null;
            }

            if (identity == null)
            {
                Logger.PrintWarning($"Identity is null! -> ID={identityId}, PlayerID={playerId}, IsServer={isServer}, SceneID={sceneId}, ObjectType={objType}");
            }

            return identity;
        }

        private static Action<ReadOnlyMemory<byte>, ushort, bool, RemoteStats> GetHandler(byte id)
        {
            if (handlers.TryGetValue(id, out var handler))
            {
                return handler;
            }

            return null;
        }

        public static byte AddHandler<T>(Action<ReadOnlyMemory<byte>, ushort, bool, RemoteStats> handler) where T : IMessage, new()
        {
            T instance = new();
            if (handlers.ContainsKey(instance.Id))
            {
                Logger.PrintError($"Handler for ID={instance.Id} already exists!");
                return instance.Id;
            }

            if (!handlers.TryAdd(instance.Id, handler))
            {
                Logger.PrintError($"Failed to add handler for ID={instance.Id}!");
                return instance.Id;
            }

            using (MemoryStream stream = new())
            {
                try
                {
                    MessagePackSerializer.Serialize(stream, instance);
                }
                catch (Exception ex)
                {
                    Logger.PrintError($"Failed to serialize {typeof(T).Name}: {ex.Message}");
                    Logger.PrintError("It is necessary to generate Ahead-of-Time (AOT) code and register the type resolver.");
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
            var OBJ_TYPE = NeutronHelper.GetObjectType(messageType);
            // Let's process packets with maximum performance!
            // Data must be read in the same order in which they are written.
            switch (messageType)
            {
                case MessageType.Connect:
                    {
                        if (isServer)
                        {
                            Logger.Print($"The endpoint {remoteEndPoint} has been successfully established.");
                        }

                        var ipAddress = new IPAddress(remoteEndPoint.GetIPAddress());
                        var remoteIPEndPoint = new IPEndPoint(ipAddress, remoteEndPoint.GetPort());
                        OnConnected?.Invoke(isServer, remoteIPEndPoint, parameters);
                        break;
                    }
                case MessageType.Remote:
                    {
                        ushort sourceId = parameters.ReadUShort();
                        ushort destinationId = parameters.ReadUShort();
                        byte remoteId = parameters.ReadByte();
                        byte instanceId = parameters.ReadByte();

                        ByteStream message = ByteStream.Get();
                        message.WriteRemainingBytes(parameters);

                        #region Cache System
                        if (isServer)
                        {
#if UNITY_SERVER || UNITY_EDITOR
                            switch (cacheMode)
                            {
                                case CacheMode.Append:
                                    Logger.PrintError("Cache System -> Append is not supported yet. Support will be added in a future update.");
                                    Logger.PrintWarning("Warning: The 'Append' mode in the Cache System is not recommended due to high memory usage and increased bandwidth consumption. This is caused by sending all stored states when using the 'Append' mode. Please use 'Overwrite' mode instead.");
                                    break;
                                case CacheMode.Overwrite:
                                    {
                                        var key = (remoteId, instanceId, sourceId);
                                        if (globalRemoteCache.TryGetValue(key, out NeutronCache cache))
                                        {
                                            cache.SetData(message.Buffer, message.BytesWritten);
                                        }
                                        else
                                        {
                                            byte[] data = new byte[Instance.udpPacketSize];
                                            Buffer.BlockCopy(message.Buffer, 0, data, 0, message.BytesWritten);
                                            NeutronCache newCache = new(data, message.BytesWritten, sourceId, destinationId, 0, 0, remoteId, instanceId, default, channel, default);
                                            if (!globalRemoteCache.TryAdd(key, newCache))
                                            {
                                                Logger.PrintError("Could not create cache, hash key already exists?");
                                            }
                                        }
                                        break;
                                    }
                            }
#endif
                        }
                        #endregion

                        #region Process RPC
                        SubTarget processedSubTarget = NeutronHelper.GetSubTarget(isServer, subTarget);
                        switch (processedSubTarget)
                        {
                            case SubTarget.Server:
                                {
                                    var rpc = NeutronBehaviour.GetRpc(remoteId, instanceId, isServer);
                                    rpc?.Invoke(parameters, sourceId, destinationId, isServer, new RemoteStats(NeutronTime.Time, parameters.BytesRemaining));
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
                        ushort fromId = parameters.ReadUShort();
                        ushort toId = parameters.ReadUShort();
                        byte sceneId = parameters.ReadByte();
                        ushort identityId = parameters.ReadUShort();
                        byte remoteId = parameters.ReadByte();
                        byte instanceId = parameters.ReadByte();

                        ByteStream message = ByteStream.Get();
                        message.WriteRemainingBytes(parameters);
                        ushort PLAYER_ID_OF_IDENTITY = messageType == MessageType.RemoteStatic || messageType == MessageType.RemoteScene ? NeutronHelper.GetPlayerId(isServer) : toId;

                        #region Cache System
                        if (isServer)
                        {
#if UNITY_SERVER || UNITY_EDITOR
                            switch (cacheMode)
                            {
                                case CacheMode.Append:
                                    Logger.PrintError("Cache System -> Append is not supported!");
                                    break;
                                case CacheMode.Overwrite:
                                    {
                                        var key = (remoteId, identityId, instanceId, fromId, sceneId, OBJ_TYPE);
                                        if (remoteCache.TryGetValue(key, out NeutronCache cache))
                                            cache.SetData(message.Buffer, message.BytesWritten);
                                        else
                                        {
                                            byte[] data = new byte[Instance.udpPacketSize];
                                            Buffer.BlockCopy(message.Buffer, 0, data, 0, message.BytesWritten);
                                            NeutronCache remoteCache = new(data, message.BytesWritten, fromId, toId, sceneId, identityId, remoteId, instanceId, messageType, channel, OBJ_TYPE);
                                            if (!NeutronNetwork.remoteCache.TryAdd(key, remoteCache)) Logger.PrintError("Could not create cache, hash key already exists?");
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
                                    NeutronIdentity identity = GetIdentity(identityId, PLAYER_ID_OF_IDENTITY, isServer, sceneId, OBJ_TYPE);
                                    if (identity != null)
                                    {
                                        var rpc = identity.GetRpc(instanceId, remoteId);
                                        rpc?.Invoke(parameters, fromId, toId, new RemoteStats(NeutronTime.Time, parameters.BytesRemaining));
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
                        ushort identityId = parameters.ReadUShort();
                        ushort playerId = parameters.ReadUShort();
                        byte instanceId = parameters.ReadByte();
                        byte sceneId = parameters.ReadByte();

                        ByteStream message = ByteStream.Get();
                        message.WriteRemainingBytes(parameters);
                        ushort PLAYER_ID_OF_IDENTITY = messageType == MessageType.OnSerializeStatic || messageType == MessageType.OnSerializeScene ? NeutronHelper.GetPlayerId(isServer) : playerId;

                        #region Cache System
                        if (isServer)
                        {
#if UNITY_SERVER || UNITY_EDITOR
                            switch (cacheMode)
                            {
                                case CacheMode.Append:
                                    Logger.PrintError("Cache System -> Append is not supported!");
                                    break;
                                case CacheMode.Overwrite:
                                    {
                                        var key = (identityId, instanceId, playerId, sceneId, OBJ_TYPE);
                                        if (serializeCache.TryGetValue(key, out NeutronCache cache))
                                            cache.SetData(message.Buffer, message.BytesWritten);
                                        else
                                        {
                                            byte[] data = new byte[Instance.udpPacketSize];
                                            Buffer.BlockCopy(message.Buffer, 0, data, 0, message.BytesWritten);
                                            NeutronCache serializeCache = new(data, message.BytesWritten, playerId, playerId, sceneId, identityId, 0, instanceId, messageType, channel, OBJ_TYPE);
                                            if (!NeutronNetwork.serializeCache.TryAdd(key, serializeCache)) Logger.PrintError("Could not create cache, hash key already exists?");
                                        }
                                    }
                                    break;
                            }
#endif
                        }
                        #endregion

                        #region Process OnSerializeView
                        switch (NeutronHelper.GetSubTarget(isServer, subTarget))
                        {
                            case SubTarget.Server:
                                {
                                    NeutronIdentity identity = GetIdentity(identityId, PLAYER_ID_OF_IDENTITY, isServer, sceneId, OBJ_TYPE);
                                    if (identity != null)
                                        identity.GetNeutronObject(instanceId).OnSerializeView(parameters, false, new RemoteStats(NeutronTime.Time, parameters.BytesRemaining));
                                    else
                                        Logger.PrintWarning($"The identity has been destroyed or does not exist! -> [IsServer]={isServer} -> [{identityId}, {PLAYER_ID_OF_IDENTITY}, {isServer}]");
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
                        byte varId = parameters.ReadByte();
                        ushort identityId = parameters.ReadUShort();
                        ushort playerId = parameters.ReadUShort();
                        byte instanceId = parameters.ReadByte();
                        byte sceneId = parameters.ReadByte();

                        ByteStream message = ByteStream.Get();
                        message.WriteRemainingBytes(parameters);
                        ushort PLAYER_ID_OF_IDENTITY = messageType == MessageType.OnSyncBaseStatic || messageType == MessageType.OnSyncBaseScene ? NeutronHelper.GetPlayerId(isServer) : playerId;

                        #region Cache System
                        if (isServer)
                        {
#if UNITY_SERVER || UNITY_EDITOR
                            switch (cacheMode)
                            {
                                case CacheMode.Append:
                                    Logger.PrintError("Cache System -> Append is not supported!");
                                    break;
                                case CacheMode.Overwrite:
                                    {
                                        var key = (varId, identityId, instanceId, playerId, sceneId, OBJ_TYPE);
                                        if (syncCache.TryGetValue(key, out NeutronCache cache))
                                            cache.SetData(message.Buffer, message.BytesWritten);
                                        else
                                        {
                                            byte[] data = new byte[Instance.udpPacketSize];
                                            Buffer.BlockCopy(message.Buffer, 0, data, 0, message.BytesWritten);
                                            NeutronCache syncCache = new(data, message.BytesWritten, playerId, playerId, sceneId, identityId, varId, instanceId, messageType, channel, OBJ_TYPE);
                                            if (!NeutronNetwork.syncCache.TryAdd(key, syncCache)) Logger.PrintError("Could not create cache, hash key already exists?");
                                        }
                                    }
                                    break;
                            }
#endif
                        }
                        #endregion

                        #region Process OnSyncBase
                        switch (NeutronHelper.GetSubTarget(isServer, subTarget))
                        {
                            case SubTarget.Server:
                                {
                                    NeutronIdentity identity = GetIdentity(identityId, PLAYER_ID_OF_IDENTITY, isServer, sceneId, OBJ_TYPE);
                                    if (identity != null)
                                        identity.GetNeutronObject(instanceId).OnSyncBase?.Invoke(varId, parameters);
                                    else
                                        Logger.PrintWarning($"The identity has been destroyed or does not exist! -> [IsServer]={isServer} -> [{identityId}, {PLAYER_ID_OF_IDENTITY}, {isServer}]");
                                    break;
                                }
                        }
                        #endregion

                        #region Send
                        ushort fromPort = (ushort)remoteEndPoint.GetPort();
                        if (isServer && fromPort != Port)
                            OnSyncBase(message, varId, identityId, instanceId, playerId, sceneId, isServer, messageType, channel, target, SubTarget.None, cacheMode);
                        #endregion
                    }
                    break;
                case MessageType.GetCache:
                    {
                        CacheType cacheType = (CacheType)parameters.ReadByte();
                        byte id = parameters.ReadByte();
                        bool ownerCache = parameters.ReadBool();
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
                                            NeutronCache cache = ICache.Value;
                                            if (!ownerCache && cache.fromId == fromPort)
                                                continue;
                                            var message = ByteStream.Get();
                                            message.Write(cache.Buffer);
                                            #region Send
                                            if (isServer && fromPort != Port)
                                                Remote(cache.rpcId, cache.sceneId, cache.identityId, cache.instanceId, cache.fromId, cache.toId, isServer, message, cache.messageType, cache.channel, Target.Me, SubTarget.None, CacheMode.None, fromPort);
                                            #endregion
                                        }
                                    }
                                    else Logger.PrintError("There is no cached data!");
                                }
                                break;
                            case CacheType.GlobalRemote:
                                {
                                    var caches = globalRemoteCache.Where(x => x.Key.remoteId == id);
                                    if (caches.Count() != 0)
                                    {
                                        foreach (var ICache in caches)
                                        {
                                            NeutronCache cache = ICache.Value;
                                            if (!ownerCache && cache.fromId == fromPort)
                                                continue;
                                            var message = ByteStream.Get();
                                            message.Write(cache.Buffer);
                                            #region Send
                                            if (isServer && fromPort != Port)
                                                Remote(cache.rpcId, cache.instanceId, cache.fromId, cache.toId, isServer, message, cache.channel, Target.Me, SubTarget.None, CacheMode.None, fromPort);
                                            #endregion
                                        }
                                    }
                                    else Logger.PrintError("There is no cached data!");
                                }
                                break;
                            case CacheType.OnSerialize:
                                {
                                    if (serializeCache.Count() != 0)
                                    {
                                        foreach (var ICache in serializeCache)
                                        {
                                            NeutronCache cache = ICache.Value;
                                            if (!ownerCache && cache.fromId == fromPort)
                                                continue;
                                            var message = ByteStream.Get();
                                            message.Write(cache.Buffer);
                                            #region Send
                                            if (isServer && fromPort != Port)
                                                OnSerializeView(message, cache.identityId, cache.instanceId, cache.toId, cache.sceneId, isServer, cache.messageType, cache.channel, Target.Me, SubTarget.None, CacheMode.None, fromPort);
                                            #endregion
                                        }
                                    }
                                    else Logger.PrintError("There is no cached data!");
                                }
                                break;
                            case CacheType.OnSync:
                                {
                                    var caches = syncCache.Where(x => x.Key.varId == id);
                                    if (caches.Count() != 0)
                                    {
                                        foreach (var ICache in caches)
                                        {
                                            NeutronCache cache = ICache.Value;
                                            if (!ownerCache && cache.fromId == fromPort)
                                                continue;
                                            var message = ByteStream.Get();
                                            message.Write(cache.Buffer);
                                            #region Send
                                            if (isServer && fromPort != Port)
                                                OnSyncBase(message, cache.rpcId, cache.identityId, cache.instanceId, cache.toId, cache.sceneId, isServer, cache.messageType, cache.channel, Target.Me, SubTarget.None, CacheMode.None, fromPort);
                                            #endregion
                                        }
                                    }
                                    else Logger.PrintError("There is no cached data!");
                                }
                                break;
                            case CacheType.GlobalMessage:
                                {
                                    var caches = globalCache.Where(x => x.Key.id == id);
                                    if (caches.Count() != 0)
                                    {
                                        foreach (var ICache in caches)
                                        {
                                            NeutronCache cache = ICache.Value;
                                            if (!ownerCache && cache.fromId == fromPort)
                                                continue;
                                            var message = ByteStream.Get();
                                            message.Write(cache.Buffer);
                                            #region Send
                                            if (isServer && fromPort != Port)
                                                GlobalMessage(message, cache.rpcId, cache.toId, isServer, cache.channel, Target.Me, SubTarget.None, CacheMode.None, fromPort);
                                            #endregion
                                        }
                                    }
                                    else Logger.PrintError("There is no cached data!");
                                }
                                break;
                            case CacheType.LocalMessage:
                                {
                                    var caches = localCache.Where(x => x.Key.id == id);
                                    if (caches.Count() != 0)
                                    {
                                        foreach (var ICache in caches)
                                        {
                                            NeutronCache cache = ICache.Value;
                                            if (!ownerCache && cache.fromId == fromPort)
                                                continue;
                                            var message = ByteStream.Get();
                                            message.Write(cache.Buffer);
                                            #region Send
                                            if (isServer && fromPort != Port)
                                                LocalMessage(message, cache.rpcId, cache.identityId, cache.instanceId, cache.toId, cache.sceneId, isServer, cache.messageType, cache.channel, Target.Me, SubTarget.None, CacheMode.None, fromPort);
                                            #endregion
                                        }
                                    }
                                    else Logger.PrintError("There is no cached data!");
                                }
                                break;
                        }
                    }
                    break;
                case MessageType.GlobalMessage:
                    {
                        byte id = parameters.ReadByte();
                        ushort playerId = parameters.ReadUShort();

                        ByteStream message = ByteStream.Get();
                        message.WriteRemainingBytes(parameters);

                        #region Cache System
                        if (isServer)
                        {
#if UNITY_SERVER || UNITY_EDITOR
                            switch (cacheMode)
                            {
                                case CacheMode.Append:
                                    Logger.PrintError("Cache System -> Append is not supported!");
                                    break;
                                case CacheMode.Overwrite:
                                    {
                                        var key = (id, playerId);
                                        if (globalCache.TryGetValue(key, out NeutronCache cache))
                                            cache.SetData(message.Buffer, message.BytesWritten);
                                        else
                                        {
                                            byte[] data = new byte[Instance.udpPacketSize];
                                            Buffer.BlockCopy(message.Buffer, 0, data, 0, message.BytesWritten);
                                            NeutronCache globalCache = new(data, message.BytesWritten, playerId, playerId, 0, 0, id, 0, default, channel, default);
                                            if (!NeutronNetwork.globalCache.TryAdd(key, globalCache)) Logger.PrintError("Could not create cache, hash key already exists?");
                                        }
                                    }
                                    break;
                            }
#endif
                        }
                        #endregion

                        #region Execute Message
                        switch (NeutronHelper.GetSubTarget(isServer, subTarget))
                        {
                            case SubTarget.Server:
                                {
                                    var handler = GetHandler(id);
                                    handler?.Invoke(parameters.ReadAsReadOnlyMemory(), playerId, isServer, new RemoteStats(NeutronTime.Time, parameters.BytesRemaining));
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
                        byte id = parameters.ReadByte();
                        ushort identityId = parameters.ReadUShort();
                        ushort playerId = parameters.ReadUShort();
                        byte instanceId = parameters.ReadByte();
                        byte sceneId = parameters.ReadByte();

                        ByteStream message = ByteStream.Get();
                        message.WriteRemainingBytes(parameters);
                        ushort PLAYER_ID_OF_IDENTITY = messageType == MessageType.LocalMessageStatic || messageType == MessageType.LocalMessageScene ? NeutronHelper.GetPlayerId(isServer) : playerId;

                        #region Cache System
                        if (isServer)
                        {
#if UNITY_SERVER || UNITY_EDITOR
                            switch (cacheMode)
                            {
                                case CacheMode.Append:
                                    Logger.PrintError("Cache System -> Append is not supported!");
                                    break;
                                case CacheMode.Overwrite:
                                    {
                                        var key = (id, identityId, instanceId, playerId, sceneId, OBJ_TYPE);
                                        if (localCache.TryGetValue(key, out NeutronCache cache))
                                            cache.SetData(message.Buffer, message.BytesWritten);
                                        else
                                        {
                                            byte[] data = new byte[Instance.udpPacketSize];
                                            Buffer.BlockCopy(message.Buffer, 0, data, 0, message.BytesWritten);
                                            NeutronCache localCache = new(data, message.BytesWritten, playerId, playerId, sceneId, identityId, id, instanceId, messageType, channel, OBJ_TYPE);
                                            if (!NeutronNetwork.localCache.TryAdd(key, localCache)) Logger.PrintError("Could not create cache, hash key already exists?");
                                        }
                                    }
                                    break;
                            }
#endif
                        }
                        #endregion

                        #region Execute Message
                        switch (NeutronHelper.GetSubTarget(isServer, subTarget))
                        {
                            case SubTarget.Server:
                                {
                                    NeutronIdentity identity = GetIdentity(identityId, PLAYER_ID_OF_IDENTITY, isServer, sceneId, NeutronHelper.GetObjectType(messageType));
                                    if (identity != null)
                                    {
                                        NeutronObject @this = identity.GetNeutronObject(instanceId);
                                        var handler = @this.GetHandler(id);
                                        handler?.Invoke(parameters.ReadAsReadOnlyMemory(), playerId, isServer, new RemoteStats(NeutronTime.Time, parameters.BytesRemaining));
                                    }
                                    else
                                        Logger.PrintWarning($"The identity has been destroyed or does not exist! -> [IsServer]={isServer} -> [{identityId}, {PLAYER_ID_OF_IDENTITY}, {isServer}]");
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
                if (NeutronTime.LocalTime - lastTime >= frequency)
                {
                    lastTime = NeutronTime.LocalTime;
                    return true;
                }
                else return false;
            }
            else
            {
                if (NeutronTime.Time - lastTime >= frequency)
                {
                    lastTime = NeutronTime.Time;
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
            SendDataViaSocket(message, NeutronHelper.GetPlayerId(senderId, toId), fromServer, channel, target, subTarget, cacheMode);
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
            SendDataViaSocket(message, NeutronHelper.GetPlayerId(senderId, toId), fromServer, channel, target, subTarget, cacheMode);
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
            SendDataViaSocket(message, NeutronHelper.GetPlayerId(senderId, playerId), fromServer, channel, target, subTarget, cacheMode);
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
            SendDataViaSocket(message, NeutronHelper.GetPlayerId(senderId, playerId), fromServer, channel, target, subTarget, cacheMode);
            message.Release();
        }

        public static void GetCache(CacheType cacheType, bool ownerCache, byte cacheId, bool fromServer, Channel channel) => GetCache(cacheType, ownerCache, cacheId, NeutronHelper.GetPlayerId(fromServer), fromServer, channel);
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
            SendDataViaSocket(message, NeutronHelper.GetPlayerId(senderId, playerId), fromServer, channel, target, subTarget, cacheMode);
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
            SendDataViaSocket(message, NeutronHelper.GetPlayerId(senderId, playerId), fromServer, channel, target, subTarget, cacheMode);
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
            Logger.PrintError("Fire Event not work on client side!");
#endif
        }

        internal static void ClearAllCaches(ushort playerId)
        {
            globalCache.RemoveAll(x => x.playerId == playerId);
            globalRemoteCache.RemoveAll(x => x.playerId == playerId);
            localCache.RemoveAll(x => x.playerId == playerId);
            remoteCache.RemoveAll(x => x.playerId == playerId);
            serializeCache.RemoveAll(x => x.playerId == playerId);
            syncCache.RemoveAll(x => x.playerId == playerId);
        }

        public static Player GetPlayerFromServer(ushort playerId)
        {
            Player player = udpServer.GetClient(playerId).Player;
            if (player == null)
                Logger.PrintError("Player not found!");
            return player;
        }

        public static Player GetPlayerFromClient(ushort playerId)
        {
            Player player = udpClient.GetClient(playerId).Player;
            if (player == null)
                Logger.PrintError("Player not found!");
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