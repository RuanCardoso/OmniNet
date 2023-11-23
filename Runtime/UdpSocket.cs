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
#if !OMNI_MULTI_THREADED
using System.Collections;
#else
using ThreadPriority = System.Threading.ThreadPriority;
#endif
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using static Omni.Core.Enums;
using static Omni.Core.OmniNetwork;
using static Omni.Core.PlatformSettings;

namespace Omni.Core
{
    internal abstract class UdpSocket
    {
        private readonly RecvWindow RECV_WINDOW = new();
        private readonly SentWindow SENT_WINDOW = new();

        internal abstract UdpClient GetClient(UdpEndPoint remoteEndPoint);
        protected abstract void Disconnect(UdpEndPoint endPoint, string msg = "");
        protected abstract void OnMessage(DataIOHandler IOHandler, DataDeliveryMode deliveryMode, DataTarget target, DataProcessingOption processingOption, DataCachingOption cachingOption, MessageType messageType, UdpEndPoint remoteEndPoint);

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
                ReceiveBufferSize = ClientSettings.recvBufferSize,
                SendBufferSize = ClientSettings.sendBufferSize,
            };

            try
            {
                IsConnected = localEndPoint.GetPort() == Port;
#if UNITY_SERVER || UNITY_EDITOR
                if (IsServer) // Only work in Windows Server and Linux Server, Mac Os Server not support!
                {
                    if (Application.platform == RuntimePlatform.WindowsServer || Application.platform == RuntimePlatform.WindowsEditor)
                    {
                        // Disable ICMP error messages
                        globalSocket.IOControl(-1744830452, new byte[] { 0, 0, 0, 0 }, null);
                    }

                    switch (Application.platform) // [ONLY SERVER]
                    {
                        case RuntimePlatform.LinuxEditor:
                        case RuntimePlatform.LinuxServer:
                        case RuntimePlatform.WindowsEditor:
                        case RuntimePlatform.WindowsServer:
                            Native.setsockopt(globalSocket.Handle, SocketOptionLevel.Udp, SocketOptionName.NoChecksum, 0, sizeof(int));
                            break;
                        default:
                            OmniLogger.PrintError("This plataform not support -> \"SocketOptionName.NoChecksum\"");
                            break;
                    }
                }
#endif
                Initialize();
                globalSocket.ExclusiveAddressUse = true;
                globalSocket.Bind(localEndPoint);
#if OMNI_MULTI_THREADED
                ReadData(); // Globally, used for all clients, not dispose or cancel this!
#else
                Instance.StartCoroutine(ReadData()); // Globally, used for all clients, do not dispose or cancel this!
#endif
            }
            catch (SocketException ex)
            {
                IsConnected = false;
                if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                    OmniLogger.PrintError($"The {Name} not binded to {localEndPoint} because it is already in use!");
                else
                    OmniLogger.LogStacktrace(ex);
            }
            catch (Exception ex)
            {
                OmniLogger.LogStacktrace(ex);
            }
        }

#if OMNI_MULTI_THREADED
        protected void WINDOW(UdpEndPoint remoteEndPoint) => SENT_WINDOW.Relay(this, remoteEndPoint, cancellationTokenSource.Token);
#else
        protected void WINDOW(UdpEndPoint remoteEndPoint) => WINDOW_COROUTINE = Instance.StartCoroutine(SENT_WINDOW.Relay(this, remoteEndPoint, cancellationTokenSource.Token));
