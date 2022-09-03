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
    public static class SGBD
    {
        #region Fields
        private static Query query;
        private static QueryFactory queryFactory;
        private static IDbConnection iDbConnection;
        #endregion

        #region Properties
        public static Query Db
        {
            get
            {
                ThrowErrorIfNotInitialized();
                return query;
            }
        }

        public static QueryFactory Factory
        {
            get
            {
                ThrowErrorIfNotInitialized();
                return queryFactory;
            }
        }

        public static IDbConnection Connection
        {
            get
            {
                ThrowErrorIfNotInitialized();
                return iDbConnection;
            }
        }
        #endregion

        public static void Initialize(IDbConnection iDbConnection, Compiler compiler, string tableName, int timeout = 30)
        {
            try
            {
                SGBD.iDbConnection = iDbConnection;
                SGBD.iDbConnection.Open();
                query = (queryFactory = new QueryFactory(SGBD.iDbConnection, compiler, timeout)).Query(tableName);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static void Initialize(string tableName, int timeout = 30)
        {
            Initialize(new SqliteConnection("Data Source=neutron_server_db.sqlite3"), new SqliteCompiler(), tableName, timeout);
        }

        static void ThrowErrorIfNotInitialized()
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