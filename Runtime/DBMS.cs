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
    // Data Base Management System
    public class DBMS : IDisposable
    {
        private string tableName;
        private IDbConnection iDbConnection;
        private Query query;
        private QueryFactory queryFactory;
        internal bool finishAfterUse;

        public Query Db
        {
            get
            {
                query = Factory.Query(tableName);
                return query;
            }
        }

        public QueryFactory Factory
        {
            get
            {
                ThrowErrorIfNotInitialized();
                return queryFactory;
            }
        }

        public IDbConnection Connection
        {
            get
            {
                ThrowErrorIfNotInitialized();
                return iDbConnection;
            }
        }

        public void Initialize(IDbConnection iDbConnection, Compiler compiler, string tableName, int timeout = 30)
        {
            try
            {
                this.tableName = tableName;
                this.iDbConnection = iDbConnection;
                this.iDbConnection.Open();
                queryFactory = new QueryFactory(this.iDbConnection, compiler, timeout);
            }
            catch (Exception ex)
            {
                OmniLogger.PrintError(ex.Message);
                OmniLogger.PrintError(ex.InnerException.Message);
            }
        }

        public void Initialize(string tableName, SGDBType dBType = SGDBType.SQLite, string connectionString = "Data Source=omni_server_db.sqlite3", int timeout = 30)
        {
            try
            {
                this.tableName = tableName;
                switch (dBType)
                {
                    case SGDBType.SQLite:
                        {
                            Initialize(new SqliteConnection(connectionString), new SqliteCompiler(), tableName, timeout);
                        }
                        break;
                    case SGDBType.MariaDB:
                    case SGDBType.MySQL:
                        {
                            Initialize(new MySqlConnection(connectionString), new MySqlCompiler(), tableName, timeout);
                        }
                        break;
                    default:
                        throw new Exception("SGDB Type not supported!");
                }
            }
            catch (Exception ex)
            {
                OmniLogger.PrintError(ex.Message);
                OmniLogger.PrintError(ex.InnerException.Message);
            }
        }

        public void Close()
        {
            iDbConnection.Close();
        }

        public void Dispose()
        {
            iDbConnection.Dispose();
        }

        private void ThrowErrorIfNotInitialized()
        {
            if (queryFactory == null || iDbConnection == null)
                throw new Exception($"Call \"{nameof(Initialize)}()\" before it! -> {iDbConnection.State}");
            if (iDbConnection != null)
            {
                if (iDbConnection.State != ConnectionState.Open)
                    throw new Exception($"The database is not connected! -> {iDbConnection.State}");
            }
        }
    }

    public enum SGDBType
    {
        MariaDB,
        MySQL,
        SQLite,
    }
}