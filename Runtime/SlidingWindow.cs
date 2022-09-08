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
using System.Threading;
using System.Threading.Tasks;

namespace Neutron.Core
{
    public class SlidingWindow
    {
        private int sequence = -1;
        private int nextSequence = 0;
        private int offset = 0;
        private ByteStream[] window = new ByteStream[byte.MaxValue * 10];
        internal SlidingWindow()
        {
            for (int i = 0; i < window.Length; i++)
                window[i] = new ByteStream(128);
        }

        internal ByteStream Get(int sequence) => window[sequence];
        internal int Increment() => Interlocked.Increment(ref sequence);
        internal bool Slide(int sequence)
        {
            if (nextSequence == sequence)
            {
                nextSequence = sequence + 1;
                offset = nextSequence;
                return true;
            }
            else if (sequence > nextSequence) return false; // Discard out of order packets!
            else if (sequence < nextSequence) return false; // Discard duplicate packets!
            else return false;
        }

        internal void Relay(UdpSocket socket, UdpEndPoint remoteEndPoint, CancellationToken token)
        {
            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    for (int i = offset; i < window.Length; i++)
                    {
                        ByteStream _stream_ = window[i];
                        if (_stream_.BytesWritten > 0)
                        {
                            if (DateTime.UtcNow.Subtract(_stream_.LastWriteTime).TotalSeconds > 0.100d)
                            {
                                _stream_.Position = 0;
                                _stream_.SetLastWriteTime();
                                socket.Send(_stream_, remoteEndPoint);
                                Logger.Print("Reenviando!");
                            }
                            else continue;
                        }
                        else continue;
                    }

                    await Task.Delay(15);
                }
            });
        }
    }
}