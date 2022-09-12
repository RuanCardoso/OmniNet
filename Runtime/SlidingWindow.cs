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
        private ByteStream[] window = new ByteStream[255];
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
                return true;
            }
            else if (sequence > nextSequence) return false; // out of order! 
            else if (sequence < nextSequence) return false;  // duplicated!
            else return false;
        }

        internal void Relay(UdpSocket _socket_, UdpEndPoint remoteEndPoint, CancellationToken token)
        {
            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                while (!token.IsCancellationRequested)
                {
                    ByteStream _stream_ = window[nextSequence];
                    if (_stream_.BytesWritten > 0)
                    {
                        if (DateTime.UtcNow.Subtract(_stream_.LastWriteTime).TotalSeconds > 0.1d)
                        {
                            _stream_.Position = 0;
                            _stream_.SetLastWriteTime();
                            _socket_.Send(_stream_, remoteEndPoint);
                            Logger.Print("Reenviando!");
                        }
                        else continue;
                    }
                    else continue;
                    await Task.Delay(15);
                }
            });
        }
    }
}