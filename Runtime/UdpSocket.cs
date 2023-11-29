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
using UnityEngine;
using static Omni.Core.Enums;
using static Omni.Core.OmniNetwork;
using static Omni.Core.PlatformSettings;

#pragma warning disable

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

        internal Socket socket;
        internal readonly CancellationTokenSource cancellationTokenSource = new();

        private Coroutine WINDOW_COROUTINE;

        protected void Initialize()
        {
            RECV_WINDOW.Initialize();
            SENT_WINDOW.Initialize();
        }

        internal void Bind(UdpEndPoint localEndPoint)
        {
            socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
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
                        socket.IOControl(-1744830452, new byte[] { 0, 0, 0, 0 }, null);
                    }

                    switch (Application.platform) // [ONLY SERVER]
                    {
                        case RuntimePlatform.LinuxEditor:
                        case RuntimePlatform.LinuxServer:
                        case RuntimePlatform.WindowsEditor:
                        case RuntimePlatform.WindowsServer:
                            {
                                int udpChecksumOp = Instance.UdpChecksum ? 1 : 0;
                                Native.setsockopt(socket.Handle, SocketOptionLevel.Udp, SocketOptionName.DontFragment, 1, sizeof(int));
                                Native.setsockopt(socket.Handle, SocketOptionLevel.Udp, SocketOptionName.NoChecksum, udpChecksumOp, sizeof(int));
                            }
                            break;
                        default:
                            OmniLogger.PrintError("This plataform not support -> \"SocketOptionName.NoChecksum\"");
                            break;
                    }
                }
