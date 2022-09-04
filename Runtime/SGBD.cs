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
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Mono.Data.Sqlite;
using MySqlConnector;
using SqlKata;
using SqlKata.Compilers;
using SqlKata.Execution;
using UnityEngine;

namespace Neutron.Core
{
    public class SGBD : IDisposable
    {
        #region Fields
        private string tableName;
        private Query query;
        private QueryFactory queryFactory;
        private IDbConnection iDbConnection;
        private SqliteConnection sqliteConnection;
        private MySqlConnection mySqlConnection;
        #endregion

        #region Properties
        public Query Db
        {
            get
            {
                ThrowErrorIfNotInitialized();
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

        public SqliteConnection SQLiteConnection
        {
            get
            {
                ThrowErrorIfNotInitialized();
                return sqliteConnection;
            }
        }

        public MySqlConnection MySQLConnection
        {
            get
            {
                ThrowErrorIfNotInitialized();
                return mySqlConnection;
            }
        }
        #endregion

        public void Initialize(IDbConnection iDbConnection, Compiler compiler, string tableName, int timeout = 30)
        {
            try
            {
                this.tableName = tableName;
                this.iDbConnection = iDbConnection;
                this.iDbConnection.Open();
                query = (queryFactory = new QueryFactory(this.iDbConnection, compiler, timeout)).Query(tableName);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public void Initialize(string tableName, SGDBType sGDBType = SGDBType.SQLite, string connectionString = "Data Source=neutron_server_db.sqlite3", int timeout = 30)
        {
            try
            {
                this.tableName = tableName;
                switch (sGDBType)
                {
                    case SGDBType.SQLite:
                        {
                            sqliteConnection = new(connectionString);
                            this.iDbConnection = sqliteConnection;
                            sqliteConnection.Open();
                            query = (queryFactory = new QueryFactory(sqliteConnection, new SqliteCompiler(), timeout)).Query(tableName);
                        }
                        break;
                    case SGDBType.MariaDB:
                    case SGDBType.MySQL:
                        {
                            mySqlConnection = new MySqlConnection(connectionString);
                            this.iDbConnection = mySqlConnection;
                            mySqlConnection.Open();
                            query = (queryFactory = new QueryFactory(mySqlConnection, new MySqlCompiler(), timeout)).Query(tableName);
                        }
                        break;
                    default:
                        throw new Exception("SGDB Type not supported!");
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public void Close()
        {
            ThrowErrorIfNotInitialized();
            iDbConnection.Close();
            iDbConnection.Dispose();
        }

        public void Dispose()
        {
            Close();
        }

        private void ThrowErrorIfNotInitialized()
        {
            if (query == null || queryFactory == null || iDbConnection == null)
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