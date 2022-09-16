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
using System.Collections.Generic;

namespace Neutron.Core
{
    public class SGBDManager
    {
        private readonly object _lock = new();
        private readonly Stack<SGBD> pool = new();
        private readonly Func<bool, SGBD> initializer;

        public SGBDManager(Action<SGBD> initializer, int connections = 2, bool reuseTemporaryConnections = false)
        {
            this.initializer = (finishAfterUse) =>
            {
                var sgbd = new SGBD();
                initializer?.Invoke(sgbd);
                sgbd.finishAfterUse = !reuseTemporaryConnections && finishAfterUse;
                return sgbd;
            };

            for (int i = 0; i < connections; i++)
                pool.Push(this.initializer(false));
        }

        public SGBD Get()
        {
            lock (_lock)
            {
                if (pool.Count == 0)
                {
                    Logger.Print("Query: No connections available, a temporary connection will open!");
                    return initializer(true);
                }
                else
                    return pool.Pop();
            }
        }

        public void Release(SGBD sgbd)
        {
            if (!sgbd.finishAfterUse)
            {
                lock (_lock)
                {
                    pool.Push(sgbd);
                }
            }
            else
            {
                sgbd.Dispose();
                sgbd.Close();
            }
        }

        public void Close()
        {
            lock (_lock)
            {
                foreach (SGBD sgbd in pool)
                    sgbd.Close();
            }
        }

        public int Count => pool.Count;
    }
}