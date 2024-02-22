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

using LiteNetLib;
using Omni.Core;
using Omni.Internal.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static Omni.Internal.Transport.WebTransport;

#pragma warning disable

namespace Omni.Internal.Transport
{
	internal partial class OmniTcpTransport : ITransport, ITransportClient<TcpTransportClient<Socket>>
	{
		private const int ExpectedSize = 2;

		public event Action<bool, NetworkPeer> OnClientConnected;
		public event Action<bool, NetworkPeer> OnClientDisconnected;
		public event Action<bool, byte[], int, NetworkPeer> OnMessageReceived;

		public Dictionary<EndPoint, TcpTransportClient<Socket>> PeerList { get; } = new();
		public CancellationTokenSource CancellationTokenSource { get; } = new();

		private TransportSettings TransportSettings { get; set; }
		private Socket Socket { get; set; }
		private bool IsServer { get; set; }
		public TcpTransportClient<Socket> LocalTransportClient { get; private set; }

		public bool IsConnected { get; private set; }
		public bool IsInitialized { get; private set; }

		public ulong TotalMessagesSent { get; private set; }
		public ulong TotalMessagesReceived { get; private set; }
		public ulong TotalBytesSent { get; private set; }
		public ulong TotalBytesReceived { get; private set; }
		public ulong PacketLossPercent => 0;

		public Dictionary<EndPoint, TcpTransportClient<Socket>> TcpPeerList => PeerList;
		public Dictionary<EndPoint, LiteTransportClient<NetPeer>> LitePeerList => throw new NotImplementedException();
		public Dictionary<EndPoint, WebTransportClient<PeerBehaviour>> WebPeerList => throw new NotImplementedException();

		public Stopwatch Stopwatch { get; } = new();

		public TcpTransportClient<Socket> TcpClient => LocalTransportClient;
		public LiteTransportClient<NetPeer> LiteClient => throw new NotImplementedException();
		public WebTransportClient<PeerBehaviour> WebClient => throw new NotImplementedException();

		private Queue<Action> m_queues = new Queue<Action>();

		private void SetSettings(Socket socket, TransportSettings transportSettings)
		{
			socket.Blocking = true;
			socket.ExclusiveAddressUse = true;
			socket.Ttl = transportSettings.Ttl;
			socket.LingerState = new LingerOption(transportSettings.EnableLingerState, transportSettings.LingerStateTime);
			socket.ReceiveTimeout = transportSettings.ReceiveTimeout;
			socket.SendTimeout = transportSettings.SendTimeout;
			socket.ReceiveBufferSize = transportSettings.ReceiveBufferSize;
			socket.SendBufferSize = transportSettings.SendBufferSize;
			socket.NoDelay = transportSettings.NoDelay;
			TransportSettings = transportSettings;
		}

		public async void InitializeTransport(bool isServer, EndPoint endPoint, TransportSettings transportSettings)
		{
			try
			{
				Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				SetSettings(Socket, transportSettings);
				Socket.Bind(endPoint);

				IsServer = isServer;
				// listen(backlog) -> specifies the number of pending connections the queue will hold.
				if (isServer)
				{
					Socket.Listen(transportSettings.BackLog);
					Stopwatch.Start();
					IsInitialized = true;
					while (!CancellationTokenSource.IsCancellationRequested)
					{
						try
						{
							if (PeerList.Count >= transportSettings.MaxConnections)
							{
								await Task.Delay(1000);
								continue;
							}

							Socket socket = await Socket.AcceptAsync();
							EndPoint remoteEndPoint = socket.RemoteEndPoint;
							TcpTransportClient<Socket> transportClient = new TcpTransportClient<Socket>(new byte[transportSettings.MaxMessageSize], socket, remoteEndPoint);
							if (PeerList.TryAdd(remoteEndPoint, transportClient))
							{
								SetSettings(socket, transportSettings);
								OnClientConnected?.Invoke(isServer, transportClient.NetworkPeer);
							}
							else
							{
								OmniLogger.PrintError("The client is already connected!");
							}
						}
						catch (ObjectDisposedException) { continue; }
					}
				}
				else
				{
					StartLocalClient(transportSettings);
				}
			}
			catch (SocketException ex)
			{
				OmniLogger.PrintError(ex.Message);
			}
		}

		private void StartLocalClient(TransportSettings transportSettings)
		{
			IsInitialized = true;
			LocalTransportClient = new TcpTransportClient<Socket>(new byte[transportSettings.MaxMessageSize], Socket, Socket.LocalEndPoint);
		}

		public void Connect(EndPoint endPoint)
		{
			if (!IsServer)
			{
				Socket.Connect(endPoint);
				Stopwatch.Start();
				IsConnected = true;
				OnClientConnected?.Invoke(IsServer, LocalTransportClient.NetworkPeer);
			}
		}

