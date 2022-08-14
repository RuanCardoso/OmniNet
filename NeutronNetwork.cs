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
        public static ByteStreamPool ByteStreams = new();
        private UdpServer udpServer = new UdpServer();
        private UdpClient udpClient = new UdpClient();

        private void Awake()
        {
#if UNITY_SERVER
            Console.Clear();
            Console.WriteLine("Neutron Network is being initialized...");
#endif
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
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ByteStream byteStream = ByteStreams.Get();
                byteStream.WritePacket(MessageType.Test);
                udpClient.Send(byteStream, Channel.Unreliable);
                ByteStreams.Release(byteStream);
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                ByteStream byteStream = ByteStreams.Get();
                byteStream.WritePacket(MessageType.Test);
                udpClient.Send(byteStream, Channel.Reliable);
                ByteStreams.Release(byteStream);
            }
        }

        private void OnApplicationQuit()
        {
            udpClient.Close();
            udpServer.Close();
        }
    }
}