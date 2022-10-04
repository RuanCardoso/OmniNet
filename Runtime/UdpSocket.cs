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
#if !NEUTRON_MULTI_THREADED
using System.Collections;
#else
using ThreadPriority = System.Threading.ThreadPriority;
#endif
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace Neutron.Core
{
    internal abstract class UdpSocket
    {
        private readonly RecvWindow RECV_WINDOW = new();
        private readonly SentWindow SENT_WINDOW = new();

        internal abstract UdpClient GetClient(UdpEndPoint remoteEndPoint);
        protected abstract void OnMessage(ByteStream RECV_STREAM, Channel channel, Target target, MessageType messageType, UdpEndPoint remoteEndPoint);

        protected abstract bool IsServer { get; }
        protected abstract string Name { get; }

        internal Socket globalSocket;
        internal readonly CancellationTokenSource cancellationTokenSource = new();

        protected void Initialize()
        {
            RECV_WINDOW.Initialize();
            SENT_WINDOW.Initialize();
        }

        internal void Bind(UdpEndPoint localEndPoint)
        {
            globalSocket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                ReceiveBufferSize = NeutronNetwork.Instance.platformSettings.recvBufferSize,
                SendBufferSize = NeutronNetwork.Instance.platformSettings.sendBufferSize,
            };

            try
            {
#if UNITY_SERVER || UNITY_EDITOR
                if (IsServer) // Only work in Windows Server and Linux Server, Mac Os Server not support!
                {
                    switch (Application.platform)
                    {
                        case RuntimePlatform.LinuxEditor:
                        case RuntimePlatform.LinuxServer:
                        case RuntimePlatform.WindowsEditor:
                        case RuntimePlatform.WindowsServer:
                            Native.setsockopt(globalSocket.Handle, SocketOptionLevel.Udp, SocketOptionName.NoChecksum, 0, sizeof(int));
                            break;
                        default:
                            Logger.PrintWarning("This plataform not support -> \"SocketOptionName.NoChecksum\"");
                            break;
                    }
                }
#endif
                Initialize();
                globalSocket.ExclusiveAddressUse = true;
                globalSocket.Bind(localEndPoint);
#if NEUTRON_MULTI_THREADED
                ReadData();
#else
                NeutronNetwork.Instance.StartCoroutine(ReadData());
#endif
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                    Logger.PrintWarning($"The {Name} not binded to {localEndPoint} because it is already in use!");
                else
                    Logger.LogStacktrace(ex);
            }
            catch (Exception ex)
            {
                Logger.LogStacktrace(ex);
            }
        }

#if NEUTRON_MULTI_THREADED
        protected void MessageRelay(UdpEndPoint remoteEndPoint) => SENT_WINDOW.Relay(this, remoteEndPoint, cancellationTokenSource.Token);
#else
        protected void MessageRelay(UdpEndPoint remoteEndPoint) => NeutronNetwork.Instance.StartCoroutine(SENT_WINDOW.Relay(this, remoteEndPoint, cancellationTokenSource.Token));
#endif
        protected int SendUnreliable(ByteStream data, UdpEndPoint remoteEndPoint, Target target = Target.Me)
        {
            ByteStream stream = ByteStream.Get();
            stream.Write((byte)((byte)Channel.Unreliable | (byte)target << 2));
            stream.Write(data);
            int length = Send(stream, remoteEndPoint);
            stream.Release();
            return length;
        }

        protected int SendReliable(ByteStream data, UdpEndPoint remoteEndPoint, Channel channel = Channel.Reliable, Target target = Target.Me)
        {
            if (IsServer)
                Logger.PrintError("The server can't send data directly to a client, use the client object(UdpClient) to send data.");
            else
            {
                ByteStream stream = ByteStream.Get();
                stream.Write((byte)((byte)channel | (byte)target << 2));
                int _sequence_ = SENT_WINDOW.GetSequence();
                stream.Write(_sequence_);
                stream.Write(data);
                ByteStream window = SENT_WINDOW.GetWindow(_sequence_);
                window.EndWrite();
                window.SetLastWriteTime();
                window.Write(stream);
                int length = Send(stream, remoteEndPoint);
                stream.Release();
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
                    data.Position = 0;
                    Channel channel = (Channel)(data.ReadByte() & 0x3);
                    switch (channel)
                    {
                        case Channel.Reliable:
                            {
                                byte[] buffer = data.Buffer;
                                int _sequence_ = SENT_WINDOW.GetSequence();
                                buffer[++offset] = (byte)_sequence_;
                                buffer[++offset] = (byte)(_sequence_ >> 8);
                                buffer[++offset] = (byte)(_sequence_ >> 16);
                                buffer[++offset] = (byte)(_sequence_ >> 24);
                                offset = 0;

                                ByteStream window = SENT_WINDOW.GetWindow(_sequence_);
                                window.EndWrite();
                                window.SetLastWriteTime();
                                window.Write(data);
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
        private void ReadData()
#else
        private IEnumerator ReadData()
#endif
        {
#if NEUTRON_MULTI_THREADED
            new Thread(() =>
#endif
            {
                byte[] buffer = new byte[0x5DC];
                EndPoint endPoint = new UdpEndPoint(0, 0);
                int multiplier = NeutronNetwork.Instance.platformSettings.recvMultiplier;
                while (!cancellationTokenSource.IsCancellationRequested)
                {
#if NEUTRON_MULTI_THREADED
                    try
#endif
                    {
#if !NEUTRON_MULTI_THREADED
                        for (int i = 0; i < multiplier; i++)
#endif
                        {
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
                                    Logger.PrintError($"{Name} - ReadData - Invalid target -> {bitTarget} or channel -> {bitChannel}");
                                    //*************************************************************************************************//
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
                                                    {
                                                        RECV_STREAM.Release();
#if !NEUTRON_MULTI_THREADED
                                                        yield return null;
#endif
                                                        continue; // << skip
                                                    }

                                                    #region Send Acknowledgement
                                                    ByteStream windowStream = ByteStream.Get();
                                                    windowStream.WritePacket(MessageType.Acknowledgement);
                                                    windowStream.Write(acknowledgment);
                                                    SendUnreliable(windowStream, remoteEndPoint, Target.Me);
                                                    windowStream.Release();
                                                    #endregion

                                                    if (msgRoute == RecvWindow.MessageRoute.Duplicate || msgRoute == RecvWindow.MessageRoute.OutOfOrder)
                                                    {
                                                        RECV_STREAM.Release();
#if !NEUTRON_MULTI_THREADED
                                                        yield return null;
#endif
                                                        continue; // << skip
                                                    }

                                                    MessageType msgType = RECV_STREAM.ReadPacket();
                                                    switch (msgType)
                                                    {
                                                        default:
                                                            {
                                                                RecvWindow RECV_WINDOW = _client_.RECV_WINDOW;
                                                                while ((RECV_WINDOW.window.Length > RECV_WINDOW.LastProcessedPacket) && RECV_WINDOW.window[RECV_WINDOW.LastProcessedPacket].BytesWritten > 0) // Head-of-line blocking
                                                                {
                                                                    OnMessage(RECV_WINDOW.window[RECV_WINDOW.LastProcessedPacket], bitChannel, bitTarget, msgType, remoteEndPoint);
                                                                    if (RECV_WINDOW.ExpectedSequence <= RECV_WINDOW.LastProcessedPacket)
                                                                        RECV_WINDOW.ExpectedSequence++;
                                                                    // remove the references to make it eligible for the garbage collector.
                                                                    RECV_WINDOW.window[RECV_WINDOW.LastProcessedPacket] = null;
                                                                    RECV_WINDOW.LastProcessedPacket++;
                                                                }

                                                                if (RECV_WINDOW.LastProcessedPacket > (RECV_WINDOW.window.Length - 1))
                                                                    Logger.PrintWarning($"Recv(Reliable): Insufficient window size! no more data can be received, packet sequencing will be restarted or the window will be resized. {RECV_WINDOW.LastProcessedPacket} : {RECV_WINDOW.window.Length}");
                                                            }
                                                            break;
                                                    }
                                                    break;
                                                }
                                                else
                                                {
                                                    RECV_STREAM.Release();
#if !NEUTRON_MULTI_THREADED
                                                    yield return null;
#endif
                                                    continue; // << skip
                                                }
                                            }
                                        default:
                                            Logger.PrintError($"Unknown channel {bitChannel} received from {remoteEndPoint}");
                                            break;
                                    }
                                }

                                RECV_STREAM.Release();
                            }
                            else
                                Logger.PrintError($"{Name} - Receive - Failed to receive {length} bytes from {endPoint}");
                        }
                    }
#if NEUTRON_MULTI_THREADED
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
            }
#if NEUTRON_MULTI_THREADED
            )
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