		public async void ConnectAsync(EndPoint endPoint)
		{
			if (!IsServer)
			{
				await Socket.ConnectAsync(endPoint);
				Stopwatch.Start();
				IsConnected = true;
				OnClientConnected?.Invoke(IsServer, LocalTransportClient.NetworkPeer);
			}
		}

		public void Receive() // Called in update method!
		{
			if (IsServer)
			{
				// Perform 'operations' before 'receive operations' to avoid modifying the collection in the middle of iteration.
				while (m_queues.Count > 0)
				{
					Action exec = m_queues.Dequeue();
					exec();
				}

				foreach ((EndPoint peer, TcpTransportClient<Socket> transportClient) in PeerList)
				{
					ReadMessage(transportClient);
				}
			}
			else
			{
				ReadMessage(LocalTransportClient);
			}
		}

		public void Receive(Socket socket)
		{
			throw new NotImplementedException("Receive(Socket) not implemented!");
		}

		private void ReadMessage(TcpTransportClient<Socket> transportClient)
		{
			CancellationToken token = transportClient.CancellationTokenSource.Token;
			if (CancellationTokenSource.IsCancellationRequested || token.IsCancellationRequested)
			{
				return;
			}

			Socket socket = transportClient.Peer;
			EndPoint peer = transportClient.EndPoint;
			if (socket.Available >= transportClient.ExpectedLength)
			{
				transportClient.LastReceivedTime = DateTime.UtcNow;
				if (!transportClient.PendingMessage)
				{
					if (ReadExactly(transportClient.Buffer, transportClient.ExpectedLength, socket, token))
					{
						ushort length = (ushort)(transportClient.Buffer[0] | transportClient.Buffer[1] << 8);
						transportClient.SetExpectedLength(length, true);

						if (length > TransportSettings.MaxMessageSize)
						{
							m_queues.Enqueue(() =>
							{
								Disconnect(peer);
							});
							return;
						}

						// If the message is still available in the same iteration of header, we will process it.
						// This is optional, but it speeds up message processing, saving iterations.
						if (socket.Available >= length)
						{
							if (ReadExactly(transportClient.Buffer, length, socket, token))
							{
								TotalMessagesReceived++;
								TotalBytesReceived += (ulong)(length + ExpectedSize);
								OnMessageReceived?.Invoke(IsServer, transportClient.Buffer, length, transportClient.NetworkPeer);
								// 2 Bytes - ushort(65535) - 65kb(max receive)
								transportClient.SetExpectedLength(ExpectedSize, false); // - complete message
							}
							else
							{
								m_queues.Enqueue(() =>
								{
									Disconnect(peer);
								});
							}
						}
					}
					else
					{
						m_queues.Enqueue(() =>
						{
							Disconnect(peer);
						});
					}
				}
				else
				{
					if (ReadExactly(transportClient.Buffer, transportClient.ExpectedLength, socket, token))
					{
						TotalMessagesReceived++;
						TotalBytesReceived += (ulong)(transportClient.ExpectedLength + ExpectedSize);
						OnMessageReceived?.Invoke(IsServer, transportClient.Buffer, transportClient.ExpectedLength, transportClient.NetworkPeer);
						// 2 Bytes - ushort(65535) - 65kb(max receive)
						transportClient.SetExpectedLength(ExpectedSize, false); // - complete message

						// If the header is still available in the same iteration of message, we will process it.
						// This is optional, but it speeds up header processing, saving iterations.
						if (socket.Available >= ExpectedSize)
						{
							if (ReadExactly(transportClient.Buffer, ExpectedSize, socket, token))
							{
								ushort length = (ushort)(transportClient.Buffer[0] | transportClient.Buffer[1] << 8);
								transportClient.SetExpectedLength(length, true);

								if (length > TransportSettings.MaxMessageSize)
								{
									m_queues.Enqueue(() =>
									{
										Disconnect(peer);
									});
									return;
								}
							}
							else
							{
								m_queues.Enqueue(() =>
								{
									Disconnect(peer);
								});
							}
						}
					}
					else
					{
						m_queues.Enqueue(() =>
						{
							Disconnect(peer);
						});
					}
				}
			}
			else
			{
				TimeSpan poll = DateTime.UtcNow - transportClient.LastReceivedTime;
				if (poll.TotalSeconds > 3.5f)
				{
					m_queues.Enqueue(() =>
					{
						Disconnect(peer);
					});
				}
			}
		}

		/// <summary>
		/// Message Framing (https://blog.stephencleary.com/2009/04/message-framing.html)
		/// Read exactly the number of bytes specified by the size parameter.
		/// </summary>
		private bool ReadExactly(byte[] buffer, int length, Socket socket, CancellationToken token)
		{
			int offset = 0;
			while (offset < length)
			{
				if (CancellationTokenSource.IsCancellationRequested || token.IsCancellationRequested)
				{
					return false;
				}

				try
				{
					int len = socket.Receive(buffer, offset, length - offset, SocketFlags.None);
					offset += len;

					// The client has disconnected
					if (len <= 0)
					{
						return false;
					}
				}
				catch (IOException) { return false; }
				catch (ObjectDisposedException) { return false; }
				catch (SocketException) { return false; }
				catch (Exception) { return false; }
			}
			return offset == length;
		}

