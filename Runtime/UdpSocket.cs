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
using static Neutron.Core.Enums;
using static Neutron.Core.NeutronNetwork;

namespace Neutron.Core
{
    internal abstract class UdpSocket
    {
        private readonly RecvWindow RECV_WINDOW = new();
        private readonly SentWindow SENT_WINDOW = new();

        internal abstract UdpClient GetClient(UdpEndPoint remoteEndPoint);
        protected abstract void Disconnect(UdpEndPoint endPoint);
        protected abstract void OnMessage(ByteStream RECV_STREAM, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode, MessageType messageType, UdpEndPoint remoteEndPoint);

        internal bool IsConnected { get; set; }
        protected abstract string Name { get; }
        protected abstract bool IsServer { get; }

        internal Socket globalSocket;
        internal readonly CancellationTokenSource cancellationTokenSource = new();

        private Coroutine WINDOW_COROUTINE;

        protected void Initialize()
        {
            RECV_WINDOW.Initialize();
            SENT_WINDOW.Initialize();
        }

        internal void Bind(UdpEndPoint localEndPoint)
        {
            globalSocket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                ReceiveBufferSize = Instance.platformSettings.recvBufferSize,
                SendBufferSize = Instance.platformSettings.sendBufferSize,
            };

            try
            {
                IsConnected = localEndPoint.GetPort() == Port;
#if UNITY_SERVER || UNITY_EDITOR
                if (IsServer) // Only work in Windows Server and Linux Server, Mac Os Server not support!
                {
                    if (Application.platform == RuntimePlatform.WindowsServer || Application.platform == RuntimePlatform.WindowsEditor)
                        globalSocket.IOControl(-1744830452, new byte[] { 0, 0, 0, 0 }, null);

                    switch (Application.platform) // [ONLY SERVER]
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
                ReadData(); // Globally, used for all clients, not dispose or cancel this!
#else
                Instance.StartCoroutine(ReadData()); // Globally, used for all clients, do not dispose or cancel this!
#endif
            }
            catch (SocketException ex)
            {
                IsConnected = false;
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
        protected void WINDOW(UdpEndPoint remoteEndPoint) => SENT_WINDOW.Relay(this, remoteEndPoint, cancellationTokenSource.Token);
#else
        protected void WINDOW(UdpEndPoint remoteEndPoint) => WINDOW_COROUTINE = Instance.StartCoroutine(SENT_WINDOW.Relay(this, remoteEndPoint, cancellationTokenSource.Token));
#endif
        protected int SendUnreliable(ByteStream data, UdpEndPoint remoteEndPoint, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None)
        {
            ByteStream message = ByteStream.Get();
            message.WritePayload(Channel.Unreliable, target, subTarget, cacheMode);
            message.Write(data);
            int length = Send(message, remoteEndPoint);
            message.Release();
            return length;
        }

        protected int SendReliable(ByteStream data, UdpEndPoint remoteEndPoint, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None)
        {
            if (IsServer)
                Logger.PrintError($"The server cannot send data! Use {nameof(GetClient)} instead!");
            else
            {
                ByteStream message = ByteStream.Get();
                message.WritePayload(Channel.Reliable, target, subTarget, cacheMode);
                int _sequence_ = SENT_WINDOW.GetSequence();
                message.Write(_sequence_);
                message.Write(data);
                ByteStream window = SENT_WINDOW.GetWindow(_sequence_);
                window.Write();
                window.SetLastWriteTime();
                window.Write(message);
                int length = Send(message, remoteEndPoint);
                message.Release();
                return length;
            }
            return 0;
        }

        internal int Send(ByteStream data, UdpEndPoint remoteEndPoint, int offset = 0)
        {
            try
            {
                // overwrite an existing sequence.....
                if (data.isRawBytes)
                {
                    data.Position = 0;
                    data.ReadPayload(out Channel channel, out _, out _, out _);
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
                                window.Write();
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
                int multiplier = Instance.platformSettings.recvMultiplier;
                while (!cancellationTokenSource.IsCancellationRequested)
                {
#if !NEUTRON_MULTI_THREADED
                    if (globalSocket.Available <= 0) // prevents blocking of the main thread.
                    {
                        yield return null;
                        continue; // If there is no data we will just skip the execution.
                    }
#endif
#if NEUTRON_MULTI_THREADED
                    try
#endif
                    {
#if !NEUTRON_MULTI_THREADED
                        for (int i = 0; i < multiplier; i++)
#endif
                        {
#if !NEUTRON_MULTI_THREADED
                            if (globalSocket.Available <= 0) // prevents blocking of the main thread.
                            {
                                yield return null;
                                break; // Let's prevent our loop from spending unnecessary processing(CPU).
                            }
#endif                            
                            int totalBytesReceived = NeutronHelper.ReceiveFrom(globalSocket, buffer, endPoint, out SocketError errorCode);
                            if (totalBytesReceived > 0)
                            {
                                var remoteEndPoint = (UdpEndPoint)endPoint;
                                // Slice the received bytes and read the payload.
                                ByteStream RECV_STREAM = ByteStream.Get();
                                RECV_STREAM.Write(buffer, 0, totalBytesReceived);
                                RECV_STREAM.Position = 0;
                                RECV_STREAM.isRawBytes = true;
                                RECV_STREAM.ReadPayload(out Channel channel, out Target target, out SubTarget subTarget, out CacheMode cacheMode);
                                // Check for corrupted payload.
                                // Random data will not have a valid header.
                                if ((byte)target > 0x3 || (byte)channel > 0x1)
                                {
                                    Logger.PrintError($"{Name} - ReadData - Invalid target -> {channel} or channel -> {target}");
                                    //*************************************************************************************************
                                    RECV_STREAM.Release();
#if !NEUTRON_MULTI_THREADED
                                    yield return null;
#endif
                                    continue; // skip
                                }
                                else
                                {
                                    switch (channel)
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
                                                        OnMessage(RECV_STREAM, channel, target, subTarget, cacheMode, msgType, remoteEndPoint);
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
                                                        continue; // skip
                                                    }

                                                    #region Send Acknowledgement
                                                    ByteStream windowStream = ByteStream.Get();
                                                    windowStream.WritePacket(MessageType.Acknowledgement);
                                                    windowStream.Write(acknowledgment);
                                                    SendUnreliable(windowStream, remoteEndPoint, Target.Me); // ACK IS SENT BY UNRELIABLE CHANNEL!
                                                    windowStream.Release();
                                                    #endregion

                                                    if (msgRoute == RecvWindow.MessageRoute.Duplicate || msgRoute == RecvWindow.MessageRoute.OutOfOrder)
                                                    {
                                                        RECV_STREAM.Release();
#if !NEUTRON_MULTI_THREADED
                                                        yield return null;
#endif
                                                        continue; // skip
                                                    }

                                                    MessageType msgType = RECV_STREAM.ReadPacket();
                                                    switch (msgType)
                                                    {
                                                        default:
                                                            {
                                                                RecvWindow RECV_WINDOW = _client_.RECV_WINDOW;
                                                                while ((RECV_WINDOW.window.Length > RECV_WINDOW.LastProcessedPacket) && RECV_WINDOW.window[RECV_WINDOW.LastProcessedPacket].BytesWritten > 0) // Head-of-line blocking
                                                                {
                                                                    OnMessage(RECV_WINDOW.window[RECV_WINDOW.LastProcessedPacket], channel, target, subTarget, cacheMode, msgType, remoteEndPoint);
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
                                                    continue; // skip
                                                }
                                            }
                                        default:
                                            Logger.PrintError($"Unknown channel {channel} received from {remoteEndPoint}");
                                            break;
                                    }
                                }
                                RECV_STREAM.Release();
                            }
                            else
                            {
                                if (errorCode == SocketError.ConnectionReset)
                                {
                                    if (IsServer)
                                        Logger.PrintError("WSAECONNRESET -> The last send operation failed because the host is unreachable.");
                                    else Disconnect(null);
                                }
#if !NEUTRON_MULTI_THREADED
                                yield return null;
#endif
                                continue;
                            }
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

        internal virtual void Close(bool dispose = false)
        {
            try
            {
#if !NEUTRON_MULTI_THREADED
                Instance.StopCoroutine(WINDOW_COROUTINE);
#endif
                cancellationTokenSource.Cancel();
                if (!dispose)
                    globalSocket.Close();
            }
            catch { }
            finally
            {
                cancellationTokenSource.Dispose();
                if (globalSocket != null && !dispose)
                    globalSocket.Dispose();
            }
        }
    }
}