#endif
                Initialize();
                socket.ExclusiveAddressUse = true;
                socket.Bind(localEndPoint);
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

        private byte[] GetAesKey(UdpEndPoint udpEndPoint, bool encrypt)
        {
            UdpClient client = GetClient(udpEndPoint);
            return encrypt ? client.AesKeyToEncrypt : client.AesKeyToDecrypt;
        }

        protected void WINDOW(UdpEndPoint remoteEndPoint) => WINDOW_COROUTINE = Instance.StartCoroutine(SENT_WINDOW.Relay(this, remoteEndPoint, cancellationTokenSource.Token));
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

                if (AesEnabled)
                {
                    IOHandler.Position = 0;
                    IOHandler.ReadPayload(out DataDeliveryMode deliveryMode, out DataTarget target, out DataProcessingOption processingOption, out DataCachingOption cachingOption);

                    // Generate Aes Key and IV to encrypt the data.
                    // Skip the payload to encrypt only the data without the payload, because if the payload is encrypted, the server will not be able to read it.
                    byte[] Key = GetAesKey(remoteEndPoint, true);
                    byte[] IV = IOHandler.EncryptBuffer(Key, 1); // << SKIP 1 Byte(Payload));

                    // Write the encrypted data to the AesHandler.
                    // Iv is written to the AesHandler to be sent.
                    DataIOHandler AesHandler = DataIOHandler.Get();
                    AesHandler.WritePayload(deliveryMode, target, processingOption, cachingOption);
                    AesHandler.Write128Bits(IV);
                    AesHandler.Write(IOHandler);

                    // Send the encrypted data.
                    int length = socksend(AesHandler, offset, remoteEndPoint);
                    IOHandler.Release();
                    return length;
                }

                return socksend(IOHandler, offset, remoteEndPoint);
            }
            catch (ObjectDisposedException) { return 0; }
        }

        private int socksend(DataIOHandler data, int offset, UdpEndPoint remoteEndPoint)
        {
            int total = data.BytesWritten;
            int length = socket.SendTo(data.Buffer, offset, total - offset, SocketFlags.None, remoteEndPoint);

            NetworkMonitor.PacketsSent++;
            NetworkMonitor.BytesSent += (ulong)length;

            if (!data.isRawBytes) data.Release();
            if (length != total)
                OmniLogger.PrintError($"{Name} - Send Error - Failed to send {total} bytes to {remoteEndPoint}. Only {length} bytes were successfully sent.");
            return length;
        }

        readonly byte[] buffer = new byte[1500]; // MTU SIZE
        readonly EndPoint endPoint = new UdpEndPoint(0, 0);
        internal void Receive()
        {
            if (!IsConnected && IsServer)
            {
                return;
            }

#if UNITY_SERVER && !UNITY_EDITOR
            int multiplier = ServerSettings.recvMultiplier;
#else
            int multiplier = ClientSettings.recvMultiplier;
#endif
            if (!cancellationTokenSource.IsCancellationRequested)
            {
                if (socket.Available <= 0) // prevents blocking of the main thread.
                {
                    return; // If there is no data we will just skip the execution.
                }

                for (int i = 0; i < multiplier; i++)
                {
                    if (socket.Available <= 0) // prevents blocking of the main thread.
                    {
                        break; // Let's prevent our loop from spending unnecessary processing(CPU).
                    }

                    int totalBytesReceived = OmniHelper.ReceiveFrom(socket, buffer, endPoint, out SocketError errorCode);
                    NetworkMonitor.PacketsReceived++;
                    NetworkMonitor.BytesReceived += (ulong)totalBytesReceived;
                    if (totalBytesReceived > 0)
                    {
                        var remoteEndPoint = (UdpEndPoint)endPoint;
                        DataIOHandler IOHandler = DataIOHandler.Get();
                        IOHandler.Write(buffer, 0, totalBytesReceived);
                        IOHandler.Position = 0;
                        IOHandler.isRawBytes = true;
                        IOHandler.ReadPayload(out DataDeliveryMode dataDeliveryMode, out DataTarget target, out DataProcessingOption processingOption, out DataCachingOption cachingOption);
                        if ((byte)target > 3 || (byte)dataDeliveryMode > 2 || (byte)processingOption > 1 || (byte)cachingOption > 2)
                        {
                            OmniLogger.PrintError($"Corrupted payload received from {remoteEndPoint} -> {dataDeliveryMode}:{target}:{processingOption}:{cachingOption}");
                            //*************************************************************************************************
                            IOHandler.Release();
                            continue; // skip
                        }
                        else
                        {
                            switch (dataDeliveryMode)
                            {
                                case DataDeliveryMode.Unsecured:
                                    {
                                        var msgType = IOHandler.ReadPacket();
                                        switch (msgType)
                                        {
                                            case MessageType.Acknowledgement:
                                                int acknowledgment = IOHandler.ReadInt();
                                                UdpClient _client_ = GetClient(remoteEndPoint);
                                                _client_.SENT_WINDOW.Acknowledgement(acknowledgment);
                                                break;
                                            default:
                                                OnMessage(IOHandler, dataDeliveryMode, target, processingOption, cachingOption, msgType, remoteEndPoint);
                                                break;
                                        }
                                    }
                                    break;
                                case DataDeliveryMode.Secured:
                                case DataDeliveryMode.SecuredWithAes:
                                    {
                                        if (dataDeliveryMode == DataDeliveryMode.SecuredWithAes)
                                        {
                                            byte[] IV = IOHandler.Read128Bits();
                                            byte[] Key = GetAesKey(remoteEndPoint, false);
                                            IOHandler.DecryptBuffer(Key, IV, IOHandler.Position);
                                            OmniLogger.Print("Passou pelo DecryptBuffer: " + IsServer);
                                        }

                                        // In AES mode, the sequence number is an integral part of the encrypted data, playing a crucial role in ensuring the integrity and order of transmitted information.
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
                                                continue; // skip
                                            }

                                            var msgType = IOHandler.ReadPacket();
                                            switch (msgType)
                                            {
                                                default:
                                                    {
                                                        RecvWindow RECV_WINDOW = _client_.RECV_WINDOW;
                                                        while ((RECV_WINDOW.window.Length > RECV_WINDOW.LastProcessedPacket) && RECV_WINDOW.window[RECV_WINDOW.LastProcessedPacket].BytesWritten > 0) // Head-of-line blocking
                                                        {
                                                            OnMessage(RECV_WINDOW.window[RECV_WINDOW.LastProcessedPacket], dataDeliveryMode, target, processingOption, cachingOption, msgType, remoteEndPoint);
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
                                            continue; // skip
                                        }
                                    }
                                default:
                                    OmniLogger.PrintError($"Unknown deliveryMode {dataDeliveryMode} received from {remoteEndPoint}");
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
                        continue;
                    }
                }
            }
        }

        internal virtual void Close(bool dispose = false)
        {
            try
            {
                Instance.StopCoroutine(WINDOW_COROUTINE);
                cancellationTokenSource.Cancel();
                if (!dispose)
                    socket.Close();
            }
            catch { }
            finally
            {
                cancellationTokenSource.Dispose();
                if (socket != null && !dispose)
                    socket.Dispose();
            }
        }
    }
}