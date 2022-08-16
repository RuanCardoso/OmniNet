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
using System.Net.Sockets;

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
            SendReliableMessages(this.remoteEndPoint);
        }

        internal void Connect(UdpEndPoint remoteEndPoint)
        {
            SendReliableMessages(remoteEndPoint);
            this.remoteEndPoint = remoteEndPoint;
            ByteStream connStream = ByteStream.Get();
            connStream.WritePacket(MessageType.Connect);
            Send(connStream, Channel.Reliable, Target.Me);
            connStream.Release();
        }

        internal int Send(ByteStream byteStream) => Send(byteStream, remoteEndPoint, 0);
        internal void Send(ByteStream byteStream, Channel channel = Channel.Unreliable, Target target = Target.Me)
        {
            if (remoteEndPoint == null)
                throw new System.Exception("You must call Connect() before Send()");

            switch (channel)
            {
                case Channel.Unreliable:
                    SendUnreliable(byteStream, remoteEndPoint, target);
                    break;
                case Channel.Reliable:
                    SendReliable(byteStream, remoteEndPoint, Channel.Reliable, target);
                    break;
                case Channel.ReliableAndOrderly:
                    SendReliable(byteStream, remoteEndPoint, Channel.ReliableAndOrderly, target);
                    break;
            }
        }

        protected override void OnMessage(ByteStream recvStream, Channel channel, Target target, MessageType messageType, UdpEndPoint remoteEndPoint)
        {
            switch (messageType)
            {
                case MessageType.Connect:
                    Id = recvStream.ReadUShort();
                    IsConnected = true;
                    NeutronNetwork.OnMessage(recvStream, messageType, channel, target, remoteEndPoint, IsServer);
                    break;
                default:
                    NeutronNetwork.OnMessage(recvStream, messageType, channel, target, remoteEndPoint, IsServer);
                    break;
            }
        }

        internal override UdpClient GetClient(UdpEndPoint remoteEndPoint) => this;
    }
}