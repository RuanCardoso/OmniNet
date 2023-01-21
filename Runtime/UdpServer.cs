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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static Neutron.Core.Enums;
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

        protected override void OnMessage(ByteStream RECV_STREAM, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode, MessageType messageType, UdpEndPoint remoteEndPoint)
        {
            switch (messageType)
            {
                case MessageType.Disconnect:
                    Disconnect(remoteEndPoint);
                    break;
                case MessageType.Connect:
                    {
                        ushort uniqueId = (ushort)remoteEndPoint.GetPort();
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
                            NeutronNetwork.OnMessage(RECV_STREAM, messageType, channel, target, subTarget, cacheMode, remoteEndPoint, IsServer);
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
                    {
                        UdpClient client = GetClient(remoteEndPoint);
                        if (client != null)
                        {
                            client.lastTimeReceivedPing = NeutronTime.LocalTime;
                            double timeOfClient = RECV_STREAM.ReadDouble();
                            ByteStream stream = ByteStream.Get(messageType);
                            stream.Write(timeOfClient);
                            stream.Write(NeutronTime.LocalTime);
                            client.Send(stream, channel, target);
                            stream.Release();
                        }
                        else Logger.PrintError("A ping attempt on a disconnected client!");
                    }
                    break;
                default:
                    NeutronNetwork.OnMessage(RECV_STREAM, messageType, channel, target, subTarget, cacheMode, remoteEndPoint, IsServer);
                    break;
            }
        }

        internal override UdpClient GetClient(UdpEndPoint remoteEndPoint) => GetClient((ushort)remoteEndPoint.GetPort());
        internal UdpClient GetClient(ushort playerId) => clients.TryGetValue(playerId, out UdpClient udpClient) ? udpClient : null;

        internal void Send(ByteStream byteStream, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode, ushort playerId) => Send(byteStream, channel, target, subTarget, cacheMode, GetClient(playerId));
        internal void Send(ByteStream byteStream, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode, UdpEndPoint remoteEndPoint) => Send(byteStream, channel, target, subTarget, cacheMode, GetClient(remoteEndPoint));
        internal void Send(ByteStream byteStream, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode, UdpClient sender)
        {
            // When we send the data to itself, we will always use the Unreliable channel.
            // LocalHost(Loopback), there are no risks of drops or clutter.
            if (sender != null)
            {
                switch (target)
                {
                    case Target.Me:
                        {
                            if (subTarget == SubTarget.Server) // Defines whether to execute the instruction on the server when the server itself is the sender.
                                SendUnreliable(byteStream, Client.remoteEndPoint, target, subTarget, cacheMode);

                            if (!sender.itSelf)
                            {
                                if (!byteStream.isRawBytes)
                                    sender.Send(byteStream, channel, target, subTarget, cacheMode);
                                else
                                    sender.Send(byteStream);
                            }
                            else if (subTarget != SubTarget.Server)
                                Logger.PrintError("Are you trying to run the instruction on yourself? Use SubTarget.Server");
                        }
                        break;
                    case Target.All:
                        {
                            if (subTarget == SubTarget.Server)
                                SendUnreliable(byteStream, Client.remoteEndPoint, target, subTarget, cacheMode);

                            foreach (var (_, otherClient) in clients)
                            {
                                if (otherClient.itSelf)
                                    continue;
                                else
                                {
                                    if (!byteStream.isRawBytes)
                                        otherClient.Send(byteStream, channel, target, subTarget, cacheMode);
                                    else
                                        otherClient.Send(byteStream);
                                }
                            }
                        }
                        break;
                    case Target.Others:
                        {
                            if (subTarget == SubTarget.Server)
                                SendUnreliable(byteStream, Client.remoteEndPoint, target, subTarget, cacheMode);

                            foreach (var (id, otherClient) in clients)
                            {
                                if (otherClient.itSelf)
                                    continue;
                                else
                                {
                                    if (id != sender.Id)
                                    {
                                        if (!byteStream.isRawBytes)
                                            otherClient.Send(byteStream, channel, target, subTarget, cacheMode);
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

        private bool RemoveClient(ushort uniqueId, out UdpClient disconnected)
        {
#if !NEUTRON_MULTI_THREADED
            return clients.Remove(uniqueId, out disconnected);
#else
            return clients.TryRemove(uniqueId, out disconnected);
#endif
        }

        internal override void Close(bool fromServer = false)
        {
            base.Close();
            foreach (var udpClient in clients.Values)
                udpClient.Close();
        }

        protected override void Disconnect(UdpEndPoint endPoint)
        {
            ushort uniqueId = (ushort)endPoint.GetPort();
            if (RemoveClient(uniqueId, out UdpClient disconnected))
            {
                NeutronNetwork.ClearAllCaches(uniqueId);
                disconnected.Close(true);
                Logger.Print($"The endpoint {endPoint} has been disconnected.");
            }
            else Logger.PrintError("Failed to disconnect client!");
        }

        internal IEnumerator CheckTheLastReceivedPing(double maxPingRequestTime)
        {
            while (true)
            {
                UdpClient[] clients = this.clients.Values.ToArray();
                for (int i = 0; i < clients.Length; i++)
                {
                    UdpClient client = clients[i];
                    if (client.itSelf) continue;
                    if ((NeutronTime.LocalTime - client.lastTimeReceivedPing) >= maxPingRequestTime)
                        Disconnect(client.remoteEndPoint);
                    else continue;
                }

                yield return NeutronNetwork.WAIT_FOR_CHECK_REC_PING;
            }
        }
    }
}