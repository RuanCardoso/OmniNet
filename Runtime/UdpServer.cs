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

#if OMNI_MULTI_THREADED
using System.Collections.Concurrent;
#else
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static Omni.Core.Enums;
#endif

namespace Omni.Core
{
    internal sealed class UdpServer : UdpSocket
    {
        protected override string Name => "Omni_Server";
        protected override bool IsServer => true;

#if OMNI_MULTI_THREADED
        private readonly ConcurrentDictionary<ushort, UdpClient> clients = new();
#else
        private readonly Dictionary<ushort, UdpClient> clients = new();
#endif
        internal UdpClient Client { get; private set; }
        internal void Initialize(ushort playerId)
        {
            Client = new UdpClient(true);
            clients.TryAdd(playerId, Client);
        }

        protected override void OnMessage(ByteStream RECV_STREAM, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode, MessageType messageType, UdpEndPoint remoteEndPoint)
        {
            switch (messageType)
            {
                case MessageType.Disconnect:
                    Disconnect(remoteEndPoint, "Info: The endpoint {0} has been successfully disconnected.");
                    break;
                case MessageType.Connect:
                    {
                        ushort uniqueId = (ushort)remoteEndPoint.GetPort();
                        if (uniqueId == OmniNetwork.Port)
                        {
                            OmniLogger.LogWarning($"Warning: Client connection denied. The port is in exclusive use -> {OmniNetwork.Port}");
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
                            OmniNetwork.OnMessage(RECV_STREAM, messageType, channel, target, subTarget, cacheMode, remoteEndPoint, IsServer);
                            #endregion
                        }
                        else
                        {
                            OmniLogger.Print("Unreliable -> Previous connection attempt failed, re-establishing connection.");
                            #region Response
                            ByteStream stream = ByteStream.Get(messageType);
                            stream.Write(uniqueId);
                            UdpClient connectedClient = GetClient(remoteEndPoint);
                            if (connectedClient != null)
                            {
                                connectedClient.Send(stream, channel, target);
                            }
                            else
                            {
                                OmniLogger.PrintError("Connect -> Client is null!");
                            }
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
                            client.lastTimeReceivedPing = OmniTime.LocalTime;
                            double timeOfClient = RECV_STREAM.ReadDouble();
                            ByteStream stream = ByteStream.Get(messageType);
                            stream.Write(timeOfClient);
                            stream.Write(OmniTime.LocalTime);
                            client.Send(stream, channel, target);
                            stream.Release();
                        }
                        else
                        {
                            OmniLogger.PrintError("Error: Attempted to ping a disconnected client!");
                        }
                    }
                    break;
                default:
                    OmniNetwork.OnMessage(RECV_STREAM, messageType, channel, target, subTarget, cacheMode, remoteEndPoint, IsServer);
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
                            {
                                SendUnreliable(byteStream, Client.remoteEndPoint, target, subTarget, cacheMode);
                            }

                            if (!sender.itSelf)
                            {
                                if (!byteStream.isRawBytes)
                                {
                                    sender.Send(byteStream, channel, target, subTarget, cacheMode);
                                }
                                else
                                {
                                    sender.Send(byteStream);
                                }
                            }
                            else if (subTarget != SubTarget.Server)
                            {
                                OmniLogger.PrintError("Are you trying to execute this instruction on yourself? Please use SubTarget.Server or verify if 'IsMine' is being used correctly.");
                            }
                        }
                        break;
                    case Target.All:
                        {
                            if (subTarget == SubTarget.Server)
                            {
                                SendUnreliable(byteStream, Client.remoteEndPoint, target, subTarget, cacheMode);
                            }

                            foreach (var (_, otherClient) in clients)
                            {
                                if (otherClient.itSelf)
                                    continue;
                                else
                                {
                                    if (!byteStream.isRawBytes)
                                    {
                                        otherClient.Send(byteStream, channel, target, subTarget, cacheMode);
                                    }
                                    else
                                    {
                                        otherClient.Send(byteStream);
                                    }
                                }
                            }
                        }
                        break;
                    case Target.Others:
                        {
                            if (subTarget == SubTarget.Server)
                            {
                                SendUnreliable(byteStream, Client.remoteEndPoint, target, subTarget, cacheMode);
                            }

                            foreach (var (id, otherClient) in clients)
                            {
                                if (otherClient.itSelf)
                                    continue;
                                else
                                {
                                    if (id != sender.Id)
                                    {
                                        if (!byteStream.isRawBytes)
                                        {
                                            otherClient.Send(byteStream, channel, target, subTarget, cacheMode);
                                        }
                                        else
                                        {
                                            otherClient.Send(byteStream);
                                        }
                                    }
                                    else continue;
                                }
                            }
                        }
                        break;
                    case Target.Server:
                        break;
                    default:
                        OmniLogger.PrintError($"Invalid target -> {target}");
                        break;
                }
            }
            else
            {
                OmniLogger.PrintError("Error: Client not found. Ensure that the client is not mistakenly sending data through the server socket.");
            }
        }

        private bool RemoveClient(ushort uniqueId, out UdpClient disconnected)
        {
#if !OMNI_MULTI_THREADED
            return clients.Remove(uniqueId, out disconnected);
#else
            return clients.TryRemove(uniqueId, out disconnected);
#endif
        }

        internal override void Close(bool fromServer = false)
        {
            base.Close();
            foreach (var udpClient in clients.Values)
            {
                udpClient.Close();
            }
        }

        protected override void Disconnect(UdpEndPoint endPoint, string msg = "")
        {
            ushort uniqueId = (ushort)endPoint.GetPort();
            if (RemoveClient(uniqueId, out UdpClient disconnected))
            {
                OmniLogger.Print(string.Format(msg, endPoint));
                Dictionaries.ClearDataCache(uniqueId);
                disconnected.Close(true);
            }
            else
            {
                OmniLogger.PrintError("Error: Failed to disconnect the client.");
            }
        }

        internal IEnumerator CheckTheLastReceivedPing(double maxPingRequestTime)
        {
            while (true)
            {
                UdpClient[] clients = this.clients.Values.ToArray();
                for (int i = 0; i < clients.Length; i++)
                {
                    UdpClient client = clients[i];
                    if (client.itSelf)
                    {
                        continue;
                    }
                    else
                    {
                        if ((OmniTime.LocalTime - client.lastTimeReceivedPing) >= maxPingRequestTime)
                        {
                            Disconnect(client.remoteEndPoint, "Info: The endpoint {0} has been disconnected due to lack of response from the client.");
                        }
                        else
                        {
                            continue;
                        }
                    }
                }

                yield return OmniNetwork.WAIT_FOR_CHECK_REC_PING;
            }
        }
    }
}