		public void SendToClient(byte[] buffer, int length, EndPoint endPoint, DataDeliveryMode dataDeliveryMode, byte channel)
		{
			if (IsServer)
			{
				if (PeerList.TryGetValue(endPoint, out TcpTransportClient<Socket> transportClient))
				{
					Send(buffer, length, transportClient.Peer);
				}
				else
				{
					OmniLogger.PrintError("TCP Error: Failed to perform operation because the client is not connected to the server.");
				}
			}
			else
			{
				OmniLogger.PrintError($"This transport is not valid for this operation! Use {nameof(SendToServer)}");
			}
		}

		public void SendToServer(byte[] buffer, int length, DataDeliveryMode dataDeliveryMode, byte channel)
		{
			if (!IsServer)
			{
				if (IsConnected)
				{
					Send(buffer, length, Socket);
				}
				else
				{
					OmniLogger.PrintError($"This transport is not valid for this operation! because it is not connected.");
				}
			}
			else
			{
				OmniLogger.PrintError($"This transport is not valid for this operation! Use {nameof(SendToClient)}");
			}
		}

		/// <summary>
		/// Message Framing (https://blog.stephencleary.com/2009/04/message-framing.html)
		/// Send exactly the number of bytes with the message.
		/// </summary>
		private void Send(byte[] data, int length, Socket socket)
		{
			if (data != null && length <= 0)
			{
				OmniLogger.PrintError("The size parameter cannot be zero.");
				return;
			}

			if (length > TransportSettings.MaxMessageSize)
			{
				OmniLogger.PrintError("The size parameter cannot be greater than MaxMessageSize.");
				return;
			}

			if (data.Length < length)
			{
				OmniLogger.PrintError("You are trying to send more data than is available in the buffer.");
				return;
			}

			// prefixes the message size(ushort) to the message itself.
			byte[] prefixedData = new byte[length + ExpectedSize];
			prefixedData[0] = (byte)length;
			prefixedData[1] = (byte)(length >> 8);

			// Copies the data after the prefix.
			for (int i = 0; i < length; i++)
			{
				prefixedData[i + ExpectedSize] = data[i];
			}

			// Now let's send the new data with the prefix.
			data = prefixedData;
			length = prefixedData.Length;

			// Send....
			int offset = 0;
			while (offset < length)
			{
				if (CancellationTokenSource.IsCancellationRequested)
				{
					return;
				}

				try
				{
					int len = socket.Send(data, offset, length - offset, SocketFlags.None);
					offset += len;

					if (len <= 0)
					{
						return;
					}
				}
				catch
				{
					return;
				}
			}

			if (offset == length)
			{
				TotalMessagesSent++;
				TotalBytesSent += (ulong)length;
			}
		}

		public void Disconnect(EndPoint endPoint)
		{
			if (IsServer)
			{
				if (PeerList.Remove(endPoint, out TcpTransportClient<Socket> transportClient))
				{
					OnClientDisconnected?.Invoke(IsServer, transportClient.NetworkPeer);
					DisconnectRemotePeer(transportClient);
				}
			}
			else
			{
				if (IsConnected)
				{
					IsConnected = false;
					OnClientDisconnected?.Invoke(IsServer, LocalTransportClient.NetworkPeer);
					DisconnectLocalPeer();
				}
			}
		}

		private void DisconnectLocalPeer()
		{
			try
			{
				LocalTransportClient?.CancellationTokenSource?.Cancel();
				Socket?.Shutdown(SocketShutdown.Both);
				Socket?.Disconnect(false);
				Socket?.Close();
			}
			catch { }
			finally
			{
				LocalTransportClient?.CancellationTokenSource?.Dispose();
			}
		}

		private void DisconnectRemotePeer(TcpTransportClient<Socket> transportClient)
		{
			try
			{
				transportClient?.CancellationTokenSource?.Cancel();
				transportClient?.Peer?.Shutdown(SocketShutdown.Both);
				transportClient?.Peer?.Disconnect(false);
				transportClient?.Peer?.Close();
			}
			catch { }
			finally
			{
				transportClient?.CancellationTokenSource?.Dispose();
			}
		}

		public void Close()
		{
			try
			{
				CancellationTokenSource.Cancel();
				DisconnectLocalPeer();

				foreach ((EndPoint peer, TcpTransportClient<Socket> transportClient) in PeerList)
				{
					DisconnectRemotePeer(transportClient);
				}
			}
			finally
			{
				CancellationTokenSource.Dispose();
				Socket.Dispose();
			}
		}
	}
}