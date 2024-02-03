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
using System.Threading.Tasks;

namespace Omni.Core
{
	public class SimpleHttpProtocol
	{
		private enum HttpOption
		{
			App = 235, // node express
			Axios = 251, // js fetch
		}

		internal class Route
		{
			public Route(Action<IDataReader, IDataWriter> path, bool isSecure)
			{
				Path = path;
				IsSecure = isSecure;
			}

			internal Action<IDataReader, IDataWriter> Path { get; }
			internal bool IsSecure { get; }
		}

		// Server
		public class App
		{
			internal readonly Dictionary<string, Route> m_paths = new();
			public void Post(string path, Action<IDataReader, IDataWriter> func, bool isSecure = true)
			{
#if UNITY_EDITOR
				NetworkHelper.ThrowAnErrorIfConcurrent();
#endif
				if (!m_paths.TryAdd(path, new Route(func, isSecure)))
				{
					OmniLogger.PrintError($"Error: Route '{path}' already exists. Use a unique route for each post method.");
				}
			}
		}

		// Client
		public class Axios
		{
			internal Queue<TaskCompletionSource<IDataReader>> m_tasks = new();
			public Task<IDataReader> Post(string path, IDataWriter writer, bool isSecure = true)
			{
#if UNITY_EDITOR
				NetworkHelper.ThrowAnErrorIfConcurrent();
#endif
				TaskCompletionSource<IDataReader> tcs = new();
				m_tasks.Enqueue(tcs);
				IDataWriter internalWriter = NetworkCommunicator.DataWriterPool.Get();
				internalWriter.Write(path);
				internalWriter.Write(writer.Buffer, 0, writer.BytesWritten);
				OmniNetwork.Communicator.Internal_SendCustomMessage(HttpOption.Axios, internalWriter, isSecure ? DataDeliveryMode.SecuredWithAes : DataDeliveryMode.Secured, 0);
				NetworkCommunicator.DataWriterPool.Release(internalWriter);
				return tcs.Task;
			}
		}

		public static App Server { get; } = new();
		public static Axios Client { get; } = new();

		internal static void AddEventListener()
		{
			NetworkCallbacks.Internal_OnCustomMessage += NetworkCallbacks_Internal_OnCustomMessage;
		}

		private static void NetworkCallbacks_Internal_OnCustomMessage(bool isServer, IDataReader reader, NetworkPeer peer)
		{
			HttpOption httpOption = reader.ReadCustomMessage<HttpOption>();
			if (isServer && httpOption == HttpOption.Axios)
			{
				string path = reader.ReadString();
				if (Server.m_paths.TryGetValue(path, out Route route))
				{
					IDataWriter internalWriter = NetworkCommunicator.DataWriterPool.Get();
					route.Path(reader, internalWriter);
					OmniNetwork.Communicator.Internal_SendCustomMessage(HttpOption.App, internalWriter, peer.Id, route.IsSecure ? DataDeliveryMode.SecuredWithAes : DataDeliveryMode.Secured, 0);
					NetworkCommunicator.DataWriterPool.Release(internalWriter);
				}
				else
				{
					OmniLogger.PrintError($"Error: Route '{path}' does not exists.");
				}
			}
			else if (!isServer && httpOption == HttpOption.App)
			{
				if (Client.m_tasks.Count > 0)
				{
					TaskCompletionSource<IDataReader> tcs = Client.m_tasks.Dequeue();
					tcs.SetResult(reader);
				}
				else
				{
					throw new Exception("There are no http tasks, something is wrong ):");
				}
			}
		}

		internal static void Close()
		{
			foreach (var task in Client.m_tasks)
			{
				task.SetCanceled();
			}
		}
	}
}