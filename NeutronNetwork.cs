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
using MessagePack;
using MessagePack.Resolvers;
using Neutron.Resolvers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neutron.Core
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-0x64)]
    public class NeutronNetwork : MonoBehaviour
    {
        private static Dictionary<int, Action<ByteStream>> handlers = new();
        private static UdpServer udpServer = new();
        private static UdpClient udpClient = new();

        #region Events
        public static event Action<bool> OnConnected;
        #endregion

        static IFormatterResolver FormatterResolver;
        public static void AddResolver(IFormatterResolver resolver = null)
        {
            FormatterResolver = resolver == null
                ? (resolver = CompositeResolver.Create(NeutronResolver.Instance, MessagePack.Unity.Extension.UnityBlitResolver.Instance, MessagePack.Unity.UnityResolver.Instance, StandardResolver.Instance))
                : (resolver = CompositeResolver.Create(resolver, FormatterResolver));
            MessagePackSerializer.DefaultOptions = MessagePackSerializerOptions.Standard.WithResolver(resolver);
        }

        [SerializeField] private int targetFrameRate = 60;
        private void Awake()
        {
            AddResolver(null);
            DontDestroyOnLoad(this);
#if UNITY_SERVER
            Console.Clear();
            Console.WriteLine("Neutron Network is being initialized...");
#endif
            Application.targetFrameRate = targetFrameRate;
            var remoteEndPoint = new UdpEndPoint(IPAddress.Any, 5055);
#if UNITY_SERVER || UNITY_EDITOR
            udpServer.Bind(remoteEndPoint);
#endif
#if UNITY_SERVER
            Console.WriteLine("Neutron Network is ready!");
#endif
#if !UNITY_SERVER || UNITY_EDITOR
            udpClient.Bind(new UdpEndPoint(IPAddress.Any, Helper.GetFreePort()));
            udpClient.Connect(new UdpEndPoint(IPAddress.Loopback, remoteEndPoint.GetPort()));
#endif
#if UNITY_SERVER || UNITY_EDITOR
            SceneManager.CreateScene("Server", new CreateSceneParameters(LocalPhysicsMode.None));
#endif
        }

        public static void AddHandler<T>(Action<ByteStream> handler) where T : ISerializable, new()
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
                        if (handlers.TryGetValue(id, out Action<ByteStream> action)) action(recvStream);
                        else Logger.PrintError($"Handler for {id} not found!");
                    }
                    break;
                case MessageType.Test:
                    Logger.PrintError("Test: " + recvStream.ReadInt());
                    if (!isServer)
                        return;
                    udpServer.Send(recvStream, channel, target, remoteEndPoint);
                    break;
            }
        }

        public static void Message(ByteStream byteStream, Channel channel = Channel.Unreliable, Target target = Target.Me, int playerId = 0)
        {
            Send(byteStream, playerId, channel, target);
        }

        internal static void iRPC(ByteStream byteStream, Channel channel = Channel.Unreliable, Target target = Target.Me, int playerId = 0)
        {

        }

        private void OnApplicationQuit()
        {
            udpClient.Close();
            udpServer.Close();
        }
    }
}