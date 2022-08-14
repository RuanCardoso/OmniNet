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

        protected override void OnMessage(ByteStream byteStream, MessageType messageType, UdpEndPoint remoteEndPoint)
        {
            int uniqueId = remoteEndPoint.GetPort();
            switch (messageType)
            {
                case MessageType.Connect:
                    {
                        UdpClient udpClient = new(remoteEndPoint, globalSocket);
                        if (udpClients.TryAdd(uniqueId, udpClient))
                        {
                            ByteStream connStream = NeutronNetwork.ByteStreams.Get();
                            connStream.WritePacket(MessageType.Connect);
                            connStream.Write((ushort)uniqueId);
                            udpClient.Send(connStream, Channel.Reliable);
                            NeutronNetwork.ByteStreams.Release(connStream);
                        }
                        else
                            udpClient.Close(true);
                    }
                    break;
            }
        }

        protected override UdpClient GetClient(int port)
        {
            if (udpClients.TryGetValue(port, out UdpClient udpClient))
                return udpClient;
            return null;
        }

        internal override void Close(bool fromServer = false)
        {
            base.Close();
            foreach (var udpClient in udpClients.Values)
                udpClient.Close();
        }
    }
}