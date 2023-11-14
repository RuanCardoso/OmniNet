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
using System.Threading;

namespace Omni.Core
{
    /// <summary>
    /// Manages a pool of DBMS connections for database queries.
    /// This class is Thread-Safe.
    /// </summary>
    public class DBMSManager
    {
        /// <summary>
        /// Indicates whether temporary connections are enabled or not.
        /// </summary>
        public bool enableTemporaryConnections = true;
        private readonly object _lock = new();
        private readonly Stack<DBMS> pool = new();
        private readonly Func<bool, DBMS> onInit;

        /// <summary>
        /// Initialize a pool of DBMS instances to handle database connections and operations.
        /// </summary>
        /// <param name="onInit">Initialize the DBMS Object</param>
        /// <param name="connections">The number of connections to be created in the pool.</param>
        /// <param name="enableTemporaryConnectionReuse">Whether temporary connections should be reused or not.</param>
        public DBMSManager(Action<DBMS> onInit, int connections = 4, bool enableTemporaryConnectionReuse = false)
        {
            this.onInit = (flushTemporaryConnection) =>
            {
                DBMS dbms = new()
                {
                    manager = this,
                    flushTemporaryConnection = !enableTemporaryConnectionReuse && flushTemporaryConnection
                };

                onInit?.Invoke(dbms);
                return dbms;
            };

            for (int i = 0; i < connections; i++)
            {
                // Open a connection to the database
                pool.Push(this.onInit(false));
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
                    if (enableTemporaryConnections)
                    {
                        OmniLogger.Print("Query: No database connections are currently available. A temporary connection will be opened to handle this query.");
                        return onInit(true);
                    }
                    else
                    {
                        throw new Exception("Query: No database connections are currently available. Consider enabling temporary connections or use sequential mode.");
                    }
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
            if (!dbms.flushTemporaryConnection)
            {
                lock (_lock)
                {
                    pool.Push(dbms);
                }
            }
            else
            {
                dbms.Close();
                dbms.Dispose();
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
    }
}