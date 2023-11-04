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

namespace Omni.Core
{
    // Data Base Management System
    public class DBMSManager
    {
        private readonly object _lock = new();
        private readonly Stack<DBMS> pool = new();
        private readonly Func<bool, DBMS> func;

        public DBMSManager(Action<DBMS> func, int connections = 2, bool reuseTemporaryConnections = false)
        {
            this.func = (finishAfterUse) =>
            {
                DBMS dbms = new DBMS();
                dbms.finishAfterUse = !reuseTemporaryConnections && finishAfterUse;
                func?.Invoke(dbms);
                return dbms;
            };

            for (int i = 0; i < connections; i++)
            {
                pool.Push(this.func(false));
            }
        }

        public DBMS Get()
        {
            lock (_lock)
            {
                if (pool.Count == 0)
                {
                    Logger.Print("Query: No database connections are currently available. A temporary connection will be opened to handle this query.");
                    return func(true);
                }
                else
                {
                    return pool.Pop();
                }
            }
        }

        public void Release(DBMS dbms)
        {
            if (!dbms.finishAfterUse)
            {
                lock (_lock)
                {
                    pool.Push(dbms);
                }
            }
            else
            {
                dbms.Dispose();
                dbms.Close();
            }
        }

        public void Close()
        {
            lock (_lock)
            {
                foreach (DBMS dbms in pool)
                {
                    dbms.Close();
                }
            }
        }

        public int Count => pool.Count;
    }
}