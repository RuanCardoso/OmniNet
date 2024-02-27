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

using FirebirdSql.Data.FirebirdClient;
using Mono.Data.Sqlite;
using MySqlConnector;
using Npgsql;
using Omni.Compilers;
using Omni.Execution;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Omni.Core
{
	/// <summary>
	/// Represents a database management system that provides a set of methods to interact with a database.
	/// </summary>
	public class Database : IDisposable
	{
		private string tableName;
		private IDbConnection iDbConnection;
		private Query query;
		private QueryFactory queryFactory;

		internal DatabaseManager manager;
		internal bool flushTemporaryConnection;

		/// <summary>
		/// Represents a database query, used to perform operations on a database.<br/>
		/// Operations such as: Insert, Update, Delete, Select, Where, etc...
		/// </summary>
		public Query _
		{
			get
			{
				query = Factory.Query(tableName);
				return query;
			}
		}

		/// <summary>
		/// Represents a factory for generating SQL queries for database operations.
		/// This class encapsulates query creation logic for various database operations.
		/// </summary>
		public QueryFactory Factory
		{
			get
			{
				ThrowErrorIfNotInitialized();
				return queryFactory;
			}
		}

		/// <summary>
		/// Represents a connection to a database.
		/// </summary>
		public IDbConnection Connection
		{
			get
			{
				ThrowErrorIfNotInitialized();
				return iDbConnection;
			}
		}

		/// <summary>
		/// Initializes the DBMS with the provided IDbConnection, Compiler, table name and timeout.
		/// </summary>
		/// <param name="iDbConnection">The IDbConnection to be used by the DBMS.</param>
		/// <param name="compiler">The Compiler to be used by the DBMS.</param>
		/// <param name="tableName">The name of the table to be used by the DBMS.</param>
		/// <param name="timeout">The timeout value in seconds for the DBMS queries. Default is 30 seconds.</param>
		private void Initialize(IDbConnection iDbConnection, Compiler compiler, string tableName, int timeout = 30)
		{
			try
			{
				this.tableName = tableName;
				this.iDbConnection = iDbConnection;
				this.iDbConnection.Open();
				// Initialize the query builder to openned connection!
				if (this.iDbConnection.State == ConnectionState.Open)
				{
					OmniLogger.Print($"Connection to the database established successfully for table '{tableName}'.");
					queryFactory = new QueryFactory(this.iDbConnection, compiler, timeout);
				}
				else
				{
					OmniLogger.Print($"Failed to establish a connection for table '{tableName}'.");
				}
			}
			catch (Exception ex)
			{
				OmniLogger.PrintError($"Error while initializing the DBMS: {ex.Message}");
				if (ex.InnerException != null)
				{
					OmniLogger.PrintError(ex.InnerException.Message);
				}
			}
		}

		/// <summary>
		/// Initializes the DBMS with the provided IDbConnection, Compiler, table name and timeout.
		/// </summary>
		/// <param name="iDbConnection">The IDbConnection to be used by the DBMS.</param>
		/// <param name="compiler">The Compiler to be used by the DBMS.</param>
		/// <param name="tableName">The name of the table to be used by the DBMS.</param>
		/// <param name="timeout">The timeout value in seconds for the DBMS queries. Default is 30 seconds.</param>
		private Task<Database> InitializeAsync(IDbConnection iDbConnection, Compiler compiler, string tableName, int timeout = 30)
		{
			try
			{
				this.tableName = tableName;
				this.iDbConnection = iDbConnection;
				// async open connection to ConcurrentDatabaseManager!
				return Task.Run(async () =>
				{
					await this.iDbConnection.OpenAsync();
					// Initialize the query builder to openned connection!
					if (this.iDbConnection.State == ConnectionState.Open)
					{
						OmniLogger.PrintWarning($"Connection to the database established successfully for table '{tableName}'.");
						queryFactory = new QueryFactory(this.iDbConnection, compiler, timeout);
					}
					else
					{
						OmniLogger.Print($"Failed to establish a connection for table '{tableName}'.");
					}
					return this;
				});
			}
			catch (Exception ex)
			{
				OmniLogger.PrintError($"Error while initializing the DBMS: {ex.Message}");
				if (ex.InnerException != null)
				{
					OmniLogger.PrintError(ex.InnerException.Message);
				}
				return Task.FromException<Database>(ex);
			}
		}

		/// <summary>
		/// Initializes the DBMS with the specified parameters.<br/>
		/// eg SQLite: Initialize("table_name", SGDBType.SQLite, "Data Source=omni_server_db.sqlite3")<br/>
		/// eg MariaDb/Mysql: Initialize("table_name", SGDBType.MariaDB, "Server=localhost;Database=omni_server_db;Uid=root;Pwd=123456;")<br/>
		/// </summary>
		/// <param name="tableName">The name of the table to be used within the database.</param>
		/// <param name="dBType">The specific type of the database (e.g., MySQL, SQL Server, PostgreSQL).</param>
		/// <param name="connectionString">The connection string used to access the specified database.</param>
		/// <param name="timeout">The timeout value (in seconds) for database operations like Insert, Update, etc. Default is 30 seconds.</param>
		public void Initialize(string tableName, DatabaseType dBType, string connectionString, int timeout = 30, bool useLegacyPagination = false)
		{
			this.tableName = tableName;
			switch (dBType)
			{
				case DatabaseType.Firebird:
					Initialize(new FbConnection(connectionString), new FirebirdCompiler(), tableName, timeout);
					break;
				case DatabaseType.Oracle:
					Initialize(new OracleConnection(connectionString), new OracleCompiler() { UseLegacyPagination = useLegacyPagination }, tableName, timeout); ;
					break;
				case DatabaseType.PostgreSql:
					Initialize(new NpgsqlConnection(connectionString), new PostgresCompiler(), tableName, timeout);
					break;
				case DatabaseType.SqlServer:
					Initialize(new SqlConnection(connectionString), new SqlServerCompiler() { UseLegacyPagination = useLegacyPagination }, tableName, timeout);
					break;
				case DatabaseType.SQLite:
					Initialize(new SqliteConnection(connectionString), new SqliteCompiler(), tableName, timeout);
					break;
				case DatabaseType.MariaDb:
				case DatabaseType.MySql:
					Initialize(new MySqlConnection(connectionString), new MySqlCompiler(), tableName, timeout);
					break;
				default:
					throw new Exception("DatabaseType Type not supported!");
			}
		}

		/// <summary>
		/// Initializes the DBMS with the specified parameters.<br/>
		/// eg SQLite: Initialize("table_name", DatabaseType.SQLite, "Data Source=omni_server_db.sqlite3")<br/>
		/// eg MariaDb/Mysql: Initialize("table_name", DatabaseType.MariaDB, "Server=localhost;Database=omni_server_db;Uid=root;Pwd=123456;")<br/>
		/// </summary>
		/// <param name="tableName">The name of the table to be used within the database.</param>
		/// <param name="dBType">The specific type of the database (e.g., MySQL, SQL Server, PostgreSQL).</param>
		/// <param name="connectionString">The connection string used to access the specified database.</param>
		/// <param name="timeout">The timeout value (in seconds) for database operations like Insert, Update, etc. Default is 30 seconds.</param>
		public Task<Database> InitializeAsync(string tableName, DatabaseType dBType, string connectionString, int timeout = 30, bool useLegacyPagination = false)
		{
			this.tableName = tableName;
			switch (dBType)
			{
				case DatabaseType.Firebird:
					return InitializeAsync(new FbConnection(connectionString), new FirebirdCompiler(), tableName, timeout);
				case DatabaseType.Oracle:
					return InitializeAsync(new OracleConnection(connectionString), new OracleCompiler() { UseLegacyPagination = useLegacyPagination }, tableName, timeout); ;
				case DatabaseType.PostgreSql:
					return InitializeAsync(new NpgsqlConnection(connectionString), new PostgresCompiler(), tableName, timeout);
				case DatabaseType.SqlServer:
					return InitializeAsync(new SqlConnection(connectionString), new SqlServerCompiler() { UseLegacyPagination = useLegacyPagination }, tableName, timeout);
				case DatabaseType.SQLite:
					return InitializeAsync(new SqliteConnection(connectionString), new SqliteCompiler(), tableName, timeout);
				case DatabaseType.MariaDb:
				case DatabaseType.MySql:
					return InitializeAsync(new MySqlConnection(connectionString), new MySqlCompiler(), tableName, timeout);
				default:
					throw new Exception("DatabaseType Type not supported!");
			}
		}

		/// <summary>
		/// Initializes the DBMS with the specified parameters.<br/>
		/// eg SQLite: Initialize("table_name", DatabaseType.SQLite, "Data Source=omni_server_db.sqlite3")<br/>
		/// </summary>
		/// <param name="tableName">The name of the table to be used within the database.</param>
		/// <param name="timeout">The timeout value (in seconds) for database operations like Insert, Update, etc. Default is 30 seconds.</param>
		public void Initialize(string tableName, SqliteConnection sqliteConnection, int timeout = 30)
		{
			Initialize(new SqliteConnection(sqliteConnection), new SqliteCompiler(), tableName, timeout);
		}

		/// <summary>
		/// Initializes the DBMS with the specified parameters.<br/>
		/// eg SQLite: Initialize("table_name", SGDBType.SQLite, "Data Source=omni_server_db.sqlite3")<br/>
		/// </summary>
		/// <param name="tableName">The name of the table to be used within the database.</param>
		/// <param name="timeout">The timeout value (in seconds) for database operations like Insert, Update, etc. Default is 30 seconds.</param>
		public Task<Database> InitializeAsync(string tableName, SqliteConnection sqliteConnection, int timeout = 30)
		{
			return InitializeAsync(new SqliteConnection(sqliteConnection), new SqliteCompiler(), tableName, timeout);
		}

		/// <summary>
		/// Initializes the DBMS with the specified parameters.<br/>
		/// </summary>
		/// <param name="tableName">The name of the table to be used within the database.</param>
		/// <param name="connectionString">The connection string used to access the specified database.</param>
		/// <param name="timeout">The timeout value (in seconds) for database operations like Insert, Update, etc. Default is 30 seconds.</param>
		public void Initialize(string tableName, SqlCredential sqlCredential, string connectionString, int timeout = 30, bool useLegacyPagination = false)
		{
			Initialize(new SqlConnection(connectionString, sqlCredential), new SqlServerCompiler() { UseLegacyPagination = useLegacyPagination }, tableName, timeout);
		}

		/// <summary>
		/// Initializes the DBMS with the specified parameters.<br/>
		/// </summary>
		/// <param name="tableName">The name of the table to be used within the database.</param>
		/// <param name="connectionString">The connection string used to access the specified database.</param>
		/// <param name="timeout">The timeout value (in seconds) for database operations like Insert, Update, etc. Default is 30 seconds.</param>
		public Task<Database> InitializeAsync(string tableName, SqlCredential sqlCredential, string connectionString, int timeout = 30, bool useLegacyPagination = false)
		{
			return InitializeAsync(new SqlConnection(connectionString, sqlCredential), new SqlServerCompiler() { UseLegacyPagination = useLegacyPagination }, tableName, timeout);
		}

		/// <summary>
		/// Initializes the DBMS with the specified parameters.<br/>
		/// </summary>
		/// <param name="tableName">The name of the table to be used within the database.</param>
		/// <param name="connectionString">The connection string used to access the specified database.</param>
		/// <param name="timeout">The timeout value (in seconds) for database operations like Insert, Update, etc. Default is 30 seconds.</param>
		public void Initialize(string tableName, OracleCredential oracleCredential, string connectionString, int timeout = 30, bool useLegacyPagination = false)
		{
			Initialize(new OracleConnection(connectionString, oracleCredential), new OracleCompiler() { UseLegacyPagination = useLegacyPagination }, tableName, timeout);
		}

		/// <summary>
		/// Initializes the DBMS with the specified parameters.<br/>
		/// </summary>
		/// <param name="tableName">The name of the table to be used within the database.</param>
		/// <param name="connectionString">The connection string used to access the specified database.</param>
		/// <param name="timeout">The timeout value (in seconds) for database operations like Insert, Update, etc. Default is 30 seconds.</param>
		public Task<Database> InitializeAsync(string tableName, OracleCredential oracleCredential, string connectionString, int timeout = 30, bool useLegacyPagination = false)
		{
			return InitializeAsync(new OracleConnection(connectionString, oracleCredential), new OracleCompiler() { UseLegacyPagination = useLegacyPagination }, tableName, timeout);
		}

		/// <summary>
		/// Executes a SQL query and returns the number of rows affected.
		/// </summary>
		/// <param name="query">The SQL query to execute.</param>
		/// <param name="param">The parameters to use in the query.</param>
		/// <param name="transaction">The transaction to use for the command.</param>
		/// <param name="timeout">The command timeout (in seconds).</param>
		/// <returns>The number of rows affected.</returns>
		public int Run(string query, object param = null, IDbTransaction transaction = null, int? timeout = null)
		{
			ThrowErrorIfNotInitialized();
			return Factory.Statement(query, param, transaction, timeout);
		}

		/// <summary>
		/// Executes a SQL query and returns the number of rows affected.
		/// </summary>
		/// <param name="query">The SQL query to execute.</param>
		/// <param name="param">The parameters to use in the query.</param>
		/// <param name="transaction">The transaction to use for the command.</param>
		/// <param name="timeout">The command timeout (in seconds).</param>
		/// <returns>The number of rows affected.</returns>
		public Task<int> RunAsync(string query, object param = null, IDbTransaction transaction = null, int? timeout = null)
		{
			ThrowErrorIfNotInitialized();
			return Factory.StatementAsync(query, param, transaction, timeout);
		}

		/// <summary>
		/// Returns the raw SQL string of a given Query object.
		/// </summary>
		/// <param name="query">The Query object to compile.</param>
		/// <returns>The raw SQL string.</returns>
		public string GetRawSql(Query query)
		{
			return queryFactory.Compiler.Compile(query).RawSql;
		}

		/// <summary>
		/// Returns the SQL string representation of the given Query object.
		/// </summary>
		/// <param name="query">The Query object to compile.</param>
		/// <returns>The SQL string representation of the given Query object.</returns>
		public string GetSql(Query query)
		{
			return queryFactory.Compiler.Compile(query).Sql;
		}

		/// <summary>
		/// Starts a new persistent connection to the database. This connection must not be terminated.
		/// </summary>
		/// <returns></returns>
		public static Database New()
		{
			return new Database();
		}

		/// <summary>
		/// Closes the database connection.
		/// </summary>
		public void Close()
		{
			iDbConnection.Close();
		}

		/// <summary>
		/// Releases all resources used by the DBMS.
		/// </summary>
		public void Dispose()
		{
			iDbConnection.Dispose();
		}

		/// <summary>
		/// Releases the DBMS back to the Manager or Close.
		/// </summary>
		public void Release()
		{
			if (manager != null)
			{
				manager.Release(this);
			}
			else
			{
				Close();
				Dispose();
			}
		}

		private void ThrowErrorIfNotInitialized()
		{
			if (queryFactory == null || iDbConnection == null)
			{
				throw new Exception($"Call \"{nameof(Initialize)}()\" before accessing the QueryFactory! Connection state: {iDbConnection?.State}");
			}

			if (iDbConnection != null)
			{
				switch (iDbConnection.State)
				{
					case ConnectionState.Open:
						// Database connection is open.
						break;
					case ConnectionState.Closed:
						OmniLogger.PrintError($"The connection to the database is closed for table '{tableName}'.");
						// The database connection is closed; unable to perform operations.
						break;
					case ConnectionState.Broken:
						OmniLogger.PrintError($"The connection to the database is broken for table '{tableName}'.");
						// The database connection is broken; requires re-establishment.
						break;
					case ConnectionState.Connecting:
						OmniLogger.PrintError($"The connection to the database is currently in the process of establishing for table '{tableName}'.");
						// The database connection is in the process of establishing a connection.
						break;
					case ConnectionState.Executing:
						OmniLogger.PrintError($"The connection to the database is currently executing a command for table '{tableName}'.");
						// The database connection is executing a command.
						break;
					case ConnectionState.Fetching:
						OmniLogger.PrintError($"The connection to the database is currently fetching data for table '{tableName}'.");
						// The database connection is actively retrieving data.
						break;
					default:
						break;
				}
			}
		}
	}

	public enum DatabaseType
	{
		SqlServer,
		MariaDb,
		MySql,
		PostgreSql,
		Oracle,
		SQLite,
		Firebird,
	}
}