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

using System.Collections.Concurrent;
using System.Net;

namespace Neutron.Core
{
    internal sealed class UdpServer : UdpSocket
    {
        protected override string Name => "Neutron_Server";
        protected override bool IsServer => true;
        private ConcurrentDictionary<int, UdpClient> udpClients = new();

        protected override void OnMessage(ByteStream recvStream, Channel channel, Target target, MessageType messageType, UdpEndPoint remoteEndPoint)
        {
            int uniqueId = remoteEndPoint.GetPort();
            switch (messageType)
            {
                case MessageType.Connect:
                    {
                        UdpClient newClient = new(remoteEndPoint, globalSocket);
                        if (udpClients.TryAdd(uniqueId, newClient))
                        {
                            ByteStream connStream = ByteStream.Get();
                            connStream.WritePacket(MessageType.Connect);
                            connStream.Write((ushort)uniqueId);
                            newClient.Send(connStream, Channel.Reliable, target);
                            connStream.Release();
                            NeutronNetwork.OnMessage(recvStream, messageType, channel, target, remoteEndPoint, IsServer);
                        }
                        else newClient.Close(true);
                    }
                    break;
                default:
                    NeutronNetwork.OnMessage(recvStream, messageType, channel, target, remoteEndPoint, IsServer);
                    break;
            }
        }

        internal override UdpClient GetClient(UdpEndPoint remoteEndPoint)
        {
            if (udpClients.TryGetValue(remoteEndPoint.GetPort(), out UdpClient udpClient))
                return udpClient;
            return null;
        }

        internal void SendToTarget(ByteStream byteStream, Channel channel, Target target, UdpEndPoint remoteEndPoint, bool sendRawBytes = false)
        {
            switch (target)
            {
                case Target.Me:
                    {
                        UdpClient udpClient = GetClient(remoteEndPoint);
                        if (udpClient != null)
                        {
                            if (!sendRawBytes) udpClient.Send(byteStream, channel, target);
                            else udpClient.Send(byteStream);
                        }
                        else throw new System.Exception("Target is not connected!");
                    }
                    break;
                case Target.All:
                    {
                        foreach (var (id, udpClient) in udpClients)
                        {
                            if (!sendRawBytes) udpClient.Send(byteStream, channel, target);
                            else udpClient.Send(byteStream);
                        }
                    }
                    break;
                case Target.Others:
                    {
                        foreach (var (id, udpClient) in udpClients)
                        {
                            if (id != remoteEndPoint.GetPort())
                            {
                                if (!sendRawBytes) udpClient.Send(byteStream, channel, target);
                                else udpClient.Send(byteStream);
                            }
                        }
                    }
                    break;
                case Target.Server:
                    break;
                default:
                    throw new System.Exception("Invalid target!");
            }
        }

        internal override void Close(bool fromServer = false)
        {
            base.Close();
            foreach (var udpClient in udpClients.Values)
                udpClient.Close();
        }
    }
}