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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ThreadPriority = System.Threading.ThreadPriority;

namespace Neutron.Core
{
    internal abstract class UdpSocket
    {
        private readonly RecvWindow RECV_WINDOW = new();
        private readonly SentWindow SENT_WINDOW = new();

        internal abstract UdpClient GetClient(UdpEndPoint remoteEndPoint);
        protected abstract void OnMessage(ByteStream recvStream, Channel channel, Target target, MessageType messageType, UdpEndPoint remoteEndPoint);

        protected abstract bool IsServer { get; }
        protected abstract string Name { get; }

        internal Socket globalSocket;
        internal readonly CancellationTokenSource cancellationTokenSource = new();

        internal void Bind(UdpEndPoint localEndPoint)
        {
            globalSocket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                globalSocket.Bind(localEndPoint);
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                    Logger.PrintWarning($"The {Name} not binded to {localEndPoint} because it is already in use.");
            }
#if NEUTRON_MULTI_THREADED
            StartReadingData();
#else
            NeutronNetwork.Instance.StartCoroutine(StartReadingData());
#endif
        }

        protected void Relay(UdpEndPoint remoteEndPoint) => SENT_WINDOW.Relay(this, remoteEndPoint, cancellationTokenSource.Token);
        protected int SendUnreliable(ByteStream data, UdpEndPoint remoteEndPoint, Target target = Target.Me)
        {
            ByteStream poolStream = ByteStream.Get();
            poolStream.Write((byte)((byte)Channel.Unreliable | (byte)target << 2));
            poolStream.Write(data);
            int length = Send(poolStream, remoteEndPoint);
            poolStream.Release();
            return length;
        }

        protected int SendReliable(ByteStream data, UdpEndPoint remoteEndPoint, Channel channel = Channel.Reliable, Target target = Target.Me)
        {
            int _sequence_ = 0;
            if (IsServer)
                Logger.PrintError("The server can't send data directly to a client, use the client object(UdpClient) to send data.");
            else
            {
                ByteStream dataStream = ByteStream.Get();
                dataStream.Write((byte)((byte)channel | (byte)target << 2));
#if NEUTRON_MULTI_THREADED
                _sequence_ = SENT_WINDOW.GetSequence();
#else
                _sequence_ = ++channelObject.sequence;
#endif
                dataStream.Write(_sequence_);
                dataStream.Write(data);
                ByteStream windowStream = SENT_WINDOW.GetWindow(_sequence_);
                windowStream.EndWrite();
                windowStream.SetLastWriteTime();
                windowStream.Write(dataStream);
                int length = Send(dataStream, remoteEndPoint);
                dataStream.Release();
                return length;
            }
            return 0;
        }

