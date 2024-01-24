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
	public class DatabaseManager
	{
		/// <summary>
		/// Indicates whether temporary connections are enabled or not.
		/// </summary>
		public bool enableTemporaryConnections = true;
		private readonly object _lock = new();
		private readonly Stack<Database> pool = new();
		private Func<bool, Database> builder;

		/// <summary>
		/// Initialize a pool of DBMS instances to handle database connections and operations.
		/// </summary>
		/// <param name="builder">Initialize the DBMS Object</param>
		/// <param name="connections">The number of connections to be created in the pool.</param>
		/// <param name="enableTemporaryConnectionReuse">Whether temporary connections should be reused or not.</param>
		public void Initialize(Func<Database, Database> builder, int connections = 4, bool enableTemporaryConnectionReuse = false)
		{
			this.builder = (flushTemporaryConnection) =>
			{
				return builder?.Invoke(new Database()
				{
					manager = this,
					flushTemporaryConnection = !enableTemporaryConnectionReuse && flushTemporaryConnection
				});
			};

			for (int i = 0; i < connections; i++)
			{
				pool.Push(this.builder(false));
			}
		}

		/// <summary>
		/// Get a DBMS instance from the pool.<br/>
		/// If there are no instances available, a temporary instance will be created.<br/>
		/// This method is Thread-Safe.
		/// </summary>
		public Database Get()
		{
			lock (_lock)
			{
				if (pool.Count == 0)
				{
					if (enableTemporaryConnections)
					{
						OmniLogger.Print("Query: No database connections are currently available. A temporary connection will be opened to handle this query.");
						return builder(true);
					}
					else
					{
						throw new Exception("Query: No database connections are currently available. Consider enabling temporary connections or use a single connection without concurrency.");
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
		public void Release(Database dbms)
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
				foreach (Database dbms in pool)
				{
					dbms.Close();
					dbms.Dispose();
				}
			}
		}
	}
}