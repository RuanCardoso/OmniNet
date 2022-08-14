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

using System.Net.Sockets;

namespace Neutron.Core
{
    internal sealed class UdpClient : UdpSocket
    {
        protected override string Name => "Neutron_Client";
        protected override bool IsServer => false;
        internal UdpEndPoint remoteEndPoint;

        internal UdpClient() { }
        internal UdpClient(UdpEndPoint remoteEndPoint, Socket socket)
        {
            this.globalSocket = socket;
            this.remoteEndPoint = new(remoteEndPoint.GetIPAddress(), remoteEndPoint.GetPort()); // copy endpoint to avoid reference problems!
            SendReliableMessages(this.remoteEndPoint);
        }

        internal void Connect(UdpEndPoint remoteEndPoint)
        {
            SendReliableMessages(remoteEndPoint);
            this.remoteEndPoint = remoteEndPoint;
            ByteStream connStream = NeutronNetwork.ByteStreams.Get();
            connStream.WritePacket(MessageType.Connect);
            Send(connStream, Channel.Reliable);
            NeutronNetwork.ByteStreams.Release(connStream);
        }

        internal void Send(ByteStream byteStream, Channel channel)
        {
            if (remoteEndPoint == null)
                throw new System.Exception("You must call Connect() before Send()");
            switch (channel)
            {
                case Channel.Unreliable:
                    SendUnreliable(byteStream, remoteEndPoint);
                    break;
                case Channel.Reliable:
                    SendReliable(byteStream, remoteEndPoint);
                    break;
                case Channel.ReliableAndOrderly:
                    SendReliableAndOrderly(byteStream, remoteEndPoint);
                    break;
            }
        }

        protected override void OnMessage(ByteStream byteStream, MessageType messageType, UdpEndPoint remoteEndPoint)
        {
            switch (messageType)
            {
                case MessageType.Connect:
                    ushort uniqueId = byteStream.ReadUShort();
                    Logger.Log($"Connected to {remoteEndPoint.GetIPAddress()}:{remoteEndPoint.GetPort()} with uniqueId {uniqueId}");
                    break;
            }
        }

        protected override UdpClient GetClient(int port) => throw new System.NotImplementedException();
    }
}