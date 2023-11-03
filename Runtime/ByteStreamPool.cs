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

using System.Collections.Generic;
using static Omni.Core.PlatformSettings;

namespace Omni.Core
{
    internal class ByteStreamPool
    {
#if OMNI_MULTI_THREADED
        private readonly object _lock = new();
#endif
        private readonly Stack<ByteStream> pool = new();

        public ByteStreamPool(int length = 128)
        {
            for (int i = 0; i < length; i++)
                pool.Push(new ByteStream(ServerSettings.maxPacketSize, true));
        }

        public ByteStream Get()
        {
#if OMNI_MULTI_THREADED
            lock (_lock)
#endif
            {
                return pool.Count == 0 ? new ByteStream(ServerSettings.maxPacketSize, true) : pool.Pop();
            }
        }

        public void Release(ByteStream stream)
        {
            stream.Write();
#if OMNI_MULTI_THREADED
            lock (_lock)
#endif
            {
                pool.Push(stream);
            }
        }

        public int Count => pool.Count;
    }
}