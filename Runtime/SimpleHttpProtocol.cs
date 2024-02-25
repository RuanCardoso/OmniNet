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

using Omni.Internal;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Omni.Core
{
	public static class SimpleHttpProtocol
	{
		enum HttpOption
		{
			Response = 14,
			Fetch = 15,
		}

		public static HttpServer Server { get; } = new HttpServer();
		public static HttpClient Client { get; } = new HttpClient();

		internal class HttpServerObject
		{
			internal HttpServerObject(Action<IDataReader, IDataWriter, NetworkPeer> exec, Func<IDataReader, IDataWriter, NetworkPeer, Task> execAsync, bool isAsync)
			{
				Exec = exec;
				ExecAsync = execAsync;
				IsAsync = isAsync;
			}

			internal bool IsAsync { get; }
			internal Action<IDataReader, IDataWriter, NetworkPeer> Exec { get; }
			internal Func<IDataReader, IDataWriter, NetworkPeer, Task> ExecAsync { get; }
		}

		public class HttpServer
		{
			internal Dictionary<string, HttpServerObject> m_routes = new();
			public void Post(string route, Action<IDataReader, IDataWriter, NetworkPeer> res)
			{
#if UNITY_EDITOR
				NetworkHelper.ThrowAnErrorIfConcurrent();
#endif
				if (res == null)
					throw new ArgumentNullException("request or response is null");

				if (!m_routes.TryAdd(route, new HttpServerObject(res, default, false)))
				{
					throw new NotSupportedException($"The route {route} is global and must be unique.");
				}
			}

			public void PostAsync(string route, Func<IDataReader, IDataWriter, NetworkPeer, Task> resAsync)
			{
#if UNITY_EDITOR
				NetworkHelper.ThrowAnErrorIfConcurrent();
#endif
				if (resAsync == null)
					throw new ArgumentNullException("request or response is null");

				if (!m_routes.TryAdd(route, new HttpServerObject(default, resAsync, true)))
				{
					throw new NotSupportedException($"The route {route} is global and must be unique.");
				}
			}
		}

		public class HttpClient
		{
			internal int m_requestId = int.MinValue;
			internal Dictionary<int, Action<IDataReader>> m_results = new();
			public void Post(string route, Action<IDataWriter> request, Action<IDataReader> response, DataDeliveryMode dataDeliveryMode = DataDeliveryMode.ReliableEncryptedOrdered)
			{
#if UNITY_EDITOR
				NetworkHelper.ThrowAnErrorIfConcurrent();
#endif
				if (request == null || response == null)
					throw new ArgumentNullException("request or response is null");
				if (dataDeliveryMode == DataDeliveryMode.Unreliable)
					throw new NotSupportedException("The 'Unreliable' data delivery mode is not supported.");

				int requestId = m_requestId;
				if (m_results.TryAdd(requestId, response))
				{
					IDataWriter writer = NetworkCommunicator.DataWriterPool.Get();
					writer.Write7BitEncodedInt(requestId);
					writer.Write(route);
					request(writer);
					OmniNetwork.Communicator.Internal_SendCustomMessage(HttpOption.Fetch, writer, dataDeliveryMode, 0);
					NetworkCommunicator.DataWriterPool.Release(writer);
					m_requestId++;
				}
				else
				{
					throw new NotSupportedException($"The post method!");
				}
			}

			public Task<IDataReader> PostAsync(string route, Action<IDataWriter> request, int timeout = 3000, DataDeliveryMode dataDeliveryMode = DataDeliveryMode.ReliableEncryptedOrdered)
			{
				TaskCompletionSource<IDataReader> tcs = new();
				CancellationTokenSource cts = new();
				Client.Post(route, request, (res) =>
				{
					if (cts != null && !cts.IsCancellationRequested)
					{
						cts.Cancel();
						tcs.SetResult(res);
						cts.Dispose();
					}
				}, dataDeliveryMode);

				Task.Run(async () =>
				{
					await Task.Delay(timeout, cts.Token);
					if (cts != null && !cts.IsCancellationRequested)
					{
						cts.Cancel();
						tcs.SetException(new TimeoutException());
						cts.Dispose();
					}
				}, cts.Token);
				return tcs.Task;
			}
		}

		internal static void AddEventListener()
		{
			NetworkCallbacks.Internal_OnCustomMessageReceived += OnRoute;
		}

		private static async void OnRoute(bool isServer, IDataReader reader, NetworkPeer peer, DataDeliveryMode deliveryMode)
		{
			HttpOption httpOption = reader.ReadCustomMessage<HttpOption>(out int lastPos);
			if (isServer && httpOption == HttpOption.Fetch)
			{
				int requestId = reader.Read7BitEncodedInt();
				string route = reader.ReadString();

				if (Server.m_routes.TryGetValue(route, out HttpServerObject httpServerObject))
				{
					#region Writers
					IDataWriter httpHeader = NetworkCommunicator.DataWriterPool.Get();
					IDataWriter httpResponse = NetworkCommunicator.DataWriterPool.Get();
					#endregion

					#region Initialize
					httpHeader.Write7BitEncodedInt(requestId);
					httpHeader.Write(route);
					if (httpServerObject.IsAsync) await httpServerObject.ExecAsync(reader, httpResponse, peer);
					else httpServerObject.Exec(reader, httpResponse, peer);
					httpHeader.Write(httpResponse.Buffer, 0, httpResponse.BytesWritten);
					if (httpResponse.BytesWritten > 0)
						OmniNetwork.Communicator.Internal_SendCustomMessage(HttpOption.Response, httpHeader, peer.Id, deliveryMode, 0);
					else OmniLogger.PrintError("Error: The requested route does not have a response from the server.");
					#endregion

					#region Recycle
					NetworkCommunicator.DataWriterPool.Release(httpResponse);
					NetworkCommunicator.DataWriterPool.Release(httpHeader);
					#endregion
				}
				else
				{
					OmniLogger.PrintError($"The route {route} does not exists.");
				}
			}
			else if (!isServer && httpOption == HttpOption.Response)
			{
				int requestId = reader.Read7BitEncodedInt();
				string route = reader.ReadString();
				if (Client.m_results.Remove(requestId, out var exec))
				{
					exec(reader);
				}
				else
				{
					OmniLogger.PrintError($"Client Response -> The route {route} does not exists.");
				}
			}
			else reader.Position = lastPos;
		}

		internal static void Close() { }
	}
}