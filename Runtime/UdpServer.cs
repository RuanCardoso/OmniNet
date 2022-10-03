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

#if NEUTRON_MULTI_THREADED
using System.Collections.Concurrent;
#else
using System.Collections.Generic;
#endif

namespace Neutron.Core
{
    internal sealed class UdpServer : UdpSocket
    {
        protected override string Name => "Neutron_Server";
        protected override bool IsServer => true;

#if NEUTRON_MULTI_THREADED
        private readonly ConcurrentDictionary<int, UdpClient> clients = new();
#else
        private readonly Dictionary<int, UdpClient> clients = new();
#endif
        protected override void OnMessage(ByteStream recvStream, Channel channel, Target target, MessageType messageType, UdpEndPoint remoteEndPoint)
        {
            int uniqueId = remoteEndPoint.GetPort();
            switch (messageType)
            {
                case MessageType.Connect:
                    {
                        UdpClient _client_ = new(remoteEndPoint, globalSocket);
                        if (clients.TryAdd(uniqueId, _client_))
                        {
                            #region Response
                            ByteStream stream = ByteStream.Get(messageType);
                            stream.Write((ushort)uniqueId);
                            _client_.Send(stream, channel, target);
                            stream.Release();
                            #endregion

                            #region Process Message
                            NeutronNetwork.OnMessage(recvStream, messageType, channel, target, remoteEndPoint, IsServer);
                            #endregion
                        }
                        else
                        {
                            Logger.Print("Unreliable -> Previous connection attempt failed, re-establishing connection.");
                            #region Response
                            ByteStream stream = ByteStream.Get(messageType);
                            stream.Write((ushort)uniqueId);
                            UdpClient connectedClient = GetClient(remoteEndPoint);
                            if (connectedClient != null) connectedClient.Send(stream, channel, target);
                            else Logger.PrintError("Connect -> Client is null!");
                            stream.Release();
                            #endregion
                            _client_.Close(true);
                        }
                    }
                    break;
                case MessageType.Ping:
                    UdpClient client = GetClient(remoteEndPoint);
                    if (client != null)
                    {
                        double timeOfClient = recvStream.ReadDouble();
                        ByteStream stream = ByteStream.Get(messageType);
                        stream.Write(timeOfClient);
                        stream.Write(NeutronTime.LocalTime);
                        client.Send(stream, channel, target);
                        stream.Release();
                    }
                    else Logger.PrintError("Ping -> Client is null!");
                    break;
                default:
                    NeutronNetwork.OnMessage(recvStream, messageType, channel, target, remoteEndPoint, IsServer);
                    break;
            }
        }

        internal override UdpClient GetClient(UdpEndPoint remoteEndPoint) => GetClient(remoteEndPoint.GetPort());
        internal UdpClient GetClient(int playerId) => clients.TryGetValue(playerId, out UdpClient udpClient) ? udpClient : null;

        internal void Send(ByteStream byteStream, Channel channel, Target target, int playerId) => Send(byteStream, channel, target, GetClient(playerId));
        internal void Send(ByteStream byteStream, Channel channel, Target target, UdpEndPoint remoteEndPoint) => Send(byteStream, channel, target, GetClient(remoteEndPoint));
        internal void Send(ByteStream byteStream, Channel channel, Target target, UdpClient sender)
        {
            if (sender == null)
                Logger.PrintError("Sender is null!");
            else
            {
                switch (target)
                {
                    case Target.Me:
                        {
                            if (!byteStream.isRawBytes) sender.Send(byteStream, channel, target);
                            else sender.Send(byteStream);
                        }
                        break;
                    case Target.All:
                        {
                            foreach (var (_, udpClient) in clients)
                            {
                                if (!byteStream.isRawBytes) udpClient.Send(byteStream, channel, target);
                                else udpClient.Send(byteStream);
                            }
                        }
                        break;
                    case Target.Others:
                        {
                            foreach (var (id, udpClient) in clients)
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
                        Logger.PrintError($"Invalid target -> {target}");
                        break;
                }
            }
        }

        internal override void Close(bool fromServer = false)
        {
            base.Close();
            foreach (var udpClient in clients.Values)
                udpClient.Close();
        }
    }
}