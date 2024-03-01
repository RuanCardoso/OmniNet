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

#pragma warning disable

using LiteNetLib;
using NativeWebSocket;
using Omni.Core;
using Omni.Internal.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;
using static Omni.Internal.Transport.WebTransport;
using Client = NativeWebSocket;
using Server = WebSocketSharp.Server;

namespace Omni.Internal.Transport
{
	// This class uses WebSocketSharp.Server for the server.
	// and Native WebSocket for the client, so we can support WebGl.
	internal class WebTransport : ITransport, ITransportClient<WebTransportClient<WebTransport.PeerBehaviour>>
	{
		internal class PeerBehaviour : Server.WebSocketBehavior
		{
			internal WebTransport WebTransport { get; set; }
			internal Client.WebSocket WebClient { get; set; }
			internal bool IsServer { get; set; }
			private bool IsDisconnected { get; set; }

			public PeerBehaviour()
			{
			}

			public PeerBehaviour(WebTransport webTransport, Client.WebSocket webClient, bool isServer)
			{
				WebTransport = webTransport;
				WebClient = webClient;
				IsServer = isServer;
			}

			protected override void OnOpen()
			{
				OmniNetwork.Main.NetworkDispatcher.Dispatch(() =>
				{
					Internal_OnOpen();
				});
			}

			internal void Internal_OnOpen()
			{
				if (IsServer)
				{
					if (WebTransport.PeerList.Count >= WebTransport.TransportSettings.MaxConnections)
					{
						return;
					}

					WebTransportClient<PeerBehaviour> transportClient = new WebTransportClient<PeerBehaviour>(this, UserEndPoint);
					if (WebTransport.PeerList.TryAdd(transportClient.EndPoint, transportClient))
					{
						WebTransport.OnClientConnected?.Invoke(IsServer, transportClient.NetworkPeer);
					}
					else
					{
						OmniLogger.PrintError("The client is already connected!");
					}
				}
				else
				{
					WebTransport.Stopwatch.Start();
					WebTransport.IsConnected = true;
					WebTransport.OnClientConnected?.Invoke(IsServer, WebTransport.LocalTransportClient.NetworkPeer);
				}
			}

			protected override void OnMessage(MessageEventArgs e)
			{
				OmniNetwork.Main.NetworkDispatcher.Dispatch(() =>
				{
					Internal_OnMessage(e);
				});
			}

			internal void Internal_OnMessage(MessageEventArgs e)
			{
				byte[] data = e.RawData;
				if (e.IsBinary && !e.IsPing)
				{
					if (IsServer)
					{
						if (WebTransport.PeerList.TryGetValue(UserEndPoint, out WebTransportClient<PeerBehaviour> transportClient))
						{
							WebTransport.OnMessageReceived?.Invoke(IsServer, data, data.Length, transportClient.NetworkPeer);
						}
						else
						{
							OmniLogger.PrintError("Transport Receive Event: The client is not connected!");
						}
					}
					else
					{
						WebTransport.OnMessageReceived?.Invoke(IsServer, data, data.Length, WebTransport.LocalTransportClient.NetworkPeer);
					}
				}
				else
				{
					OmniLogger.PrintError($"Transport Receive Event: Invalid data received -> IsBinary:{e.IsBinary} | IsPing:{e.IsPing}");
				}
			}

			internal void Internal_Send(byte[] buffer, int offset, int count)
			{
				if (IsServer)
				{
					Send(buffer, offset, count);
				}
				else
				{
					WebClient.Send(buffer, offset, count);
				}
			}

			protected override void OnError(ErrorEventArgs e)
			{
				OmniNetwork.Main.NetworkDispatcher.Dispatch(() =>
				{
					Internal_OnError(e.Message);
				});
			}

			internal void Internal_OnError(string reason)
			{
				try
				{
					if (!IsDisconnected)
					{
						WebTransport.Disconnect(UserEndPoint, SocketError.SocketError, reason);
						IsDisconnected = true;
					}
				}
				catch (InvalidOperationException) { }
			}

