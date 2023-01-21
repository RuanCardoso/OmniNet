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
using System.Linq;
#endif
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
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

        private static int frameCount = 0;
        private static float deltaTime = 0f;

        private static readonly Dictionary<(ushort, ushort, bool, byte, ObjectType), NeutronIdentity> identities = new(); // [Identity Id, Player Id, IsServer, Scene Id, Object Type]
        private static readonly Dictionary<int, Action<ReadOnlyMemory<byte>, ushort, bool, RemoteStats>> handlers = new();
        private static readonly UdpServer udpServer = new();
        private static readonly UdpClient udpClient = new();

        #region Events
        public static event Action<bool, IPEndPoint, ByteStream> OnConnected;
        #endregion

        internal static NeutronNetwork Instance { get; private set; }

        #region Properties
        public static float Framerate { get; private set; }
        public static float CpuMs { get; private set; }
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
        [SerializeField][Range(0.01f, 60f)][Label("Ping")] private float pingTime = 1f; // seconds
        [SerializeField][Range(0.1f, 5f)][Label("Reconnection")] private float reconnectionTime = 3f; // seconds
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
        [SerializeField][ReadOnly] private string[] defined;
        #endregion

        //* Multi-Threading
        //internal static double timeAsDouble;

        [SerializeField][HideInInspector] private LocalSettings[] allPlatformSettings = new LocalSettings[SETTINGS_SIZE];
        [Header("Plataforms")][SerializeField] internal LocalSettings platformSettings;

        private ActionDispatcher dispatcher;

        internal static WaitForSeconds WAIT_FOR_CONNECT;
        internal static WaitForSeconds WAIT_FOR_PING;
        internal static WaitForSeconds WAIT_FOR_CHECK_REC_PING;

        private static readonly Dictionary<(byte remoteId, ushort identityId, byte instanceId, ushort playerId, byte sceneId, ObjectType objectType), NeutronCache> remoteCache = new(); // [Remote Id, Identity Id, Instance Id, Player Id, Scene Id, Object Type]
        private static readonly Dictionary<(byte remoteId, byte instanceId, ushort playerId), NeutronCache> globalRemoteCache = new(); // [Remote Id, Player Id]
        private static readonly Dictionary<(ushort identityId, byte instanceId, ushort playerId, byte sceneId, ObjectType objectType), NeutronCache> serializeCache = new(); // [Identity Id, Instance Id, Player Id, Scene Id, Object Type]
        private static readonly Dictionary<(byte varId, ushort identityId, byte instanceId, ushort playerId, byte sceneId, ObjectType objectType), NeutronCache> syncCache = new(); // [Var Id, Identity Id, Instance Id, Player Id, Scene Id, Object Type]
        private static readonly Dictionary<(byte id, ushort playerId), NeutronCache> globalCache = new(); // [Id, Player Id]
        private static readonly Dictionary<(byte id, ushort identityId, byte instanceId, ushort playerId, byte sceneId, ObjectType objectType), NeutronCache> localCache = new(); // [id, Identity Id, Instance Id, Player Id, Scene Id, Object Type]

        public static IFormatterResolver Formatter { get; private set; }
        public static MessagePackSerializerOptions AddResolver(IFormatterResolver resolver = null, [CallerMemberName] string _ = "")
        {
            if (_ != "Awake")
                Logger.PrintError($"{nameof(AddResolver)} must be called from Awake!");
            else
            {
                Formatter = resolver == null
                    ? (resolver = CompositeResolver.Create(UnityBlitWithPrimitiveArrayResolver.Instance, UnityResolver.Instance, StandardResolver.Instance))
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
            //----------------------------------------------------
            WAIT_FOR_CONNECT = new(reconnectionTime);
            WAIT_FOR_PING = new(pingTime);
            WAIT_FOR_CHECK_REC_PING = new(pingSweepTime);
            //----------------------------------------------------
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
                if (!isServer)
                {
                    SceneManager.LoadScene(nextIndex, LoadSceneMode.Additive);
                    OnConnected -= NeutronNetwork_OnConnected;
                }
#else
                SceneManager.LoadScene(nextIndex, LoadSceneMode.Additive);
                OnConnected -= NeutronNetwork_OnConnected;
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
                Framerate = frameCount / deltaTime;
                CpuMs = deltaTime / frameCount * 1000f;
                deltaTime = 0f;
                frameCount = 0;
            }

            //* Multi-Threading
            //timeAsDouble = Time.timeAsDouble;
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
            defined = NeutronHelper.GetDefines(out _).Where(x => x.StartsWith("NEUTRON_")).ToArray();
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

        private static NeutronIdentity GetIdentity(ushort identityId, ushort playerId, bool isServer, byte sceneId, ObjectType objType)
        {
            if (!identities.TryGetValue((identityId, playerId, isServer, sceneId, objType), out NeutronIdentity identity))
                Logger.PrintWarning($"Indentity not found! -> [IsServer]={isServer}");
            return identity;
        }

        private static Action<ReadOnlyMemory<byte>, ushort, bool, RemoteStats> GetHandler(byte id) => handlers.TryGetValue(id, out var handler) ? handler : null;
        public static byte AddHandler<T>(Action<ReadOnlyMemory<byte>, ushort, bool, RemoteStats> handler) where T : IMessage, new()
        {
            T instance = new();
            if (!handlers.TryAdd(instance.Id, handler))
                Logger.PrintError($"Handler for {instance.Id} already exists!");
            else
            {
                try
                {
                    MessagePackSerializer.Serialize(instance);
                }
                catch (Exception ex)
                {
                    ex = ex.InnerException;
                    Logger.PrintError(ex.Message);
                    Logger.PrintError("It is necessary to generate the AOT code and register the type.");
                }
            }

            return instance.Id;
        }

        private static void ThrowErrorIfWrongSocket(bool fromServer)
        {
#if UNITY_SERVER && !UNITY_EDITOR
            if (!fromServer)
                throw new Exception("The server cannot send data through the client socket!");
#elif !UNITY_SERVER && !UNITY_EDITOR
            if (fromServer)
                throw new Exception("The client cannot send data through the server socket!");
#endif
        }

        private static void Intern_Send(ByteStream byteStream, ushort id, bool fromServer, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode)
        {
            ThrowErrorIfWrongSocket(fromServer);
            if (fromServer && IsBind) udpServer.Send(byteStream, channel, target, subTarget, cacheMode, id);
            else udpClient.Send(byteStream, channel, target, subTarget, cacheMode);
        }

        private static void Intern_Send(ByteStream byteStream, UdpEndPoint remoteEndPoint, bool fromServer, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode)
        {
            ThrowErrorIfWrongSocket(fromServer);
            if (fromServer && IsBind) udpServer.Send(byteStream, channel, target, subTarget, cacheMode, remoteEndPoint);
            else udpClient.Send(byteStream, channel, target, subTarget, cacheMode);
        }

        internal static void OnMessage(ByteStream parameters, MessageType messageType, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode, UdpEndPoint remoteEndPoint, bool isServer)
        {
            var OBJ_TYPE = NeutronHelper.GetObjectType(messageType);
            // Let's process packets with maximum performance!
            // Data must be read in the same order in which they are written.
            switch (messageType)
            {
                case MessageType.Connect:
                    if (isServer)
                        Logger.Print($"The endpoint {remoteEndPoint} has been established.");
                    OnConnected?.Invoke(isServer, new IPEndPoint(new IPAddress(remoteEndPoint.GetIPAddress()), remoteEndPoint.GetPort()), parameters);
                    break;
                case MessageType.Remote:
                    {
                        ushort fromId = parameters.ReadUShort();
                        ushort toId = parameters.ReadUShort();
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
                                    Logger.PrintError("Cache System -> Append is not supported!");
                                    break;
                                case CacheMode.Overwrite:
                                    {
                                        var key = (remoteId, instanceId, fromId);
                                        if (globalRemoteCache.TryGetValue(key, out NeutronCache cache))
                                            cache.SetData(message.Buffer, message.BytesWritten);
                                        else
                                        {
                                            byte[] data = new byte[Instance.udpPacketSize];
                                            Buffer.BlockCopy(message.Buffer, 0, data, 0, message.BytesWritten);
                                            NeutronCache globalRemoteCache = new(data, message.BytesWritten, fromId, toId, 0, 0, remoteId, instanceId, default, channel, default);
                                            if (!NeutronNetwork.globalRemoteCache.TryAdd(key, globalRemoteCache)) Logger.PrintError("Could not create cache, hash key already exists?");
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
                                    var rpc = NeutronBehaviour.GetRpc(remoteId, instanceId, isServer);
                                    rpc?.Invoke(parameters, fromId, toId, isServer, new RemoteStats(NeutronTime.Time, parameters.BytesRemaining));
                                }
                                break;
                        }
                        #endregion

                        #region Send
                        ushort fromPort = (ushort)remoteEndPoint.GetPort();
                        if (isServer && fromPort != Port)
                            Remote(remoteId, instanceId, fromId, toId, isServer, message, channel, target, SubTarget.None, cacheMode);
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
            Intern_Send(message, NeutronHelper.GetPlayerId(senderId, toId), fromServer, channel, target, subTarget, cacheMode);
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
            Intern_Send(message, NeutronHelper.GetPlayerId(senderId, toId), fromServer, channel, target, subTarget, cacheMode);
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
            Intern_Send(message, NeutronHelper.GetPlayerId(senderId, playerId), fromServer, channel, target, subTarget, cacheMode);
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
            Intern_Send(message, NeutronHelper.GetPlayerId(senderId, playerId), fromServer, channel, target, subTarget, cacheMode);
            message.Release();
        }

        public static void GetCache(CacheType cacheType, bool ownerCache, byte cacheId, bool fromServer, Channel channel) => GetCache(cacheType, ownerCache, cacheId, NeutronHelper.GetPlayerId(fromServer), fromServer, channel);
        public static void GetCache(CacheType cacheType, bool ownerCache, byte cacheId, ushort playerId, bool fromServer, Channel channel)
        {
            ByteStream message = ByteStream.Get(MessageType.GetCache);
            message.Write((byte)cacheType);
            message.Write(cacheId);
            message.Write(ownerCache);
            Intern_Send(message, playerId, fromServer, channel, Target.Me, SubTarget.None, CacheMode.None);
            message.Release();
        }

        internal static void GlobalMessage(ByteStream msg, byte id, ushort playerId, bool fromServer, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode, ushort senderId = 0)
        {
            ByteStream message = ByteStream.Get(MessageType.GlobalMessage);
            message.Write(id);
            message.Write(playerId);
            message.Write(msg);
            msg.Release();
            Intern_Send(message, NeutronHelper.GetPlayerId(senderId, playerId), fromServer, channel, target, subTarget, cacheMode);
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
            Intern_Send(message, NeutronHelper.GetPlayerId(senderId, playerId), fromServer, channel, target, subTarget, cacheMode);
            message.Release();
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

        public static Player GetPlayer(ushort playerId, bool isServer = true) => isServer ? udpServer.GetClient(playerId).Player : udpClient.Player;

        internal void OnApplicationQuit()
        {
            udpClient.Disconnect();
            udpClient.Close();
            udpServer.Close();
        }
    }
}