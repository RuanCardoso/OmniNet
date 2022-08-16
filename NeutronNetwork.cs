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
using System.Net;
using UnityEngine;

namespace Neutron.Core
{
    [DefaultExecutionOrder(-0x64)]
    internal class NeutronNetwork : MonoBehaviour
    {
        internal static NeutronNetwork Instance { get; private set; }
        internal static ByteStreamPool ByteStreams = new();
        private static UdpServer Server { get; set; }
        private UdpServer udpServer = new UdpServer();
        private UdpClient udpClient = new UdpClient();
        public static event Action<bool> OnConnected;

        [SerializeField] private int targetFrameRate = 60;
        private void Awake()
        {
            Instance = this;
#if UNITY_SERVER
            Console.Clear();
            Console.WriteLine("Neutron Network is being initialized...");
#endif
            Application.targetFrameRate = targetFrameRate;
        }

        private void Start()
        {
            var remoteEndPoint = new UdpEndPoint(IPAddress.Any, 5055);
#if UNITY_SERVER || UNITY_EDITOR
            udpServer.Bind(remoteEndPoint);
#endif
#if !UNITY_SERVER || UNITY_EDITOR
            udpClient.Bind(new UdpEndPoint(IPAddress.Any, Helper.GetFreePort()));
            /*-------------------------------------------------------------------------------*/
            udpClient.Connect(new UdpEndPoint(IPAddress.Loopback, remoteEndPoint.GetPort()));
#endif
#if UNITY_SERVER
            Console.WriteLine("Neutron Network is ready!");
#endif
#if UNITY_SERVER || UNITY_EDITOR
            Server = udpServer;
#endif
        }

        int value;
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ByteStream byteStream = ByteStream.Get();
                byteStream.WritePacket(MessageType.Test);
                byteStream.Write(++value);
                udpClient.Send(byteStream, Channel.Unreliable, Target.Me);
                byteStream.Release();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                ByteStream byteStream = ByteStream.Get();
                byteStream.WritePacket(MessageType.Test);
                byteStream.Write(++value);
                udpClient.Send(byteStream, Channel.Reliable, Target.Me);
                byteStream.Release();
            }
        }

        internal static void OnMessage(ByteStream recvStream, MessageType messageType, Channel channel, Target target, UdpEndPoint remoteEndPoint, bool isServer)
        {
            switch (messageType)
            {
                case MessageType.Connect:
                    OnConnected?.Invoke(isServer);
                    break;
                case MessageType.Test:
                    Logger.Print("Test: " + recvStream.ReadInt());
                    if (!isServer)
                        return;

                    ByteStream testStream = ByteStream.Get();
                    testStream.WritePacket(MessageType.Test);
                    testStream.Write(10);
                    Server.SendToTarget(testStream, channel, target, remoteEndPoint);
                    testStream.Release();
                    break;
            }
        }

        private void OnApplicationQuit()
        {
            udpClient.Close();
            udpServer.Close();
        }
    }
}