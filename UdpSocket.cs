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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ThreadPriority = System.Threading.ThreadPriority;

namespace Neutron.Core
{
    internal abstract class UdpSocket
    {
        protected abstract void OnMessage(ByteStream recvStream, Channel channel, Target target, MessageType messageType, UdpEndPoint remoteEndPoint);
        internal abstract UdpClient GetClient(UdpEndPoint remoteEndPoint);

        protected abstract bool IsServer { get; }
        protected abstract string Name { get; }

        internal Socket globalSocket;
        internal CancellationTokenSource cancellationTokenSource = new();
        internal HashSet<uint> reliableMessagesAck = new();
        internal ConcurrentDictionary<uint, ByteStream> reliableMessages = new();

        private object syncLock = new();
        private uint sequence = 0;

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
            StartReadingData();
        }

        protected async void SendReliableMessages(UdpEndPoint remoteEndPoint)
        {
            await Task.Run(async () =>
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    await Task.Delay(30);
                    foreach (var (sequence, relayStream) in reliableMessages)
                    {
                        relayStream.Position = 0;
                        Send(relayStream, remoteEndPoint);
                    }
                }
            }, cancellationTokenSource.Token);
        }

        protected void SendUnreliable(ByteStream byteStream, UdpEndPoint remoteEndPoint, Target target = Target.Me)
        {
            ByteStream poolStream = ByteStream.Get();
            poolStream.Write((byte)((byte)Channel.Unreliable | (byte)target << 2));
            poolStream.Write(byteStream);
            Send(poolStream, remoteEndPoint);
            poolStream.Release();
        }

        protected void SendReliable(ByteStream byteStream, UdpEndPoint remoteEndPoint, Channel channel = Channel.Reliable, Target target = Target.Me)
        {
            uint _sequence_ = 0;
            if (IsServer)
                throw new Exception("The server can't send data directly to a client, use the client object(UdpClient) to send data.");

            ByteStream poolStream = ByteStream.Get();
            poolStream.Write((byte)((byte)channel | (byte)target << 2));
            lock (syncLock) poolStream.Write(_sequence_ = ++sequence);
            poolStream.Write(byteStream);
            ByteStream relayStream = new ByteStream(poolStream.BytesWritten);
            relayStream.Write(poolStream);
            reliableMessages.TryAdd(_sequence_, relayStream);
            Send(poolStream, remoteEndPoint);
            poolStream.Release();
        }

        protected int Send(ByteStream byteStream, UdpEndPoint remoteEndPoint, int offset = 0)
        {
            try
            {
                if (byteStream.isRawBytes)
                {
                    uint _sequence_ = 0;
                    byteStream.Position = 0;
                    Channel channel = (Channel)(byteStream.ReadByte() & 0x3);
                    if (channel == Channel.Reliable)
                    {
                        byte[] buffer = byteStream.Buffer;
                        lock (syncLock) _sequence_ = ++sequence;
                        buffer[++offset] = (byte)_sequence_;
                        buffer[++offset] = (byte)(_sequence_ >> 8);
                        buffer[++offset] = (byte)(_sequence_ >> 16);
                        buffer[++offset] = (byte)(_sequence_ >> 24);
                        offset = 0;

                        ByteStream relayStream = new ByteStream(byteStream.BytesWritten);
                        relayStream.Write(byteStream);
                        reliableMessages.TryAdd(_sequence_, relayStream);
                    }
                }

                int bytesWritten = byteStream.BytesWritten;
                int length = globalSocket.SendTo(byteStream.Buffer, offset, bytesWritten - offset, SocketFlags.None, remoteEndPoint);
                if (length != bytesWritten) throw new Exception($"{Name} - Send - Failed to send {bytesWritten} bytes to {remoteEndPoint}");
                return length;
            }
            catch (ObjectDisposedException) { return 0; }
        }

        private void StartReadingData()
        {
            new Thread(() =>
            {
                byte[] buffer = new byte[0x5DC];
                EndPoint endPoint = new UdpEndPoint(0, 0);
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        int length = globalSocket.ReceiveFrom(buffer, SocketFlags.None, ref endPoint);
                        if (length > 0)
                        {
                            UdpEndPoint remoteEndPoint = (UdpEndPoint)endPoint;
                            ByteStream recvStream = ByteStream.Get();
                            recvStream.Write(buffer, 0, length);
                            recvStream.Position = 0;
                            recvStream.isRawBytes = true;
                            #region Bit Mask
                            byte bit = recvStream.ReadByte();
                            Channel channel = (Channel)(bit & 0x3);
                            Target target = (Target)((bit >> 2) & 0x3);
                            if ((byte)target > 0x3)
                                throw new Exception($"{Name} - StartReadingData - Invalid target {target}");
                            #endregion
                            switch (channel)
                            {
                                case Channel.Unreliable:
                                    {
                                        MessageType msgType = recvStream.ReadPacket();
                                        switch (msgType)
                                        {
                                            case MessageType.Acknowledgement:
                                                {
                                                    uint sequence = recvStream.ReadUInt();
                                                    UdpClient client = GetClient(remoteEndPoint);
                                                    if (client != null) client.reliableMessages.TryRemove(sequence, out _);
                                                }
                                                break;
                                            default:
                                                OnMessage(recvStream, channel, target, msgType, remoteEndPoint);
                                                break;
                                        }
                                    }
                                    break;
                                case Channel.Reliable:
                                case Channel.ReliableAndOrderly:
                                    {
                                        #region Send Acknowledgement
                                        uint ack = recvStream.ReadUInt();
                                        ByteStream ackStream = ByteStream.Get();
                                        ackStream.WritePacket(MessageType.Acknowledgement);
                                        ackStream.Write(ack);
                                        SendUnreliable(ackStream, remoteEndPoint, Target.Me);
                                        ackStream.Release();
                                        #endregion

                                        UdpClient client = GetClient(remoteEndPoint);
                                        if (client != null)
                                        {
                                            if (!client.reliableMessagesAck.Add(ack))
                                                Logger.PrintError($"Duplicate acknowledgement {ack}");
                                        }

                                        MessageType msgType = recvStream.ReadPacket();
                                        switch (msgType)
                                        {
                                            default:
                                                OnMessage(recvStream, channel, target, msgType, remoteEndPoint);
                                                break;
                                        }
                                    }
                                    break;
                                default:
                                    Logger.PrintError($"Unknown channel {channel} received from {remoteEndPoint}");
                                    break;
                            }
                            recvStream.Release();
                        }
                        else
                            throw new System.Exception($"{Name} - Receive - Failed to receive {length} bytes from {endPoint}");
                    }
                    catch (ThreadAbortException) { }
                    catch (ObjectDisposedException) { }
                    catch (SocketException ex)
                    {
                        if (ex.ErrorCode == 10004)
                            break;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogStacktrace(ex);
                    }
                }
            })
            {
                Name = Name,
                IsBackground = true,
                Priority = ThreadPriority.Highest
            }.Start();
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