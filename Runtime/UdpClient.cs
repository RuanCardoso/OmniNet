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
using static Neutron.Core.Enums;
using static Neutron.Core.NeutronNetwork;

namespace Neutron.Core
{
    internal sealed class UdpClient : UdpSocket
    {
        public Player Player { get; }
        internal int Id { get; private set; }

        protected override string Name => "Neutron_Client";
        protected override bool IsServer => false;

        internal double lastTimeReceivedPing;
        internal bool itSelf;
        internal UdpEndPoint remoteEndPoint;
        internal UdpClient(bool itSelf = false)
        {
            this.itSelf = itSelf;
            if (itSelf)
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
            lastTimeReceivedPing = NeutronTime.LocalTime;
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
                Logger.PrintError("You are connected!");

            while (!IsConnected)
            {
                ByteStream message = ByteStream.Get();
                message.WritePacket(MessageType.Connect);
                Send(message, Channel.Unreliable, Target.Me);
                message.Release();
                yield return WAIT_FOR_CONNECT;
            }
        }

        private IEnumerator Ping()
        {
            while (IsConnected)
            {
                ByteStream message = ByteStream.Get();
                message.WritePacket(MessageType.Ping);
                message.Write(NeutronTime.LocalTime);
                Send(message, Channel.Unreliable, Target.Me);
                message.Release();
                yield return WAIT_FOR_PING;
            }
        }

        internal int Send(ByteStream byteStream) => Send(byteStream, remoteEndPoint, 0);
        internal int Send(ByteStream byteStream, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None)
        {
            if (remoteEndPoint == null)
                Logger.PrintError("You must call Connect() before Send()");
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
                            NeutronNetwork.OnMessage(RECV_STREAM, messageType, channel, target, subTarget, cacheMode, remoteEndPoint, IsServer);
                        }
                        else Logger.PrintError("The client is already connected!");
                    }
                    break;
                case MessageType.Ping:
                    {
                        double timeOfClient = RECV_STREAM.ReadDouble();
                        double timeOfServer = RECV_STREAM.ReadDouble();
                        NeutronTime.SetTime(timeOfClient, timeOfServer);
                    }
                    break;
                default:
                    NeutronNetwork.OnMessage(RECV_STREAM, messageType, channel, target, subTarget, cacheMode, remoteEndPoint, IsServer);
                    break;
            }
        }

        internal override UdpClient GetClient(UdpEndPoint remoteEndPoint) => this;
        protected override void Disconnect(UdpEndPoint endPoint) => OnDisconnected();
    }
}