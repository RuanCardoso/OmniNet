using FirebirdSql.Data.FirebirdClient;
using Mono.Data.Sqlite;
using MySqlConnector;
using Newtonsoft.Json;
using Npgsql;
using Omni.Execution;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Omni.Core
{
	public static class DatabaseExtensions
	{
		public static Task OpenAsync(this IDbConnection dbConnection)
		{
			return dbConnection switch
			{
				MySqlConnection connection => connection.OpenAsync(),
				SqlConnection connection => connection.OpenAsync(),
				OracleConnection connection => connection.OpenAsync(),
				NpgsqlConnection connection => connection.OpenAsync(),
				SqliteConnection connection => connection.OpenAsync(),
				FbConnection connection => connection.OpenAsync(),
				_ => throw new NotImplementedException("Asynchronous implementation is not supported in this database!"),
			};
		}

		/// <summary>
		/// Maps the first result of a query to the specified type.
		/// </summary>
		/// <typeparam name="T">The type to map the result to.</typeparam>
		/// <param name="query">The query to execute.</param>
		/// <param name="transaction">The database transaction (optional).</param>
		/// <param name="timeout">The command timeout (optional).</param>
		/// <returns>The mapped result of type T.</returns>
		public static T MapFirstResultTo<T>(this Query query, IDbTransaction transaction = null, int? timeout = null)
		{
			var toJsonObject = query.First<object>(transaction, timeout);
			var fromJsonObject = JsonConvert.SerializeObject(toJsonObject);
			return JsonConvert.DeserializeObject<T>(fromJsonObject);
		}

		/// <summary>
		/// Maps all the results of a database query to a collection of objects of type T.
		/// </summary>
		/// <typeparam name="T">The type of objects to map the results to.</typeparam>
		/// <param name="query">The database query.</param>
		/// <param name="transaction">The database transaction (optional).</param>
		/// <param name="timeout">The timeout for the query (optional).</param>
		/// <returns>A collection of objects of type T.</returns>
		public static IEnumerable<T> MapAllResultsTo<T>(this Query query, IDbTransaction transaction = null, int? timeout = null)
		{
			var toJsonObject = query.Get<object>(transaction, timeout);
			var fromJsonObject = JsonConvert.SerializeObject(toJsonObject);
			return JsonConvert.DeserializeObject<IEnumerable<T>>(fromJsonObject);
		}

		/// <summary>
		/// Maps the paginated results of a query to a specified type.
		/// </summary>
		/// <typeparam name="T">The type to map the results to.</typeparam>
		/// <param name="query">The query to paginate.</param>
		/// <param name="page">The page number to retrieve.</param>
		/// <param name="perPage">The number of results per page.</param>
		/// <param name="transaction">The database transaction to use.</param>
		/// <param name="timeout">The timeout for the query.</param>
		/// <returns>An enumerable collection of mapped results.</returns>
		public static IEnumerable<T> MapPageResultsTo<T>(this Query query, int page, int perPage = 25, IDbTransaction transaction = null, int? timeout = null)
		{
			var toJsonObject = query.Paginate<object>(page, perPage, transaction, timeout);
			var fromJsonObject = JsonConvert.SerializeObject(toJsonObject.List);
			return JsonConvert.DeserializeObject<IEnumerable<T>>(fromJsonObject);
		}

		/// <summary>
		/// Processes a query in chunks, invoking a function for each chunk of elements.
		/// </summary>
		/// <typeparam name="T">The type of elements in the query.</typeparam>
		/// <param name="query">The query to process.</param>
		/// <param name="chunkSize">The size of each chunk.</param>
		/// <param name="func">The function to invoke for each chunk of elements. The function should return true to continue processing, or false to stop processing.</param>
		/// <param name="transaction">The database transaction to use.</param>
		/// <param name="timeout">The timeout for the query.</param>
		public static void ProcessInChunks<T>(this Query query, int chunkSize, Func<IEnumerable<T>, int, bool> func, IDbTransaction transaction = null, int? timeout = null)
		{
			query.Chunk<T>(chunkSize, func, transaction, timeout);
		}

		/// <summary>
		/// Processes a query in chunks, invoking the specified action for each chunk of items.
		/// </summary>
		/// <typeparam name="T">The type of items in the query.</typeparam>
		/// <param name="query">The query to process.</param>
		/// <param name="chunkSize">The size of each chunk.</param>
		/// <param name="action">The action to invoke for each chunk of items.</param>
		/// <param name="transaction">The database transaction to use (optional).</param>
		/// <param name="timeout">The timeout for the query (optional).</param>
		public static void ProcessInChunks<T>(this Query query, int chunkSize, Action<IEnumerable<T>, int> action, IDbTransaction transaction = null, int? timeout = null)
		{
			query.Chunk<T>(chunkSize, action, transaction, timeout);
		}

		/// <summary>
		/// Maps a Query object to a Row object.
		/// </summary>
		/// <param name="query">The Query object to map.</param>
		/// <returns>The mapped Row object.</returns>
		public static Row MapToRow(this Query query, IDbTransaction transaction = null, int? timeout = null)
		{
			return MapFirstResultTo<Row>(query, transaction, timeout);
		}

		/// <summary>
		/// Maps the query results to a collection of rows.
		/// </summary>
		/// <param name="query">The query to map.</param>
		/// <returns>A collection of rows.</returns>
		public static IEnumerable<Row> MapToRows(this Query query, IDbTransaction transaction = null, int? timeout = null)
		{
			return MapAllResultsTo<Row>(query, transaction, timeout);
		}
	}
}