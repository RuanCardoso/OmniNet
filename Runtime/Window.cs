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

#if !NEUTRON_MULTI_THREADED
using System.Collections;
using UnityEngine;
#else
using System.Threading.Tasks;
#endif
using System.Threading;
using System;

namespace Neutron.Core
{
    public class Window
    {
        int lastIndex = 0;
        internal ByteStream[] window = new ByteStream[NeutronNetwork.WINDOW_SIZE];

        public Window() => Resize(window.Length - 1);
        internal void Resize(int sequence)
        {
            if (!window.InBounds(sequence))
            {
                int size = Math.Abs(sequence - window.Length) + NeutronNetwork.WINDOW_SIZE;
                Array.Resize(ref window, window.Length + size);
            }

            for (int i = lastIndex; i < window.Length; i++) window[i] = new(128);
            if (lastIndex != window.Length) lastIndex = window.Length;
        }
    }

    public class SentWindow : Window
    {
        #region Fields
        private const int WINDOW_SIZE_COMPENSATION = 2;
        private int sequence = -1;
#if NEUTRON_MULTI_THREADED
        private readonly object window_resize_lock = new();
#endif
        #endregion
        internal void Acknowledgement(int acknowledgment)
        {
            if (window.InBounds(acknowledgment))
            {
                ByteStream byteStream = window[acknowledgment];
                if (byteStream != null) byteStream.IsAcked = true;
            }
            else Logger.PrintError($"Ack: Discarded, it's out of window limits -> {sequence}:{window.Length}");
        }
#if NEUTRON_MULTI_THREADED
        internal int GetSequence() => Interlocked.Increment(ref sequence);
#else
        internal int GetSequence() => ++sequence;
#endif
        internal ByteStream GetWindow(int sequence)
        {
#if NEUTRON_MULTI_THREADED
            lock (window_resize_lock)
#endif
            {
                Resize(sequence);
                return window[sequence];
            }
        }
#if NEUTRON_MULTI_THREADED
        internal void Relay(UdpSocket socket, UdpEndPoint remoteEndPoint, CancellationToken token)
#else
        internal IEnumerator Relay(UdpSocket socket, UdpEndPoint remoteEndPoint, CancellationToken token)
#endif
        {
#if NEUTRON_MULTI_THREADED
            ThreadPool.QueueUserWorkItem(async (o) =>
#endif
            {
                int nextSequence = 0;
                while (!token.IsCancellationRequested)
                {
#if NEUTRON_AGRESSIVE_RELAY
#if NEUTRON_MULTI_THREADED
                    int sequence = Interlocked.CompareExchange(ref this.sequence, 0, 0) + WINDOW_SIZE_COMPENSATION;
#else
                    int sequence = this.sequence + WINDOW_SIZE_COMPENSATION;
#endif
#if NEUTRON_MULTI_THREADED
                    //lock (window_resize_lock)
#endif
                    {
                        for (int i = nextSequence; i < sequence && window.InBounds(i); i++)
#endif
                        {
                            try
                            {
#if !NEUTRON_AGRESSIVE_RELAY && NEUTRON_MULTI_THREADED
                                //lock (window_resize_lock)
#endif
                                {
#if NEUTRON_AGRESSIVE_RELAY
                                    ByteStream window = this.window[i];
#else
                                    ByteStream window = this.window[nextSequence];
#endif
                                    if (window.BytesWritten > 0)
                                    {
                                        if (window.IsAcked == true)
                                        {
#if !NEUTRON_AGRESSIVE_RELAY
                                            nextSequence++;
                                            // remove the references to make it eligible for the garbage collector.
                                            this.window[i] = null;
#else
                                            int confirmedSequence = i;
                                            if (confirmedSequence == nextSequence)
                                            {
                                                nextSequence++;
                                                // remove the references to make it eligible for the garbage collector.
                                                this.window[i] = null;
                                            }
#endif
                                        }
                                        else
                                        {
                                            double totalSeconds = DateTime.UtcNow.Subtract(window.LastWriteTime).TotalSeconds;
                                            if (totalSeconds > 0.1d)
                                            {
                                                window.Position = 0;
                                                window.SetLastWriteTime();
                                                socket.Send(window, remoteEndPoint);
                                            }
                                            else { }
                                        }
                                    }
                                    else { }
                                }
                            }
                            catch (Exception ex)
                            {
#if NEUTRON_AGRESSIVE_RELAY
                                Logger.PrintError($"Failed to re-transmit the sequence message! -> {ex.Message}:{i}");
#else
                                Logger.PrintError($"Failed to re-transmit the sequence message! -> {ex.Message}:{nextSequence}");
#endif
                                continue;
                            }
                        }
#if NEUTRON_AGRESSIVE_RELAY
                    }
#endif

#if NEUTRON_MULTI_THREADED
                    await Task.Delay(100);
#else
                    yield return new WaitForSeconds(0.1f);
#endif
                }
            }
#if NEUTRON_MULTI_THREADED
            );
#endif
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

        internal int Acknowledgment(int sequence, ByteStream RECV_STREAM, out MessageRoute route)
        {
            #region Message Route
            route = MessageRoute.Orderly;
            if (ExpectedSequence == sequence) ExpectedSequence++;
            else if (sequence > ExpectedSequence) route = MessageRoute.OutOfOrder;
            else if (sequence < ExpectedSequence) route = MessageRoute.Duplicate;
            else route = MessageRoute.Unk;
            #endregion
            #region Write In Sequence
            switch (route)
            {
                case MessageRoute.Orderly:
                case MessageRoute.OutOfOrder:
                    {
                        Resize(sequence);
                        //*****************************************
                        ByteStream window = this.window[sequence];
                        if (window.BytesWritten <= 0)
                        {
                            int POS = RECV_STREAM.Position + sizeof(byte);
                            window.Write(RECV_STREAM, POS, RECV_STREAM.BytesWritten);
                            window.isRawBytes = RECV_STREAM.isRawBytes;
                            window.Position = 0;
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