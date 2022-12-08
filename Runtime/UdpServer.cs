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
using System.Diagnostics;
using System.Net;
#endif

namespace Neutron.Core
{
    internal sealed class UdpServer : UdpSocket
    {
        protected override string Name => "Neutron_Server";
        protected override bool IsServer => true;

#if NEUTRON_MULTI_THREADED
        private readonly ConcurrentDictionary<ushort, UdpClient> clients = new();
#else
        private readonly Dictionary<ushort, UdpClient> clients = new();
#endif
        internal UdpClient Client { get; private set; }
        internal void CreateServerPlayer(ushort playerId)
        {
            Client = new UdpClient(true);
            clients.TryAdd(playerId, Client);
        }

        protected override void OnMessage(ByteStream RECV_STREAM, Channel channel, Target target, MessageType messageType, UdpEndPoint remoteEndPoint)
        {
            ushort uniqueId = (ushort)remoteEndPoint.GetPort();
            switch (messageType)
            {
                case MessageType.Connect:
                    {
                        if (uniqueId == NeutronNetwork.Port)
                        {
                            Logger.LogWarning($"Client denied! Exclusive port -> {NeutronNetwork.Port}");
                            return;
                        }

                        UdpClient _client_ = new(remoteEndPoint, globalSocket);
                        if (clients.TryAdd(uniqueId, _client_))
                        {
                            #region Response
                            ByteStream stream = ByteStream.Get(messageType);
                            stream.Write(uniqueId);
                            _client_.Send(stream, channel, target);
                            stream.Release();
                            #endregion

                            #region Process Message
                            NeutronNetwork.OnMessage(RECV_STREAM, messageType, channel, target, remoteEndPoint, IsServer);
                            #endregion
                        }
                        else
                        {
                            Logger.Print("Unreliable -> Previous connection attempt failed, re-establishing connection.");
                            #region Response
                            ByteStream stream = ByteStream.Get(messageType);
                            stream.Write(uniqueId);
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
                        double timeOfClient = RECV_STREAM.ReadDouble();
                        ByteStream stream = ByteStream.Get(messageType);
                        stream.Write(timeOfClient);
                        stream.Write(NeutronTime.LocalTime);
                        client.Send(stream, channel, target);
                        stream.Release();
                    }
                    else Logger.PrintError("Ping -> Client is null!");
                    break;
                default:
                    NeutronNetwork.OnMessage(RECV_STREAM, messageType, channel, target, remoteEndPoint, IsServer);
                    break;
            }
        }

        internal override UdpClient GetClient(UdpEndPoint remoteEndPoint) => GetClient((ushort)remoteEndPoint.GetPort());
        internal UdpClient GetClient(ushort playerId) => clients.TryGetValue(playerId, out UdpClient udpClient) ? udpClient : null;

        internal void Send(ByteStream byteStream, Channel channel, Target target, SubTarget subTarget, ushort playerId) => Send(byteStream, channel, target, subTarget, GetClient(playerId));
        internal void Send(ByteStream byteStream, Channel channel, Target target, SubTarget subTarget, UdpEndPoint remoteEndPoint) => Send(byteStream, channel, target, subTarget, GetClient(remoteEndPoint));
        internal void Send(ByteStream byteStream, Channel channel, Target target, SubTarget subTarget, UdpClient sender)
        {
            // When we send the data to itself, we will always use the Unreliable channel.
            // LocalHost(Loopback), there are no risks of drops or clutter.
            if (sender != null)
            {
                switch (target)
                {
                    case Target.Me:
                        {
                            if (subTarget == SubTarget.Server)
                                SendUnreliable(byteStream, Client.remoteEndPoint, target);

                            if (!sender.itSelf)
                            {
                                if (!byteStream.isRawBytes)
                                    sender.Send(byteStream, channel, target);
                                else
                                    sender.Send(byteStream);
                            }
                        }
                        break;
                    case Target.All:
                        {
                            if (subTarget == SubTarget.Server)
                                SendUnreliable(byteStream, Client.remoteEndPoint, target);

                            foreach (var (_, otherClient) in clients)
                            {
                                if (otherClient.itSelf)
                                    continue;
                                else
                                {
                                    if (!byteStream.isRawBytes)
                                        otherClient.Send(byteStream, channel, target);
                                    else
                                        otherClient.Send(byteStream);
                                }
                            }
                        }
                        break;
                    case Target.Others:
                        {
                            if (subTarget == SubTarget.Server)
                                SendUnreliable(byteStream, Client.remoteEndPoint, target);

                            foreach (var (id, otherClient) in clients)
                            {
                                if (otherClient.itSelf)
                                    continue;
                                else
                                {
                                    if (id != sender.Id)
                                    {
                                        if (!byteStream.isRawBytes)
                                            otherClient.Send(byteStream, channel, target);
                                        else
                                            otherClient.Send(byteStream);
                                    }
                                    else continue;
                                }
                            }
                        }
                        break;
                    case Target.Server:
                        break;
                    default:
                        Logger.PrintError($"Invalid target -> {target}");
                        break;
                }
            }
            else Logger.PrintError("Client not found! -> Check that the client is not sending the data using the server socket?");
        }

        internal override void Close(bool fromServer = false)
        {
            base.Close();
            foreach (var udpClient in clients.Values)
                udpClient.Close();
        }
    }
}