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
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static Omni.Core.Enums;
using static Omni.Core.OmniNetwork;

namespace Omni.Core
{
    internal sealed class UdpClient : UdpSocket
    {
        public OmniPlayer Player { get; }
        internal int Id { get; private set; }

        protected override string Name => "Omni_Client";
        protected override bool IsServer => false;

        internal double lastTimeReceivedPing;
        internal bool itSelf;
        internal UdpEndPoint remoteEndPoint;
        internal UdpClient(bool itSelf = false)
        {
            this.itSelf = itSelf;
            if (this.itSelf)
            {
                Id = NetworkId;
                remoteEndPoint = new UdpEndPoint(IPAddress.Loopback, Port);
                Player = new(Id, remoteEndPoint);
            }
            else { /*Client Player*/ }
        }

        internal UdpClient(UdpEndPoint remoteEndPoint, Socket socket) // [SERVER CONSTRUCTOR]
        {
            Initialize();
            IsConnected = true;
            globalSocket = socket;
            lastTimeReceivedPing = OmniTime.LocalTime;
            Id = remoteEndPoint.GetPort();
            Player = new(Id, remoteEndPoint);
            this.remoteEndPoint = new(remoteEndPoint.GetIPAddress(), Id); // copy endpoint to avoid reference problems!
            WINDOW(this.remoteEndPoint);
        }

        internal void Connect(UdpEndPoint remoteEndPoint, CancellationToken cancellationToken)
        {
            this.remoteEndPoint = remoteEndPoint;
            WINDOW(this.remoteEndPoint);
            Instance.StartCoroutine(Connect());

            // Check if the host is available
            Task.Run(async () =>
            {
                await Task.Delay(10000, cancellationToken);
                if (!IsConnected)
                {
                    Instance.StopCoroutine(Connect());
                    OmniLogger.LogError($"Sorry, it seems that the host is currently unavailable. Please try again later -> {remoteEndPoint}");
                }
            }, cancellationToken);
        }

        internal void Disconnect()
        {
            DataIOHandler IOHandler = DataIOHandler.Get(MessageType.Disconnect, true);
            Send(IOHandler, DataDeliveryMode.Secured, DataTarget.Self);
            IOHandler.Release();
            OnDisconnected();
        }

        internal void OnDisconnected()
        {
            Instance.StopCoroutine(Connect());
            Instance.StopCoroutine(Ping());

            IsConnected = false;
            remoteEndPoint = null;
        }

        private IEnumerator Connect()
        {
            if (IsConnected)
            {
                OmniLogger.PrintError("You are connected!");
            }

            while (!IsConnected)
            {
                DataIOHandler IOHandler = DataIOHandler.Get();
                IOHandler.WritePacket(MessageType.Connect);
                Send(IOHandler, DataDeliveryMode.Unsecured, DataTarget.Self);
                IOHandler.Release();
                yield return WAIT_FOR_CONNECT;
                if (!IsConnected)
                    OmniLogger.Log("Retrying to establish connection...");
            }
        }

        private IEnumerator Ping()
        {
            yield return new WaitForSeconds(1f);
            // Send the ping to server to keep alive and calc de RTT.
            while (IsConnected)
            {
                DataIOHandler IOHandler = DataIOHandler.Get();
                IOHandler.WritePacket(MessageType.Ping);
                IOHandler.Write(OmniTime.LocalTime);
                Send(IOHandler, DataDeliveryMode.Unsecured, DataTarget.Self);
                IOHandler.Release();
                yield return WAIT_FOR_PING;
            }
        }

        internal int Send(DataIOHandler IOHandler) => Send(IOHandler, remoteEndPoint, 0);
        internal int Send(DataIOHandler IOHandler, DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured, DataTarget target = DataTarget.Self, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None)
        {
            if (remoteEndPoint == null)
            {
                OmniLogger.PrintError("Error: Call Connect() before Send()");
            }
            else
            {
                return deliveryMode switch
                {
                    DataDeliveryMode.Unsecured => SendUnreliable(IOHandler, remoteEndPoint, target, processingOption, cachingOption),
                    DataDeliveryMode.Secured => SendReliable(IOHandler, remoteEndPoint, target, processingOption, cachingOption),
                    _ => 0,
                };
            }

            return 0;
        }

        protected override void OnMessage(DataIOHandler IOHandler, DataDeliveryMode deliveryMode, DataTarget target, DataProcessingOption processingOption, DataCachingOption cachingOption, MessageType messageType, UdpEndPoint remoteEndPoint)
        {
            switch (messageType)
            {
                case MessageType.Connect:
                    {
                        if (!IsConnected)
                        {
                            Id = IOHandler.ReadUShort();
                            IsConnected = true;
                            Instance.StartCoroutine(Ping());

                            // Reposiciona o IOHandler para a posi��o 0
                            // Chama o evento OnMessage do OmniNetwork com os par�metros fornecidos
                            IOHandler.Position = 0;
                            OmniNetwork.OnMessage(IOHandler, messageType, deliveryMode, target, processingOption, cachingOption, remoteEndPoint, IsServer);
                        }
                        else
                        {
                            OmniLogger.PrintError("Error: The client is already connected. Disconnect before attempting to connect again.");
                        }
                    }
                    break;
                case MessageType.Ping:
                    {
                        double timeOfClient = IOHandler.ReadDouble();
                        double timeOfServer = IOHandler.ReadDouble();
                        OmniTime.SetTime(timeOfClient, timeOfServer);

                        // Reposiciona o IOHandler para a posi��o 0
                        // Chama o evento OnMessage do OmniNetwork com os par�metros fornecidos
                        IOHandler.Position = 0;
                        OmniNetwork.OnMessage(IOHandler, messageType, deliveryMode, target, processingOption, cachingOption, remoteEndPoint, IsServer);
                    }
                    break;
                default:
                    OmniNetwork.OnMessage(IOHandler, messageType, deliveryMode, target, processingOption, cachingOption, remoteEndPoint, IsServer);
                    break;
            }
        }

        internal UdpClient GetClient(ushort playerId) => throw new NotImplementedException("GetClient method is not implemented.");
        internal override UdpClient GetClient(UdpEndPoint remoteEndPoint) => this;
        protected override void Disconnect(UdpEndPoint endPoint, string msg = "") => OnDisconnected();
    }
}