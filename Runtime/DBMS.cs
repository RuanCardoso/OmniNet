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

using Mono.Data.Sqlite;
using MySqlConnector;
using Omni.Compilers;
using Omni.Execution;
using System;
using System.Data;

namespace Omni.Core
{
    /// <summary>
    /// Represents a database management system that provides a set of methods to interact with a database.
    /// </summary>
    public class DBMS : IDisposable
    {
        private string tableName;
        private IDbConnection iDbConnection;
        private Query query;
        private QueryFactory queryFactory;
        internal DBMSManager manager;
        internal bool flushTemporaryConnection;

        /// <summary>
        /// Represents a database query, used to perform operations on a database.<br/>
        /// Operations such as: Insert, Update, Delete, Select, Where, etc...
        /// </summary>
        public Query Db
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
        public void Initialize(IDbConnection iDbConnection, Compiler compiler, string tableName, int timeout = 30)
        {
            try
            {
                this.tableName = tableName;
                this.iDbConnection = iDbConnection;
                this.iDbConnection.Open();
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
                    OmniLogger.PrintError(ex.InnerException.Message);
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
        public void Initialize(string tableName, DBMSOp dBType = DBMSOp.SQLite, string connectionString = "Data Source=omni_server_db.sqlite3", int timeout = 30)
        {
            this.tableName = tableName;
            switch (dBType)
            {
                case DBMSOp.SQLite:
                    {
                        Initialize(new SqliteConnection(connectionString), new SqliteCompiler(), tableName, timeout);
                    }
                    break;
                case DBMSOp.MariaDB:
                case DBMSOp.MySQL:
                    {
                        Initialize(new MySqlConnection(connectionString), new MySqlCompiler(), tableName, timeout);
                    }
                    break;
                default:
                    throw new Exception("SGDB Type not supported!");
            }
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
        /// Releases the DBMS back to the DBMSManager.
        /// </summary>
        public void Release()
        {
            manager?.Release(this);
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

    public enum DBMSOp
    {
        MariaDB,
        MySQL,
        SQLite,
    }
}