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
using Omni.Database;
using System;
using System.Data;
using UnityEngine;

namespace Omni.Core
{
    public class SGBD : IDisposable
    {
        #region Fields
        private string tableName;
        private Query query;
        private QueryFactory queryFactory;
        private IDbConnection iDbConnection;
        internal bool finishAfterUse;
        #endregion

        #region Properties
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
        #endregion

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
                Debug.LogException(ex);
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
                        Initialize(new SqliteConnection(connectionString), new SqliteCompiler(), tableName, timeout);
                        break;
                    case SGDBType.MariaDB:
                    case SGDBType.MySQL:
                        Initialize(new MySqlConnection(connectionString), new MySqlCompiler(), tableName, timeout);
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