			protected override void OnClose(CloseEventArgs e)
			{
				OmniNetwork.Main.NetworkDispatcher.Dispatch(() =>
				{
					Internal_OnClose(e.Reason);
				});
			}

			internal void Internal_OnClose(string message)
			{
				try
				{
					if (!IsDisconnected)
					{
						WebTransport.Disconnect(UserEndPoint, SocketError.Disconnecting, message);
						IsDisconnected = true;
					}
				}
				catch (InvalidOperationException) { }
			}

			internal async void Disconnect()
			{
				if (IsServer)
				{
					Close();
				}
				else
				{
					await WebClient.Close();
				}
			}
		}

		private Server.WebSocketServer m_Server;
		private Client.WebSocket m_Client;

		public Stopwatch Stopwatch { get; } = new();

		public CancellationTokenSource CancellationTokenSource { get; } = new();
		public Dictionary<EndPoint, WebTransportClient<PeerBehaviour>> PeerList { get; } = new();
		public WebTransportClient<PeerBehaviour> LocalTransportClient { get; private set; }

		private bool IsServer { get; set; }
		private TransportSettings TransportSettings { get; set; }
		public bool IsInitialized { get; private set; }
		public bool IsConnected { get; private set; }

		public ulong TotalMessagesSent { get; private set; }
		public ulong TotalMessagesReceived { get; private set; }
		public ulong TotalBytesSent { get; private set; }
		public ulong TotalBytesReceived { get; private set; }
		public ulong PacketLossPercent { get; private set; }

		public Dictionary<EndPoint, TcpTransportClient<Socket>> TcpPeerList => throw new NotImplementedException();
		public Dictionary<EndPoint, LiteTransportClient<NetPeer>> LitePeerList => throw new NotImplementedException();
		public Dictionary<EndPoint, WebTransportClient<PeerBehaviour>> WebPeerList => PeerList;

		public TcpTransportClient<Socket> TcpClient => throw new NotImplementedException();
		public LiteTransportClient<NetPeer> LiteClient => throw new NotImplementedException();
		public WebTransportClient<PeerBehaviour> WebClient => LocalTransportClient;

		public event Action<bool, NetworkPeer> OnClientConnected;
		public event Action<bool, NetworkPeer, SocketError, string> OnClientDisconnected;
		public event Action<bool, byte[], int, NetworkPeer> OnMessageReceived;

		public void InitializeTransport(bool isServer, EndPoint endPoint, TransportSettings settings)
		{
			this.IsServer = isServer;
			this.TransportSettings = settings;
			string host = settings.UseHttpsOnly ? $"wss://{settings.Host}:{settings.ServerPort}" : $"ws://{settings.Host}:{settings.ServerPort}";
			if (isServer)
			{
#if !UNITY_WEBGL || UNITY_EDITOR
				m_Server = new Server.WebSocketServer(host);
				m_Server.AddWebSocketService<PeerBehaviour>("/", (instance) =>
				{
					instance.WebTransport = this;
					instance.IsServer = isServer;
				});
				m_Server.Start();
				Stopwatch.Start();
				IsInitialized = true;
#endif
			}
			else
			{
				m_Client = new Client.WebSocket(host);
				LocalTransportClient = new WebTransportClient<PeerBehaviour>(new PeerBehaviour(this, m_Client, isServer), endPoint);
				IsInitialized = true;

				// Callbacks from WebSocketClient
				m_Client.OnOpen += () => LocalTransportClient.Peer.Internal_OnOpen();
				m_Client.OnError += (error) => LocalTransportClient.Peer.Internal_OnError(error);
				m_Client.OnClose += (reason) => LocalTransportClient.Peer.Internal_OnClose(reason.ToString());
				m_Client.OnMessage += (data) => LocalTransportClient.Peer.Internal_OnMessage(new MessageEventArgs(Opcode.Binary, data));
			}
		}