        internal int Send(ByteStream data, UdpEndPoint remoteEndPoint, int offset = 0)
        {
            try
            {
                if (data.isRawBytes)
                {
                    int _sequence_ = 0;
                    data.Position = 0;
                    Channel channel = (Channel)(data.ReadByte() & 0x3);
                    switch (channel)
                    {
                        case Channel.Reliable:
                            {
                                byte[] buffer = data.Buffer;
#if NEUTRON_MULTI_THREADED
                                _sequence_ = SENT_WINDOW.GetSequence();
#else
                                _sequence_ = ++channelObject.sequence;
#endif
                                buffer[++offset] = (byte)_sequence_;
                                buffer[++offset] = (byte)(_sequence_ >> 8);
                                buffer[++offset] = (byte)(_sequence_ >> 16);
                                buffer[++offset] = (byte)(_sequence_ >> 24);
                                offset = 0;

                                ByteStream windowStream = SENT_WINDOW.GetWindow(_sequence_);
                                windowStream.EndWrite();
                                windowStream.SetLastWriteTime();
                                windowStream.Write(data);
                                break;
                            }
                    }
                }

                int bytesWritten = data.BytesWritten;
                int length = globalSocket.SendTo(data.Buffer, offset, bytesWritten - offset, SocketFlags.None, remoteEndPoint);
                if (length != bytesWritten) Logger.PrintError($"{Name} - Send - Failed to send {bytesWritten} bytes to {remoteEndPoint}");
                return length;
            }
            catch (ObjectDisposedException) { return 0; }
        }

#if NEUTRON_MULTI_THREADED
        private void StartReadingData()
#else
        private IEnumerator StartReadingData()
#endif
        {
#if NEUTRON_MULTI_THREADED
            new Thread(() =>
            {
#endif
                byte[] buffer = new byte[0x5DC];
                EndPoint endPoint = new UdpEndPoint(0, 0);
#if NEUTRON_MULTI_THREADED
                while (!cancellationTokenSource.IsCancellationRequested)
                {
#else
            while (true)
            {
#endif
#if NEUTRON_MULTI_THREADED
                    try
                    {
#endif
#if !NEUTRON_MULTI_THREADED
                if (globalSocket.Available <= 0)
                {
                    yield return null;
                    continue;
                }
#endif
                        int length = globalSocket.ReceiveFrom(buffer, SocketFlags.None, ref endPoint);
                        if (length > 0)
                        {
                            var remoteEndPoint = (UdpEndPoint)endPoint;
                            ByteStream RECV_STREAM = ByteStream.Get();
                            RECV_STREAM.Write(buffer, 0, length);
                            RECV_STREAM.Position = 0;
                            RECV_STREAM.isRawBytes = true;

                            byte maskBit = RECV_STREAM.ReadByte();
                            Channel bitChannel = (Channel)(maskBit & 0x3);
                            Target bitTarget = (Target)((maskBit >> 2) & 0x3);
                            if ((byte)bitTarget > 0x3 || (byte)bitChannel > 0x3)
                            {
                                Logger.PrintError($"{Name} - StartReadingData - Invalid target -> {bitTarget} or channel -> {bitChannel}");
                                RECV_STREAM.Release();
#if !NEUTRON_MULTI_THREADED
                        yield return null;
#endif
                                continue; // << skip
                            }
                            else
                            {
                                switch (bitChannel)
                                {
                                    case Channel.Unreliable:
                                        {
                                            MessageType msgType = RECV_STREAM.ReadPacket();
                                            switch (msgType)
                                            {
                                                case MessageType.Acknowledgement:
                                                    int acknowledgment = RECV_STREAM.ReadInt();
                                                    UdpClient _client_ = GetClient(remoteEndPoint);
                                                    _client_.SENT_WINDOW.Acknowledgement(acknowledgment);
                                                    break;
                                                default:
                                                    OnMessage(RECV_STREAM, bitChannel, bitTarget, msgType, remoteEndPoint);
                                                    break;
                                            }
                                        }
                                        break;
                                    case Channel.Reliable:
                                        {
                                            int sequence = RECV_STREAM.ReadInt();
                                            UdpClient _client_ = GetClient(remoteEndPoint);
                                            int acknowledgment = _client_.RECV_WINDOW.Acknowledgment(sequence, RECV_STREAM, out RecvWindow.MessageRoute msgRoute);
                                            if (acknowledgment > -1)
                                            {
                                                if (msgRoute == RecvWindow.MessageRoute.Unk)
                                                    continue;

                                                #region Send Acknowledgement
                                                ByteStream windowStream = ByteStream.Get();
                                                windowStream.WritePacket(MessageType.Acknowledgement);
                                                windowStream.Write(acknowledgment);
                                                SendUnreliable(windowStream, remoteEndPoint, Target.Me);
                                                windowStream.Release();
                                                #endregion

                                                if (msgRoute == RecvWindow.MessageRoute.Duplicate || msgRoute == RecvWindow.MessageRoute.OutOfOrder)
                                                    continue;

                                                MessageType msgType = RECV_STREAM.ReadPacket();
                                                switch (msgType)
                                                {
                                                    default:
                                                        {
                                                            RecvWindow nextWindow = _client_.RECV_WINDOW;
                                                            while (nextWindow.Window[nextWindow.LastProcessedPacket].BytesWritten > 0)
                                                            {
                                                                Logger.PrintError($"Sequenced: {nextWindow.LastProcessedPacket}");
                                                                //OnMessage(nextWindow.Window[nextWindow.ExpectedSequence], bitChannel, bitTarget, msgType, remoteEndPoint);
                                                                if (nextWindow.ExpectedSequence <= nextWindow.LastProcessedPacket)
                                                                    nextWindow.ExpectedSequence++;
                                                                nextWindow.LastProcessedPacket++;
                                                            }
                                                        }
                                                        break;
                                                }
                                                break;
                                            }
                                            else { }
                                        }
                                        break;
                                    default:
                                        Logger.PrintError($"Unknown channel {bitChannel} received from {remoteEndPoint}");
                                        break;
                                }
                            }

                            RECV_STREAM.Release();
                        }
                        else
                            Logger.PrintError($"{Name} - Receive - Failed to receive {length} bytes from {endPoint}");
#if NEUTRON_MULTI_THREADED
                    }
                    catch (ThreadAbortException) { continue; }
                    catch (ObjectDisposedException) { continue; }
                    catch (SocketException ex)
                    {
                        if (ex.ErrorCode == 10004) break;
                        else
                        {
                            Logger.LogStacktrace(ex);
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogStacktrace(ex);
                        continue;
                    }
#endif
#if !NEUTRON_MULTI_THREADED
                yield return null;
#endif
                }
#if NEUTRON_MULTI_THREADED
            })
            {
                Name = Name,
                IsBackground = true,
                Priority = ThreadPriority.Highest
            }.Start();
#endif
        }

        internal virtual void Close(bool fromServer = false)
        {
            try
            {
                cancellationTokenSource.Cancel();
                if (!fromServer)
                    globalSocket.Close();
            }
            catch { }
            finally
            {
                cancellationTokenSource.Dispose();
                if (globalSocket != null && !fromServer)
                    globalSocket.Dispose();
            }
        }
    }
}