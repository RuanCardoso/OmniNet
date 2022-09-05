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

using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using MessagePack;
using MessagePack.Resolvers;
using Neutron.Resolvers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neutron.Core
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-0x64)]
    public class NeutronNetwork : ActionDispatcher
    {
        private static NeutronNetwork instance;
        private static Dictionary<int, Action<ByteStream, bool>> handlers = new();
        private static UdpServer udpServer = new();
        private static UdpClient udpClient = new();

        #region Events
        public static event Action<bool> OnConnected;
        #endregion

        #region Properties
        public static int Id => udpClient.Id;
        #endregion

        public static IFormatterResolver Formatter { get; private set; }
        public static MessagePackSerializerOptions AddResolver(IFormatterResolver resolver = null, [CallerMemberName] string _ = "")
        {
            if (_ != "Awake")
                Logger.PrintError($"AddResolver must be called from Awake");
            else
            {
                Formatter = resolver == null
                    ? (resolver = CompositeResolver.Create(NeutronRuntimeResolver.Instance, MessagePack.Unity.Extension.UnityBlitWithPrimitiveArrayResolver.Instance, MessagePack.Unity.UnityResolver.Instance, StandardResolver.Instance))
                    : (resolver = CompositeResolver.Create(resolver, Formatter));
                return MessagePackSerializer.DefaultOptions = MessagePackSerializerOptions.Standard.WithResolver(resolver);
            }
            return MessagePackSerializer.DefaultOptions;
        }

        [SerializeField] private int targetFrameRate = 60;
        private void Awake()
        {
            AddResolver(null);
            DontDestroyOnLoad(this);
            instance = this;
            Application.targetFrameRate = targetFrameRate;
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
#if UNITY_SERVER
            Console.Clear();
#endif
        }

        public static void AddHandler<T>(Action<ByteStream, bool> handler) where T : ISerializable, new()
        {
            T instance = new T();
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
                    Logger.PrintError("Stress Test!");
                    if (!isServer)
                        return;
                    udpServer.Send(recvStream, channel, target, remoteEndPoint);
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