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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace Neutron.Core
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-0x64)]
    public class NeutronNetwork : ActionDispatcher
    {
        #region Confs
        internal const int WINDOW_SIZE = byte.MaxValue * 8;
        #endregion

        private static readonly Dictionary<int, Action<ByteStream, bool>> handlers = new();
        private static readonly UdpServer udpServer = new();
        private static readonly UdpClient udpClient = new();

        #region Events
        public static event Action<bool> OnConnected;
        #endregion

        #region Properties
        internal static NeutronNetwork Instance { get; private set; }
        public static int Id => udpClient.Id;
        #endregion

        #region Compiler Options
        [SerializeField][Header("[COMPILER OPTIONS]")] private bool AGRESSIVE_RELAY = false;
        [SerializeField] private bool MULTI_THREADED = false;
        [SerializeField] private bool LOCK_FPS = true;
#if NEUTRON_LOCK_FPS || !NEUTRON_MULTI_THREADED
        [Header("[RUNTIME OPTIONS]")]
#endif
#if NEUTRON_LOCK_FPS
        [SerializeField] private int MAX_FPS = 60;
#endif
#if !NEUTRON_MULTI_THREADED
        [SerializeField][Min(1)] internal int RECV_MULTIPLIER = 1;
#endif
        #endregion
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
            AddResolver(null);
            DontDestroyOnLoad(this);
            Instance = this;
#if NEUTRON_LOCK_FPS
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = MAX_FPS;
#endif
            var remoteEndPoint = new UdpEndPoint(IPAddress.Any, 5055);
#if UNITY_SERVER || UNITY_EDITOR
            udpServer.Bind(remoteEndPoint);
#endif
#if !UNITY_SERVER || UNITY_EDITOR
            udpClient.Bind(new UdpEndPoint(IPAddress.Any, Helper.GetFreePort()));
            udpClient.Connect(new UdpEndPoint(IPAddress.Loopback, remoteEndPoint.GetPort()));
#endif
#if UNITY_SERVER || UNITY_EDITOR
            SceneManager.CreateScene("Server", new CreateSceneParameters(LocalPhysicsMode.None));
#endif
        }

        private void Start()
        {
            Invoke(nameof(Main), 0.3f);
            if (!GarbageCollector.isIncremental)
                Logger.PrintWarning("Tip: Enable \"Incremental GC\" for maximum performance!");
        }

        private void Main()
        {
#if UNITY_SERVER && !UNITY_EDITOR
            Console.Clear();
            StartCoroutine(GetKeyConsole());
#endif
        }

        private IEnumerator GetKeyConsole()
        {
            Dictionary<string, string> dict = new();
            //********************************************************
            Logger.Print("Press 'Enter' to write a command!");
            Logger.Print("Ex: Ban -user Ruan -days 300");
            Logger.Print("Press 'ESC' to exit!");
            //********************************************************
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    switch (key)
                    {
                        case ConsoleKey.Enter:
                            Logger.Print("Write the command:");
                            //**********************************
                            string command = Console.ReadLine();
                            dict.Clear();
                            switch (command)
                            {
                                case "Clear":
                                case "clear":
                                    Console.Clear();
                                    break;
                                case "GC Collect":
                                case "gc collect":
                                    GC.Collect();
                                    Logger.Print("Collected");
                                    break;
                                case "Memory":
                                case "memory":
                                    long totalBytesOfMemoryUsed = GC.GetTotalMemory(false);
                                    Logger.Print($"Allocated managed memory: {totalBytesOfMemoryUsed.ToSize(SizeUnits.MB)} MB | {totalBytesOfMemoryUsed.ToSize(SizeUnits.GB)} GB");
                                    break;
                                default:
                                    {
                                        if (!string.IsNullOrEmpty(command))
                                        {
                                            int paramsCount = 0;
                                            string[][] parameters = command.Split('-').Select(x => x.Split()).ToArray();
                                            if (parameters.Length <= 1) Logger.Print("Continuous execution without parameters!");
                                            else
                                            {
                                                for (int i = 1; i < parameters.Length; i++)
                                                {
                                                    if (parameters[i].InBounds(0) && parameters[i].InBounds(1))
                                                    {
                                                        string parameter = parameters[i][0];
                                                        string value = parameters[i][1];
                                                        //*****************************************************************
                                                        if (string.IsNullOrEmpty(parameter) || string.IsNullOrEmpty(value))
                                                            Logger.Print("Continuous execution without parameters!");
                                                        else
                                                        {
                                                            paramsCount++;
                                                            if (!dict.TryAdd(parameter, value))
                                                                dict[parameter] = value;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Logger.PrintError("Invalid parameters!");
                                                        yield return null;
                                                        break;
                                                    }
                                                }
                                            }

                                            command = parameters[0][0];
                                            //**************************************************************
                                            Logger.Print($"Command executed: '{command}' | parameter count: {paramsCount}");
                                        }
                                        else
                                        {
                                            Logger.PrintError("There are no commands!");
                                            yield return null;
                                            continue;
                                        }
                                    }
                                    break;
                            }
                            break;
                        case ConsoleKey.Escape:
                            Logger.Print("Exiting...");
                            OnApplicationQuit();
                            Application.Quit(0);
                            break;
                        default:
                            Logger.Print($"There is no command for the '{key}' key");
                            break;
                    }
                }
                else
                {
                    yield return null;
                    continue;
                }

                yield return null;
            }
        }

        internal void InternDispatch(Action action) => Dispatch(action);
