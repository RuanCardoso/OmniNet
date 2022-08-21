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
                        UdpClient _client_ = new(remoteEndPoint, globalSocket);
                        if (udpClients.TryAdd(uniqueId, _client_))
                        {
                            ByteStream connStream = ByteStream.Get();
                            connStream.WritePacket(MessageType.Connect);
                            connStream.Write((ushort)uniqueId);
                            _client_.Send(connStream, Channel.Reliable, target);
                            connStream.Release();
                            NeutronNetwork.OnMessage(recvStream, messageType, channel, target, remoteEndPoint, IsServer);
                        }
                        else _client_.Close(true);
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
            else return null;
        }

        internal UdpClient GetClient(int playerId)
        {
            if (udpClients.TryGetValue(playerId, out UdpClient udpClient))
                return udpClient;
            else return null;
        }

        internal void SendToTarget(ByteStream byteStream, Channel channel, Target target, int playerId) => SendToTarget(byteStream, channel, target, GetClient(playerId));
        internal void SendToTarget(ByteStream byteStream, Channel channel, Target target, UdpEndPoint remoteEndPoint) => SendToTarget(byteStream, channel, target, GetClient(remoteEndPoint.GetPort()));
        internal void SendToTarget(ByteStream byteStream, Channel channel, Target target, UdpClient sender)
        {
            switch (target)
            {
                case Target.Me:
                    {
                        if (sender != null)
                        {
                            if (!byteStream.isRawBytes) sender.Send(byteStream, channel, target);
                            else sender.Send(byteStream);
                        }
                        else throw new System.Exception("Target is not connected!");
                    }
                    break;
                case Target.All:
                    {
                        foreach (var (id, udpClient) in udpClients)
                        {
                            if (!byteStream.isRawBytes) udpClient.Send(byteStream, channel, target);
                            else udpClient.Send(byteStream);
                        }
                    }
                    break;
                case Target.Others:
                    {
                        foreach (var (id, udpClient) in udpClients)
                        {
                            if (id != sender.remoteEndPoint.GetPort())
                            {
                                if (!byteStream.isRawBytes) udpClient.Send(byteStream, channel, target);
                                else udpClient.Send(byteStream);
                            }
                            else continue;
                        }
                    }
                    break;
                case Target.Server:
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