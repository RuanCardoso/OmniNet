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

using System.Collections;
using System.Net.Sockets;
using UnityEngine;

namespace Neutron.Core
{
    internal sealed class UdpClient : UdpSocket
    {
        internal int Id { get; private set; }
        internal bool IsConnected { get; private set; }
        protected override string Name => "Neutron_Client";
        protected override bool IsServer => false;
        internal UdpEndPoint remoteEndPoint;

        internal UdpClient() { }
        internal UdpClient(UdpEndPoint remoteEndPoint, Socket socket)
        {
            IsConnected = true;
            this.globalSocket = socket;
            this.remoteEndPoint = new(remoteEndPoint.GetIPAddress(), remoteEndPoint.GetPort()); // copy endpoint to avoid reference problems!
#if NEUTRON_MULTI_THREADED
            //SendReliableMessages(this.remoteEndPoint);
#else
            NeutronNetwork.Instance.StartCoroutine(SendReliableMessages(this.remoteEndPoint));
#endif
        }

        internal void Connect(UdpEndPoint remoteEndPoint)
        {
            this.remoteEndPoint = remoteEndPoint;
#if NEUTRON_MULTI_THREADED
            Relay(this.remoteEndPoint);
#else
            NeutronNetwork.Instance.StartCoroutine(SendReliableMessages(this.remoteEndPoint));
#endif
            NeutronNetwork.Instance.StartCoroutine(Connect());
        }

        WaitForSeconds yieldConnect = new(1f);
        private IEnumerator Connect()
        {
            while (!IsConnected)
            {
                ByteStream stream = ByteStream.Get();
                stream.WritePacket(MessageType.Connect);
                Send(stream, Channel.Unreliable, Target.Me);
                stream.Release();
                yield return yieldConnect;
            }
        }

        internal int Send(ByteStream byteStream) => Send(byteStream, remoteEndPoint, 0);
        internal int Send(ByteStream byteStream, Channel channel = Channel.Unreliable, Target target = Target.Me)
        {
            if (remoteEndPoint == null)
                Logger.PrintError("You must call Connect() before Send()");
            else
            {
                switch (channel)
                {
                    case Channel.Unreliable:
                        return SendUnreliable(byteStream, remoteEndPoint, target);
                    case Channel.Reliable:
                        return SendReliable(byteStream, remoteEndPoint, channel, target);
                    default:
                        return 0;
                }
            }
            return 0;
        }

        protected override void OnMessage(ByteStream recvStream, Channel channel, Target target, MessageType messageType, UdpEndPoint remoteEndPoint)
        {
            switch (messageType)
            {
                case MessageType.Connect:
                    {
                        if (!IsConnected)
                        {
                            Id = recvStream.ReadUShort();
                            IsConnected = true;
                            NeutronNetwork.OnMessage(recvStream, messageType, channel, target, remoteEndPoint, IsServer);
                        }
                    }
                    break;
                default:
                    NeutronNetwork.OnMessage(recvStream, messageType, channel, target, remoteEndPoint, IsServer);
                    break;
            }
        }

        internal override UdpClient GetClient(UdpEndPoint remoteEndPoint) => this;
    }
}