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

namespace Neutron.Core
{
    public class ByteStreamPool
    {
        private object _lock = new();
        private Stack<ByteStream> pool = new();

        public ByteStreamPool(int length = 128)
        {
            for (int i = 0; i < length; i++)
                pool.Push(new ByteStream(128));
        }

        public ByteStream Get()
        {
            lock (_lock)
            {
                if (pool.Count == 0)
                    return new ByteStream(512);
                return pool.Pop();
            }
        }

        public void Release(ByteStream stream)
        {
            lock (_lock)
            {
                stream.EndWrite();
                pool.Push(stream);
            }
        }

        public int Count => pool.Count;
    }
}