#if UNITY_EDITOR
        [ContextMenu("Set Compiler Options")]
        private void SetCompilerOptions()
        {
            List<string> defines = new();
            if (!LOCK_FPS) defines.Add("NEUTRON_LOCK_FPS_REMOVED");
            else defines.Add("NEUTRON_LOCK_FPS");
            if (!MULTI_THREADED) defines.Add("NEUTRON_MULTI_THREADED_REMOVED");
            else defines.Add("NEUTRON_MULTI_THREADED");
            if (!AGRESSIVE_RELAY) defines.Add("NEUTRON_AGRESSIVE_RELAY_REMOVED");
            else defines.Add("NEUTRON_AGRESSIVE_RELAY");
            Helper.SetDefine(defines: defines.ToArray());
        }
        private void OnValidate() => SetCompilerOptions();
#endif

        public static void AddHandler<T>(Action<ByteStream, bool> handler) where T : ISerializable, new()
        {
            T instance = new();
            if (!handlers.TryAdd(instance.Id, handler))
                Logger.PrintError($"Handler for {instance.Id} already exists!");
        }

        private static void Send(ByteStream byteStream, int playerId, Channel channel, Target target)
        {
            if (playerId != 0) udpServer.Send(byteStream, channel, target, playerId);
            else udpClient.Send(byteStream, channel, target);
        }

        internal static void OnMessage(ByteStream recvStream, MessageType messageType, Channel channel, Target target, UdpEndPoint remoteEndPoint, bool isServer)
        {
            switch (messageType)
            {
                case MessageType.Connect:
                    OnConnected?.Invoke(isServer);
                    break;
                case MessageType.GlobalMessage:
                    {
                        int id = recvStream.ReadInt();
                        if (handlers.TryGetValue(id, out Action<ByteStream, bool> handler))
                        {
                            ByteStream messageStream = ByteStream.Get();
                            messageStream.Write(recvStream, recvStream.Position, recvStream.BytesWritten);
                            handler(messageStream, isServer);
                            messageStream.Release();
                            if (!isServer)
                                return;
                            udpServer.Send(recvStream, channel, target, remoteEndPoint);
                        }
                        else Logger.PrintError($"Handler for {id} not found!");
                    }
                    break;
                case MessageType.StressTest:
                    {
                        int indx = recvStream.ReadInt();
                        Logger.PrintError($"Stress Test! -> {isServer}" + indx);
                        if (!isServer)
                            return;
                        ByteStream stream = ByteStream.Get();
                        stream.WritePacket(MessageType.StressTest);
                        stream.Write(indx);
                        udpServer.Send(stream, channel, target, remoteEndPoint);
                        stream.Release();
                    }
                    break;
            }
        }

        public static void Send(ByteStream byteStream, Channel channel = Channel.Unreliable, Target target = Target.Me, int playerId = 0)
        {
            Send(byteStream, playerId, channel, target);
            byteStream.Release();
        }

        internal static void iRPC(ByteStream byteStream, MessageType msgType, Channel channel = Channel.Unreliable, Target target = Target.Me, int playerId = 0)
        {
            ByteStream iRPCStream = ByteStream.Get();
            iRPCStream.WritePacket(msgType);
            iRPCStream.Write(byteStream);
            byteStream.Release();
            Send(iRPCStream, playerId, channel, target);
            iRPCStream.Release();
        }

        public static void Spawn(ByteStream byteStream, NeutronIdentity prefab, Vector3 position = default, Quaternion rotation = default, bool immediate = true, Channel channel = Channel.Unreliable, Target target = Target.Me, int playerId = 0)
        {
            // if (immediate)
            // {
            //     NeutronIdentity go = Instantiate(prefab, position, rotation);
            // }

            // ByteStream instantiateStream = ByteStream.Get();
            // instantiateStream.WritePacket(MessageType.Instantiate);
            // instantiateStream.Write(byteStream);
            // byteStream.Release();
            // Send(instantiateStream, playerId, channel, target);
            // instantiateStream.Release();
        }

        private void OnApplicationQuit()
        {
            udpClient.Close();
            udpServer.Close();
        }
    }
}