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
using UnityEngine;
#endif
using System.Threading;
using System.Threading.Tasks;

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
        internal void Relay(UdpSocket _socket_, UdpEndPoint remoteEndPoint, CancellationToken token)
#else
        internal IEnumerator Relay(UdpSocket _socket_, UdpEndPoint remoteEndPoint, CancellationToken token)
#endif
        {
#if NEUTRON_MULTI_THREADED
            ThreadPool.QueueUserWorkItem(async (o) =>
#endif
            {
                while (!token.IsCancellationRequested)
                {
                    for (int i = 0; i < NeutronNetwork.WINDOW_SIZE; i++)
                    {
                        ByteStream _stream_ = window[i];
                        if (_stream_.BytesWritten > 0 && ack_window[i] == 0)
                        {
                            double totalSeconds = DateTime.UtcNow.Subtract(_stream_.LastWriteTime).TotalSeconds;
                            if (totalSeconds > 0.1d)
                            {
                                Logger.Print($"Reenviando!");
                                _stream_.Position = 0;
                                _stream_.SetLastWriteTime();
                                _socket_.Send(_stream_, remoteEndPoint);
                            }
                            else continue;
                        }
                        else continue;
                    }
#if NEUTRON_MULTI_THREADED
                    await Task.Delay(15);
#else
                    yield return new WaitForSeconds(0.015f);
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
                        ByteStream window = this.Window[sequence];
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