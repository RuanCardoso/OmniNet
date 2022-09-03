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
using Mono.Data.Sqlite;
using SqlKata;
using SqlKata.Compilers;
using SqlKata.Execution;
using UnityEngine;

namespace Neutron.Core
{
    public class SGBD
    {
        #region Fields
        private string tableName;
        private Query query;
        private QueryFactory queryFactory;
        private IDbConnection iDbConnection;
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
        #endregion

        public SGBD(string tableName) => this.tableName = tableName;
        public void Initialize(IDbConnection iDbConnection, Compiler compiler, int timeout = 30)
        {
            try
            {
                this.iDbConnection = iDbConnection;
                this.iDbConnection.Open();
                query = (queryFactory = new QueryFactory(this.iDbConnection, compiler, timeout)).Query(tableName);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public void Initialize(int timeout = 30)
        {
            Initialize(new SqliteConnection("Data Source=neutron_server_db.sqlite3"), new SqliteCompiler(), timeout);
        }

        void ThrowErrorIfNotInitialized()
        {
            if (query == null || queryFactory == null || iDbConnection == null)
                throw new Exception($"Call \"{nameof(Initialize)}()\" before it!");
            if (iDbConnection != null)
            {
                if (iDbConnection.State != ConnectionState.Open)
                    throw new Exception("The database is not connected!");
            }
        }
    }

    public enum SGDBType
    {
        MySQL,
        SQLite,
        PostgreSQL,
        Oracle,
        Firebird,
        SQLServer
    }
}