#endif
        protected int IOSend(DataIOHandler _IOHandler_, UdpEndPoint remoteEndPoint, DataDeliveryMode dataDeliveryMode, DataTarget target = DataTarget.Self, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None)
        {
            return dataDeliveryMode switch
            {
                DataDeliveryMode.Unsecured => SendUnreliable(_IOHandler_, remoteEndPoint, target, processingOption, cachingOption),
                DataDeliveryMode.Secured or DataDeliveryMode.SecuredWithAes => SendReliable(_IOHandler_, remoteEndPoint, dataDeliveryMode, target, processingOption, cachingOption),
                _ => 0,
            };
        }

        private int SendUnreliable(DataIOHandler _IOHandler_, UdpEndPoint remoteEndPoint, DataTarget target = DataTarget.Self, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None)
        {
            if (_IOHandler_.isRawBytes)
            {
                OmniLogger.PrintError("RAW bytes cannot be sent unreliably!");
                return 0;
            }

            DataIOHandler IOHandler = DataIOHandler.Get();
            IOHandler.WritePayload(DataDeliveryMode.Unsecured, target, processingOption, cachingOption);
            IOHandler.Write(_IOHandler_);
            int length = Send(IOHandler, remoteEndPoint);
            //IOHandler.Release();
            return length;
        }

        private int SendReliable(DataIOHandler _IOHandler_, UdpEndPoint remoteEndPoint, DataDeliveryMode dataDeliveryMode, DataTarget target = DataTarget.Self, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None)
        {
            if (IsServer)
            {
                OmniLogger.PrintError("Error: The server cannot send data directly. Please use the GetClient method to obtain a client instance for sending data.");
                return 0;
            }

            if (_IOHandler_.isRawBytes)
            {
                OmniLogger.PrintError("RAW bytes cannot be sent reliably!");
                return 0;
            }

            // Write the payload and the sequence to the IOHandler.
            DataIOHandler IOHandler = DataIOHandler.Get();
            int _sequence_ = SENT_WINDOW.GetSequence();
            IOHandler.WritePayload(dataDeliveryMode, target, processingOption, cachingOption);
            IOHandler.Write(_sequence_);
            IOHandler.Write(_IOHandler_);

            // Write the data in UDP Window, is used to re-transmit data if it is lost, duplicated or arrives out of order.
            DataIOHandler wIOHandler = SENT_WINDOW.GetWindow(_sequence_);
            wIOHandler.Write();
            wIOHandler.SetLastWriteTime();
            wIOHandler.Write(IOHandler);
            return Send(IOHandler, remoteEndPoint, AesEnabled: dataDeliveryMode == DataDeliveryMode.SecuredWithAes);
        }

        internal int Send(DataIOHandler IOHandler, UdpEndPoint remoteEndPoint, int offset = 0, bool AesEnabled = false)
        {
            try
            {
                // Increments the sequence in the existing IOHandler.
                // This is not called for new IOHandlers, because they are already incremented in the SendReliable method.
                if (IOHandler.isRawBytes)
                {
                    IOHandler.Position = 0;
                    IOHandler.ReadPayload(out DataDeliveryMode deliveryMode, out _, out _, out _);
                    switch (deliveryMode)
                    {
                        case DataDeliveryMode.Secured:
                            {
                                byte[] buffer = IOHandler.Buffer;
                                int _sequence_ = SENT_WINDOW.GetSequence();
                                buffer[++offset] = (byte)_sequence_;
                                buffer[++offset] = (byte)(_sequence_ >> 8);
                                buffer[++offset] = (byte)(_sequence_ >> 16);
                                buffer[++offset] = (byte)(_sequence_ >> 24);
                                offset = 0;

                                DataIOHandler wIOHandler = SENT_WINDOW.GetWindow(_sequence_);
                                wIOHandler.Write();
                                wIOHandler.SetLastWriteTime();
                                wIOHandler.Write(IOHandler);
                                break;
                            }
                        case DataDeliveryMode.Unsecured:
                            {
                                IOHandler.Position = 0;
                            }
                            break;
                    }
                }

                NetworkMonitor.PacketsSent++;

                if (AesEnabled)
                {
                    // Crypt the data with AES create header.
                    IOHandler.Position = 0;
                    IOHandler.ReadPayload(out DataDeliveryMode deliveryMode, out DataTarget target, out DataProcessingOption processingOption, out DataCachingOption cachingOption);
                    byte[] IV = IOHandler.EncryptBuffer(AuthStorage.AesKey, 1); // Skip the payload, because it is already written in the AesHandler. 1 Byte saved.
                    DataIOHandler AesHandler = DataIOHandler.Get();
                    AesHandler.WritePayload(deliveryMode, target, processingOption, cachingOption);
                    AesHandler.WriteIV(IV);
                    AesHandler.Write(IOHandler);

                    // Send the data to the remoteEndPoint.
                    int bytesWritten = AesHandler.BytesWritten;
                    int length = globalSocket.SendTo(AesHandler.Buffer, offset, bytesWritten - offset, SocketFlags.None, remoteEndPoint);

                    NetworkMonitor.BytesSent += (ulong)length;

                    AesHandler.Release();
                    IOHandler.Release();
                    if (length != bytesWritten)
                        OmniLogger.PrintError($"{Name} - Send Error - Failed to send {bytesWritten} bytes to {remoteEndPoint}. Only {length} bytes were successfully sent.");
                    return length;
                }
                else
                {
                    int bytesWritten = IOHandler.BytesWritten;
                    int length = globalSocket.SendTo(IOHandler.Buffer, offset, bytesWritten - offset, SocketFlags.None, remoteEndPoint);

                    NetworkMonitor.BytesSent += (ulong)length;
                    if (!IOHandler.isRawBytes)
                        IOHandler.Release();
                    if (length != bytesWritten)
                        OmniLogger.PrintError($"{Name} - Send Error - Failed to send {bytesWritten} bytes to {remoteEndPoint}. Only {length} bytes were successfully sent.");
                    return length;
                }
            }
            catch (ObjectDisposedException) { return 0; }
        }

