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
using System.Diagnostics;

namespace Neutron.Core
{
    public class SentWindow
    {
        private int sequence = -1;
        private readonly ByteStream[] window = new ByteStream[NeutronNetwork.WINDOW_SIZE];
        private readonly byte[] ack_window = new byte[NeutronNetwork.WINDOW_SIZE]; // 0: not ack, 1: ack

        public SentWindow()
        {
            for (int i = 0; i < window.Length; i++)
                window[i] = new(128);
        }

        internal void Acknowledgement(int acknowledgment) => ack_window[acknowledgment] = 1;
#if NEUTRON_MULTI_THREADED
        internal int GetSequence() => Interlocked.Increment(ref sequence);
#else
        internal int GetSequence() => ++sequence;
#endif
        internal ByteStream GetWindow(int sequence) => window[sequence];
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
#if NEUTRON_AGRESSIVE_RELAY
                while (!token.IsCancellationRequested)
#else
                while (nextSequence < window.Length && !token.IsCancellationRequested)
#endif
                {
#if NEUTRON_AGRESSIVE_RELAY
                    int sequence = this.sequence + 2;
                    for (int i = nextSequence; i < sequence; i++)
#endif
                    {
#if NEUTRON_AGRESSIVE_RELAY
                        ByteStream window = this.window[i];
#else
                        ByteStream window = this.window[nextSequence];
#endif
                        if (window.BytesWritten > 0)
                        {
#if NEUTRON_AGRESSIVE_RELAY
                            byte ack = ack_window[i];
#else
                            byte ack = ack_window[nextSequence];
#endif
                            if (ack == 1)
                            {
#if !NEUTRON_AGRESSIVE_RELAY
                                nextSequence++;
#else
                                int confirmedSequence = i;
                                if (confirmedSequence == nextSequence)
                                    nextSequence++;
#endif
                            }
                            else
                            {
                                double totalSeconds = DateTime.UtcNow.Subtract(window.LastWriteTime).TotalSeconds;
                                if (totalSeconds > 1d)
                                {
                                    Logger.Print("Re-sent!");
                                    window.Position = 0;
                                    window.SetLastWriteTime();
                                    socket.Send(window, remoteEndPoint);
                                }
                                else { }
                            }
                        }
                        else { }
                    }

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

    public class RecvWindow
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

        internal ByteStream[] Window = new ByteStream[NeutronNetwork.WINDOW_SIZE];

        public RecvWindow()
        {
            for (int i = 0; i < Window.Length; i++)
                Window[i] = new(128);
        }

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
                        ByteStream window = Window[sequence];
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