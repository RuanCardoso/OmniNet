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

using System.Collections;
using System.Net;
using System.Net.Sockets;
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

        internal void Connect(UdpEndPoint remoteEndPoint)
        {
            this.remoteEndPoint = remoteEndPoint;
            WINDOW(this.remoteEndPoint);
            Instance.StartCoroutine(Connect());

            // Check if the host is available
            Task.Run(async () =>
            {
                await Task.Delay(10000);
                if (!IsConnected)
                {
                    Logger.LogError($"Sorry, it seems that the host is currently unavailable. Please try again later -> {remoteEndPoint}");
                    Instance.StopCoroutine(Connect());
                }
            });
        }

        internal void Disconnect()
        {
            ByteStream message = ByteStream.Get(MessageType.Disconnect, true);
            Send(message, Channel.Reliable, Target.Me);
            message.Release();
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
                Logger.PrintError("You are connected!");
            }

            while (!IsConnected)
            {
                ByteStream message = ByteStream.Get();
                message.WritePacket(MessageType.Connect);
                Send(message, Channel.Unreliable, Target.Me);
                message.Release();
                yield return WAIT_FOR_CONNECT;
                if (!IsConnected)
                    Logger.Log("Retrying to establish connection...");
            }
        }

        private IEnumerator Ping()
        {
            yield return new WaitForSeconds(1f);
            // Send the ping to server to keep alive and calc de RTT.
            while (IsConnected)
            {
                ByteStream message = ByteStream.Get();
                message.WritePacket(MessageType.Ping);
                message.Write(OmniTime.LocalTime);
                Send(message, Channel.Unreliable, Target.Me);
                message.Release();
                yield return WAIT_FOR_PING;
            }
        }

        internal int Send(ByteStream byteStream) => Send(byteStream, remoteEndPoint, 0);
        internal int Send(ByteStream byteStream, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None)
        {
            if (remoteEndPoint == null)
            {
                Logger.PrintError("Error: Call Connect() before Send()");
            }
            else
            {
                return channel switch
                {
                    Channel.Unreliable => SendUnreliable(byteStream, remoteEndPoint, target, subTarget, cacheMode),
                    Channel.Reliable => SendReliable(byteStream, remoteEndPoint, target, subTarget, cacheMode),
                    _ => 0,
                };
            }

            return 0;
        }

        protected override void OnMessage(ByteStream RECV_STREAM, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode, MessageType messageType, UdpEndPoint remoteEndPoint)
        {
            switch (messageType)
            {
                case MessageType.Connect:
                    {
                        if (!IsConnected)
                        {
                            Id = RECV_STREAM.ReadUShort();
                            IsConnected = true;
                            Instance.StartCoroutine(Ping());
                            OmniNetwork.OnMessage(RECV_STREAM, messageType, channel, target, subTarget, cacheMode, remoteEndPoint, IsServer);
                        }
                        else
                        {
                            Logger.PrintError("Error: The client is already connected. Disconnect before attempting to connect again.");
                        }
                    }
                    break;
                case MessageType.Ping:
                    {
                        double timeOfClient = RECV_STREAM.ReadDouble();
                        double timeOfServer = RECV_STREAM.ReadDouble();
                        OmniTime.SetTime(timeOfClient, timeOfServer);
                    }
                    break;
                default:
                    OmniNetwork.OnMessage(RECV_STREAM, messageType, channel, target, subTarget, cacheMode, remoteEndPoint, IsServer);
                    break;
            }
        }

        internal UdpClient GetClient(ushort playerId) => null;
        internal override UdpClient GetClient(UdpEndPoint remoteEndPoint) => this;
        protected override void Disconnect(UdpEndPoint endPoint, string msg = "") => OnDisconnected();
    }
}