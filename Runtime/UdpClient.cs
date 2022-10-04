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
using System.Net.Sockets;
using UnityEngine;

namespace Neutron.Core
{
    internal sealed class UdpClient : UdpSocket
    {
        internal int Id { get; private set; }
        internal bool IsConnected { get; private set; }
        protected override string Name => "Neutron_Client";
        protected override bool IsServer => false;

        internal UdpEndPoint remoteEndPoint;
        internal UdpClient() { }
        internal UdpClient(UdpEndPoint remoteEndPoint, Socket socket)
        {
            Initialize();
            IsConnected = true;
            globalSocket = socket;
            this.remoteEndPoint = new(remoteEndPoint.GetIPAddress(), remoteEndPoint.GetPort()); // copy endpoint to avoid reference problems!
            MessageRelay(this.remoteEndPoint);
        }

        internal void Connect(UdpEndPoint remoteEndPoint)
        {
            this.remoteEndPoint = remoteEndPoint;
            MessageRelay(this.remoteEndPoint);
            NeutronNetwork.Instance.StartCoroutine(Connect());
        }

        readonly WaitForSeconds yieldConnect = new(1f);
        private IEnumerator Connect()
        {
            while (!IsConnected)
            {
                ByteStream stream = ByteStream.Get();
                stream.WritePacket(MessageType.Connect);
                Send(stream, Channel.Unreliable, Target.Me);
                stream.Release();
                yield return yieldConnect;
            }
        }

        readonly WaitForSeconds yieldPing = new(1f);
        private IEnumerator Ping()
        {
            while (IsConnected)
            {
                ByteStream stream = ByteStream.Get();
                stream.WritePacket(MessageType.Ping);
                stream.Write(NeutronTime.LocalTime);
                Send(stream, Channel.Unreliable, Target.Me);
                stream.Release();
                yield return yieldPing;
            }
        }

        internal int Send(ByteStream byteStream) => Send(byteStream, remoteEndPoint, 0);
        internal int Send(ByteStream byteStream, Channel channel = Channel.Unreliable, Target target = Target.Me)
        {
            if (remoteEndPoint == null)
                Logger.PrintError("You must call Connect() before Send()");
            else
            {
                return channel switch
                {
                    Channel.Unreliable => SendUnreliable(byteStream, remoteEndPoint, target),
                    Channel.Reliable => SendReliable(byteStream, remoteEndPoint, channel, target),
                    _ => 0,
                };
            }
            return 0;
        }

        protected override void OnMessage(ByteStream RECV_STREAM, Channel channel, Target target, MessageType messageType, UdpEndPoint remoteEndPoint)
        {
            switch (messageType)
            {
                case MessageType.Connect:
                    {
                        if (!IsConnected)
                        {
                            Id = RECV_STREAM.ReadUShort();
                            IsConnected = true;
                            NeutronNetwork.Instance.StartCoroutine(Ping());
                            NeutronNetwork.OnMessage(RECV_STREAM, messageType, channel, target, remoteEndPoint, IsServer);
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
                    NeutronNetwork.OnMessage(RECV_STREAM, messageType, channel, target, remoteEndPoint, IsServer);
                    break;
            }
        }

        internal override UdpClient GetClient(UdpEndPoint remoteEndPoint) => this;
    }
}