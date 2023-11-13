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

    /// <summary>
    /// Manages a pool of DBMS connections for database queries.
    /// This class is Thread-Safe.
    /// </summary>
    public class DBMSManager
    {
        private readonly object _lock = new();
        private readonly Stack<DBMS> pool = new();
        private readonly Func<bool, DBMS> func;

        /// <summary>
        /// Initialize a pool of DBMS instances to handle database connections and operations.
        /// </summary>
        /// <param name="onFunc">Initialize the DBMS Object</param>
        /// <param name="connections">The number of connections to be created in the pool.</param>
        /// <param name="reuseTemporaryConnections">Whether temporary connections should be reused or not.</param>
        public DBMSManager(Action<DBMS> onFunc, int connections = 2, bool reuseTemporaryConnections = false)
        {
            func = (finishAfterUse) =>
            {
                DBMS dbms = new()
                {
                    finishAfterUse = !reuseTemporaryConnections && finishAfterUse
                };

                onFunc?.Invoke(dbms);
                return dbms;
            };

            for (int i = 0; i < connections; i++)
            {
                pool.Push(func(false));
            }
        }

        /// <summary>
        /// Get a DBMS instance from the pool.<br/>
        /// If there are no instances available, a temporary instance will be created.<br/>
        /// This method is Thread-Safe.
        /// </summary>
        public DBMS Get()
        {
            lock (_lock)
            {
                if (pool.Count == 0)
                {
                    OmniLogger.Print("Query: No database connections are currently available. A temporary connection will be opened to handle this query.");
                    return func(true);
                }
                else
                {
                    return pool.Pop();
                }
            }
        }

        /// <summary>
        /// Release a DBMS instance back to the pool.<br/>
        /// If the instance is temporary, it will be closed and disposed.<br/>
        /// This method is Thread-Safe.
        /// </summary>
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

        /// <summary>
        /// Close all DBMS instances in the pool.<br/>
        /// Used to close all connections when the application is closed.<br/>
        /// This method is Thread-Safe.
        /// </summary>
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