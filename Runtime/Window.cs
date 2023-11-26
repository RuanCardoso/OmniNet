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
using UnityEngine;
using System.Threading;
using System;
using static Omni.Core.PlatformSettings;

namespace Omni.Core
{
    public class Window
    {
        private int lastIndex = 0;
        private int windowSize = 0;
        internal DataIOHandler[] window;

        internal void Initialize()
        {
            if (window == null)
            {
                windowSize = ServerSettings.windowSize;
                window = new DataIOHandler[windowSize];
                Resize(window.Length - 1);
            }
        }

        internal void Resize(int sequence)
        {
            if (!window.IsInBounds(sequence))
            {
                int size = Math.Abs(sequence - window.Length) + windowSize;
                Array.Resize(ref window, window.Length + size);
            }

            for (int i = lastIndex; i < window.Length; i++)
            {
                window[i] = new(ServerSettings.maxPacketSize);
            }

            if (lastIndex != window.Length)
                lastIndex = window.Length;
        }
    }

    public class SentWindow : Window
    {
        private int sequence = -1;
        internal void Acknowledgement(int acknowledgment)
        {
            if (window.IsInBounds(acknowledgment))
            {
                DataIOHandler IOHandler = window[acknowledgment];
                if (IOHandler != null)
                    IOHandler.IsAcked = true;
            }
            else
            {
                OmniLogger.PrintError($"Ack: Discarded, it's out of window limits -> {sequence}:{window.Length}");
            }
        }

        internal int GetSequence() => ++sequence;
        internal DataIOHandler GetWindow(int sequence)
        {
            Resize(sequence);
            return window[sequence];
        }

        internal IEnumerator Relay(UdpSocket socket, UdpEndPoint remoteEndPoint, CancellationToken token)
        {
            int nextSequence = 0;
            double timeout = ServerSettings.ackTimeout;
            int sweep = ServerSettings.ackSweep;
            var yieldSec = new WaitForSeconds(sweep / 1000f); // avoid gc alloc;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    DataIOHandler wIOHandler = this.window[nextSequence];
                    if (wIOHandler.BytesWritten > 0)
                    {
                        if (wIOHandler.IsAcked == true)
                        {
                            nextSequence++;
                            // remove the references to make it eligible for the garbage collector.
                            this.window[nextSequence - 1] = null;
                        }
                        else
                        {
                            double totalSeconds = DateTime.UtcNow.Subtract(wIOHandler.LastWriteTime).TotalSeconds;
                            if (totalSeconds > timeout)
                            {
                                wIOHandler.Position = 0;
                                wIOHandler.SetLastWriteTime();
                                NetworkMonitor.PacketsRetransmitted++;
                                socket.Send(wIOHandler, remoteEndPoint);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    OmniLogger.PrintError($"Failed to re-transmit the sequence message! -> {ex.Message}:{nextSequence}");
                    continue;
                }

                yield return yieldSec; // gc alloc
            }
        }
    }

    public class RecvWindow : Window
    {
        internal enum MessageRoute : byte
        {
            Orderly,
            OutOfOrder,
            Duplicate,
            Unk,
        }

        internal int ExpectedSequence { get; set; } = 0;
        internal int LastProcessedPacket { get; set; } = 0;

        internal int Acknowledgment(int sequence, DataIOHandler IOHandler, out MessageRoute route)
        {
            #region Message Route
            route = MessageRoute.Orderly;
#pragma warning disable IDE0045
            if (ExpectedSequence == sequence) ExpectedSequence++;
            else if (sequence > ExpectedSequence) route = MessageRoute.OutOfOrder;
            else if (sequence < ExpectedSequence) route = MessageRoute.Duplicate;
            else route = MessageRoute.Unk;
#pragma warning restore IDE0045
            #endregion

            #region Write In Sequence
            switch (route)
            {
                case MessageRoute.Orderly:
                case MessageRoute.OutOfOrder:
                    {
                        Resize(sequence);
                        //***********************************************
                        DataIOHandler wIOHandler = this.window[sequence];
                        if (wIOHandler.BytesWritten <= 0)
                        {
                            int POS = IOHandler.Position + sizeof(byte);
                            wIOHandler.Write(IOHandler, POS, IOHandler.BytesWritten);
                            wIOHandler.isRawBytes = IOHandler.isRawBytes;
                            wIOHandler.Position = 0;
                        }
                        else { } // Duplicate!
                        break;
                    }
            }
            #endregion
            return sequence;
        }
    }
}