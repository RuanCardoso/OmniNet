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

        protected override void OnMessage(DataIOHandler IOHandler, DataDeliveryMode deliveryMode, DataTarget target, DataProcessingOption processingOption, DataCachingOption cachingOption, MessageType messageType, UdpEndPoint remoteEndPoint)
        {
            switch (messageType)
            {
                case MessageType.Disconnect:
                    {
                        Disconnect(remoteEndPoint, "Info: The endpoint {0} has been successfully disconnected.");

                        // Chama o evento OnMessage do OmniNetwork com os par�metros fornecidos
                        OmniNetwork.OnMessage(IOHandler, messageType, deliveryMode, target, processingOption, cachingOption, remoteEndPoint, IsServer);
                    }
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
                            DataIOHandler _IOHandler_ = DataIOHandler.Get(messageType);
                            _IOHandler_.Write(uniqueId);
                            _client_.Send(_IOHandler_, deliveryMode, target);
                            _IOHandler_.Release();
                            #endregion

                            // Chama o evento OnMessage do OmniNetwork com os par�metros fornecidos
                            OmniNetwork.OnMessage(IOHandler, messageType, deliveryMode, target, processingOption, cachingOption, remoteEndPoint, IsServer);
                        }
                        else
                        {
                            OmniLogger.Print("Unreliable -> Previous connection attempt failed, re-establishing connection.");
                            #region Response
                            DataIOHandler _IOHandler_ = DataIOHandler.Get(messageType);
                            _IOHandler_.Write(uniqueId);
                            UdpClient connectedClient = GetClient(remoteEndPoint);
                            if (connectedClient != null)
                            {
                                connectedClient.Send(_IOHandler_, deliveryMode, target);
                            }
                            else
                            {
                                OmniLogger.PrintError("Connect -> Client is null!");
                            }
                            _IOHandler_.Release();
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
                            double timeOfClient = IOHandler.ReadDouble();
                            DataIOHandler _IOHandler_ = DataIOHandler.Get(messageType);
                            _IOHandler_.Write(timeOfClient);
                            _IOHandler_.Write(OmniTime.LocalTime);
                            client.Send(_IOHandler_, deliveryMode, target);
                            _IOHandler_.Release();

                            // Reposiciona o IOHandler para a posi��o 0
                            // Chama o evento OnMessage do OmniNetwork com os par�metros fornecidos
                            IOHandler.Position = 0;
                            OmniNetwork.OnMessage(IOHandler, messageType, deliveryMode, target, processingOption, cachingOption, remoteEndPoint, IsServer);
                        }
                        else
                        {
                            OmniLogger.PrintError("Error: Attempted to ping a disconnected client!");
                        }
                    }
                    break;
                case MessageType.PacketLoss:
                    {
                        UdpClient client = GetClient(remoteEndPoint);
                        client?.Send(IOHandler);
                    }
                    break;
                default:
                    OmniNetwork.OnMessage(IOHandler, messageType, deliveryMode, target, processingOption, cachingOption, remoteEndPoint, IsServer);
                    break;
            }
        }

        internal override UdpClient GetClient(UdpEndPoint remoteEndPoint) => GetClient((ushort)remoteEndPoint.GetPort());
        internal UdpClient GetClient(ushort playerId) => clients.TryGetValue(playerId, out UdpClient udpClient) ? udpClient : null;

        internal void Send(DataIOHandler IOHandler, DataDeliveryMode deliveryMode, DataTarget target, DataProcessingOption processingOption, DataCachingOption cachingOption, ushort playerId) => Send(IOHandler, deliveryMode, target, processingOption, cachingOption, GetClient(playerId));
        internal void Send(DataIOHandler IOHandler, DataDeliveryMode deliveryMode, DataTarget target, DataProcessingOption processingOption, DataCachingOption cachingOption, UdpEndPoint remoteEndPoint) => Send(IOHandler, deliveryMode, target, processingOption, cachingOption, GetClient(remoteEndPoint));
        internal void Send(DataIOHandler IOHandler, DataDeliveryMode deliveryMode, DataTarget target, DataProcessingOption processingOption, DataCachingOption cachingOption, UdpClient sender)
        {
            // When we send the data to itself, we will always use the Unreliable deliveryMode.
            // LocalHost(Loopback), there are no risks of drops or clutter.
            if (sender != null)
            {
                switch (target)
                {
                    case DataTarget.Self:
                        {
                            if (processingOption == DataProcessingOption.ProcessOnServer) // Defines whether to execute the instruction on the server when the server itself is the sender.
                            {
                                IOSend(IOHandler, Client.remoteEndPoint, DataDeliveryMode.Unsecured, target, processingOption, cachingOption);
                            }

                            if (!sender.itSelf)
                            {
                                if (!IOHandler.isRawBytes)
                                {
                                    sender.Send(IOHandler, deliveryMode, target, processingOption, cachingOption);
                                }
                                else
                                {
                                    sender.Send(IOHandler);
                                }
                            }
                            else if (processingOption != DataProcessingOption.ProcessOnServer)
                            {
                                OmniLogger.PrintError("Are you trying to execute this instruction on yourself? Please use DataProcessingOption.ProcessOnServer or verify if 'IsMine' is being used correctly.");
                            }
                        }
                        break;
                    case DataTarget.Broadcast:
                        {
                            if (processingOption == DataProcessingOption.ProcessOnServer)
                            {
                                IOSend(IOHandler, Client.remoteEndPoint, DataDeliveryMode.Unsecured, target, processingOption, cachingOption);
                            }

                            foreach (var (_, otherClient) in clients)
                            {
                                if (otherClient.itSelf)
                                    continue;
                                else
                                {
                                    if (!IOHandler.isRawBytes)
                                    {
                                        otherClient.Send(IOHandler, deliveryMode, target, processingOption, cachingOption);
                                    }
                                    else
                                    {
                                        otherClient.Send(IOHandler);
                                    }
                                }
                            }
                        }
                        break;
                    case DataTarget.BroadcastExcludingSelf:
                        {
                            if (processingOption == DataProcessingOption.ProcessOnServer)
                            {
                                IOSend(IOHandler, Client.remoteEndPoint, DataDeliveryMode.Unsecured, target, processingOption, cachingOption);
                            }

                            foreach (var (id, otherClient) in clients)
                            {
                                if (otherClient.itSelf)
                                    continue;
                                else
                                {
                                    if (id != sender.Id)
                                    {
                                        if (!IOHandler.isRawBytes)
                                        {
                                            otherClient.Send(IOHandler, deliveryMode, target, processingOption, cachingOption);
                                        }
                                        else
                                        {
                                            otherClient.Send(IOHandler);
                                        }
                                    }
                                    else continue;
                                }
                            }
                        }
                        break;
                    case DataTarget.Server:
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