		public void Connect(EndPoint endPoint)
		{
			Internal_ConnectAsync().Wait(1000); // 1 Sec to connect!
		}

		public async void ConnectAsync(EndPoint endPoint)
		{
			if (!IsServer)
			{
				await Internal_ConnectAsync();
			}
		}

		private async Task Internal_ConnectAsync()
		{
			await m_Client.Connect();
		}

		public void Disconnect(EndPoint endPoint, SocketError errorCode, string reason)
		{
			if (IsServer)
			{
				if (PeerList.Remove(endPoint, out WebTransportClient<PeerBehaviour> transportClient))
				{
					OnClientDisconnected?.Invoke(IsServer, transportClient.NetworkPeer, errorCode, reason);
					PeerBehaviour peer = transportClient.Peer;
					peer.Disconnect();
				}
			}
			else
			{
				if (IsConnected)
				{
					IsConnected = false;
					OnClientDisconnected?.Invoke(IsServer, LocalTransportClient.NetworkPeer, errorCode, reason);
					PeerBehaviour peer = LocalTransportClient.Peer;
					peer.Disconnect();
				}
			}
		}

		public void Receive()
		{
			if (!IsServer)
			{
#if !UNITY_WEBGL || UNITY_EDITOR
				m_Client.DispatchMessageQueue();
#endif
			}
			else
			{
				throw new NotImplementedException("Web Transport does not implement this method!");
			}
		}

		public void Receive(Socket socket)
		{
			throw new NotImplementedException("Web Transport does not implement this method!");
		}

		private bool ValidateSend(byte[] data, int length)
		{
			if (data != null && length <= 0)
			{
				OmniLogger.PrintError("The size parameter cannot be zero.");
				return false;
			}

			if (length > TransportSettings.MaxMessageSize)
			{
				OmniLogger.PrintError("The size parameter cannot be greater than MaxMessageSize.");
				return false;
			}

			if (data.Length < length)
			{
				OmniLogger.PrintError("You are trying to send more data than is available in the buffer.");
				return false;
			}

			return true;
		}

		public void SendToClient(byte[] buffer, int size, EndPoint endPoint, DataDeliveryMode dataDeliveryMode, byte sequenceChannel)
		{
			if (IsServer)
			{
				if (ValidateSend(buffer, size))
				{
					if (PeerList.TryGetValue(endPoint, out WebTransportClient<PeerBehaviour> transportClient))
					{
						PeerBehaviour peer = transportClient.Peer;
						peer.Internal_Send(buffer, 0, size);
					}
					else
					{
						OmniLogger.PrintError("TCP Error: Failed to perform operation because the client is not connected to the server.");
					}
				}
			}
			else
			{
				OmniLogger.PrintError($"This transport is not valid for this operation! Use {nameof(SendToServer)}");
			}
		}

		public void SendToServer(byte[] buffer, int size, DataDeliveryMode dataDeliveryMode, byte sequenceChannel)
		{
			if (!IsServer)
			{
				if (ValidateSend(buffer, size))
				{
					if (IsConnected)
					{
						PeerBehaviour peer = LocalTransportClient.Peer;
						if (peer.WebClient.State == Client.WebSocketState.Open)
						{
							peer.Internal_Send(buffer, 0, size);
						}
						else
						{
							OmniLogger.PrintError($"This transport is not valid for this operation! because it is not connected.");
						}
					}
					else
					{
						OmniLogger.PrintError($"This transport is not valid for this operation! because it is not connected. (:");
					}
				}
			}
			else
			{
				OmniLogger.PrintError($"This transport is not valid for this operation! Use {nameof(SendToClient)}");
			}
		}

		public async void Close()
		{
			CancellationTokenSource.Dispose();
			foreach ((EndPoint peer, WebTransportClient<PeerBehaviour> transportClient) in PeerList.ToList())
			{
				transportClient.Peer.Disconnect();
			}

			m_Server?.Stop();

			try
			{
				await m_Client?.Close();
			}
			catch { }
		}
	}
}