#if OMNI_MULTI_THREADED
        private void ReadData()
#else
        private IEnumerator ReadData()
#endif
        {
#if OMNI_MULTI_THREADED
            new Thread(() =>
#endif
            {
                byte[] buffer = new byte[1500]; // MTU SIZE
                EndPoint endPoint = new UdpEndPoint(0, 0);
#if UNITY_SERVER && !UNITY_EDITOR
                int multiplier = ServerSettings.recvMultiplier;
#else
                int multiplier = ClientSettings.recvMultiplier;
#endif
                while (!cancellationTokenSource.IsCancellationRequested)
                {
#if !OMNI_MULTI_THREADED
                    if (globalSocket.Available <= 0) // prevents blocking of the main thread.
                    {
                        yield return null;
                        continue; // If there is no data we will just skip the execution.
                    }
#endif
#if OMNI_MULTI_THREADED
                    try
#endif
                    {
#if !OMNI_MULTI_THREADED
                        for (int i = 0; i < multiplier; i++)
#endif
                        {
#if !OMNI_MULTI_THREADED
                            if (globalSocket.Available <= 0) // prevents blocking of the main thread.
                            {
                                yield return null;
                                break; // Let's prevent our loop from spending unnecessary processing(CPU).
                            }
#endif                            
                            int totalBytesReceived = OmniHelper.ReceiveFrom(globalSocket, buffer, endPoint, out SocketError errorCode);
                            NetworkMonitor.BytesReceived += (ulong)totalBytesReceived;
                            NetworkMonitor.PacketsReceived++;
                            if (totalBytesReceived > 0)
                            {
                                var remoteEndPoint = (UdpEndPoint)endPoint;
                                // Slice the received bytes and read the payload.
                                DataIOHandler IOHandler = DataIOHandler.Get();
                                IOHandler.Write(buffer, 0, totalBytesReceived);
                                IOHandler.Position = 0;
                                IOHandler.isRawBytes = true;
                                IOHandler.ReadPayload(out DataDeliveryMode deliveryMode, out DataTarget target, out DataProcessingOption processingOption, out DataCachingOption cachingOption);

                                if ((byte)target > 3 || (byte)deliveryMode > 2 || (byte)processingOption > 1 || (byte)cachingOption > 2)
                                {
                                    OmniLogger.PrintError($"Corrupted payload received from {remoteEndPoint} -> {deliveryMode}:{target}:{processingOption}:{cachingOption}");
                                    //*************************************************************************************************
                                    IOHandler.Release();
#if !OMNI_MULTI_THREADED
                                    yield return null;
#endif
                                    continue; // skip
                                }
                                else
                                {
                                    if (deliveryMode == DataDeliveryMode.SecuredWithAes)
                                    {
                                        IOHandler.DecryptBuffer(AuthStorage.AesKey, IOHandler.ReadIV(), IOHandler.Position);
                                    }

                                    switch (deliveryMode)
                                    {
                                        case DataDeliveryMode.Unsecured:
                                            {
                                                MessageType msgType = IOHandler.ReadPacket();
                                                switch (msgType)
                                                {
                                                    case MessageType.Acknowledgement:
                                                        int acknowledgment = IOHandler.ReadInt();
                                                        UdpClient _client_ = GetClient(remoteEndPoint);
                                                        _client_.SENT_WINDOW.Acknowledgement(acknowledgment);
                                                        break;
                                                    default:
                                                        OnMessage(IOHandler, deliveryMode, target, processingOption, cachingOption, msgType, remoteEndPoint);
                                                        break;
                                                }
                                            }
                                            break;
                                        case DataDeliveryMode.Secured:
                                        case DataDeliveryMode.SecuredWithAes:
                                            {
                                                int sequence = IOHandler.ReadInt();
                                                UdpClient _client_ = GetClient(remoteEndPoint);
                                                int acknowledgment = _client_.RECV_WINDOW.Acknowledgment(sequence, IOHandler, out RecvWindow.MessageRoute msgRoute);
                                                if (acknowledgment > -1)
                                                {
                                                    #region Monitor
                                                    switch (msgRoute)
                                                    {
                                                        case RecvWindow.MessageRoute.Duplicate:
                                                            NetworkMonitor.PacketsDuplicated++;
                                                            break;
                                                        case RecvWindow.MessageRoute.OutOfOrder:
                                                            NetworkMonitor.PacketsOutOfOrder++;
                                                            break;
                                                    }
                                                    #endregion

                                                    if (msgRoute == RecvWindow.MessageRoute.Unk)
                                                    {
                                                        IOHandler.Release();
#if !OMNI_MULTI_THREADED
                                                        yield return null;
#endif
                                                        continue; // skip
                                                    }

                                                    #region Send Acknowledgement
                                                    DataIOHandler wIOHandler = DataIOHandler.Get();
                                                    wIOHandler.WritePacket(MessageType.Acknowledgement);
                                                    wIOHandler.Write(acknowledgment);
                                                    SendUnreliable(wIOHandler, remoteEndPoint, DataTarget.Self); // ACK IS SENT BY UNRELIABLE CHANNEL!
                                                    wIOHandler.Release();
                                                    #endregion

                                                    if (msgRoute == RecvWindow.MessageRoute.Duplicate || msgRoute == RecvWindow.MessageRoute.OutOfOrder)
                                                    {
                                                        IOHandler.Release();
#if !OMNI_MULTI_THREADED
                                                        yield return null;
#endif
                                                        continue; // skip
                                                    }

                                                    MessageType msgType = IOHandler.ReadPacket();
                                                    switch (msgType)
                                                    {
                                                        default:
                                                            {
                                                                RecvWindow RECV_WINDOW = _client_.RECV_WINDOW;
                                                                while ((RECV_WINDOW.window.Length > RECV_WINDOW.LastProcessedPacket) && RECV_WINDOW.window[RECV_WINDOW.LastProcessedPacket].BytesWritten > 0) // Head-of-line blocking
                                                                {
                                                                    OnMessage(RECV_WINDOW.window[RECV_WINDOW.LastProcessedPacket], deliveryMode, target, processingOption, cachingOption, msgType, remoteEndPoint);
                                                                    if (RECV_WINDOW.ExpectedSequence <= RECV_WINDOW.LastProcessedPacket)
                                                                        RECV_WINDOW.ExpectedSequence++;
                                                                    // remove the references to make it eligible for the garbage collector.
                                                                    RECV_WINDOW.window[RECV_WINDOW.LastProcessedPacket] = null;
                                                                    RECV_WINDOW.LastProcessedPacket++;
                                                                }

                                                                if (RECV_WINDOW.LastProcessedPacket > (RECV_WINDOW.window.Length - 1))
                                                                    OmniLogger.PrintError($"Recv(Reliable): Insufficient window size! no more data can be received, packet sequencing will be restarted or the window will be resized. {RECV_WINDOW.LastProcessedPacket} : {RECV_WINDOW.window.Length}");
                                                            }
                                                            break;
                                                    }
                                                    break;
                                                }
                                                else
                                                {
                                                    IOHandler.Release();
#if !OMNI_MULTI_THREADED
                                                    yield return null;
#endif
                                                    continue; // skip
                                                }
                                            }
                                        default:
                                            OmniLogger.PrintError($"Unknown deliveryMode {deliveryMode} received from {remoteEndPoint}");
                                            break;
                                    }
                                }
                                IOHandler.Release();
                            }
                            else
                            {
                                if (errorCode == SocketError.ConnectionReset)
                                {
                                    if (IsServer)
                                        OmniLogger.PrintError("WSAECONNRESET -> The last send operation failed because the host is unreachable.");
                                    else Disconnect(null, "There was an unexpected disconnection!");
                                }
#if !OMNI_MULTI_THREADED
                                yield return null;
#endif
                                continue;
                            }
                        }
                    }
#if OMNI_MULTI_THREADED
                    catch (ThreadAbortException) { continue; }
                    catch (ObjectDisposedException) { continue; }
                    catch (SocketException ex)
                    {
                        if (ex.ErrorCode == 10004) break;
                        else
                        {
                            OmniLogger.LogStacktrace(ex);
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        OmniLogger.LogStacktrace(ex);
                        continue;
                    }
#endif
#if !OMNI_MULTI_THREADED
                    yield return null;
#endif
                }
            }
#if OMNI_MULTI_THREADED
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
#if !OMNI_MULTI_THREADED
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