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
using LiteNetLib.Utils;
using Omni.Core;
using Omni.Internal.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp.Server;
using static Omni.Internal.Transport.WebTransport;

namespace Omni.Internal.Transport
{
	internal class LiteNetLibTransport : ITransport, ITransportClient<LiteTransportClient<NetPeer>>
	{
		private EventBasedNetListener listener = new EventBasedNetListener();
		private NetManager netManager;

		public CancellationTokenSource CancellationTokenSource { get; } = new();
		public Dictionary<EndPoint, LiteTransportClient<NetPeer>> PeerList { get; } = new();

		private TransportSettings TransportSettings { get; set; }
		private bool IsServer { get; set; }
		public LiteTransportClient<NetPeer> LocalTransportClient { get; private set; }

		public bool IsConnected { get; private set; }
		public bool IsInitialized { get; private set; }

		public ulong TotalMessagesSent => (ulong)netManager.Statistics.PacketsSent;
		public ulong TotalMessagesReceived => (ulong)netManager.Statistics.PacketsReceived;
		public ulong TotalBytesSent => (ulong)netManager.Statistics.BytesSent;
		public ulong TotalBytesReceived => (ulong)netManager.Statistics.BytesReceived;
		public ulong PacketLossPercent => 0;

		public Dictionary<EndPoint, TcpTransportClient<Socket>> TcpPeerList => throw new NotImplementedException();
		public Dictionary<EndPoint, LiteTransportClient<NetPeer>> LitePeerList => PeerList;
		public Dictionary<EndPoint, WebTransportClient<PeerBehaviour>> WebPeerList => throw new NotImplementedException();

		public TcpTransportClient<Socket> TcpClient => throw new NotImplementedException();
		public LiteTransportClient<NetPeer> LiteClient => LocalTransportClient;
		public WebTransportClient<PeerBehaviour> WebClient => throw new NotImplementedException();

		public Stopwatch Stopwatch { get; } = new();

		public event Action<bool, NetworkPeer> OnClientConnected;
		public event Action<bool, NetworkPeer> OnClientDisconnected;
		public event Action<bool, byte[], int, NetworkPeer> OnMessageReceived;

		public void InitializeTransport(bool isServer, EndPoint endPoint, TransportSettings transportSettings)
		{
			IPEndPoint iPEndPoint = (IPEndPoint)endPoint;
			netManager = new NetManager(listener);
			// Set Settings
			netManager.UnconnectedMessagesEnabled = true;
			netManager.AutoRecycle = true;
			netManager.UseNativeSockets = transportSettings.UseNativeSockets;
			netManager.BroadcastReceiveEnabled = transportSettings.BroadcastReceiveEnabled;
			netManager.DisconnectTimeout = transportSettings.DisconnectTimeout;
			netManager.EnableStatistics = true;
			netManager.IPv6Enabled = transportSettings.IPv6Enabled;
			netManager.MaxConnectAttempts = 10;
			netManager.MaxPacketsReceivePerUpdate = 0;
			netManager.NatPunchEnabled = transportSettings.NatPunchEnabled;
			netManager.PacketPoolSize = transportSettings.PacketPoolSize;
			netManager.PingInterval = transportSettings.PingInterval;
			netManager.ReconnectDelay = 500;
			netManager.ReuseAddress = false;
			netManager.UseSafeMtu = transportSettings.UseSafeMtu;
			TransportSettings = transportSettings;
			// End Settings
			netManager.Start(iPEndPoint.Address, IPAddress.IPv6Any, iPEndPoint.Port, false);
			// TTL
			netManager.Ttl = transportSettings.Ttl;
			// TTL
			IsServer = isServer;
			if (isServer)
			{
				Stopwatch.Start();
				listener.ConnectionRequestEvent += request =>
				{
					if (netManager.ConnectedPeersCount < transportSettings.MaxConnections)
					{
						request.AcceptIfKey("OmniNet");
					}
					else
					{
						request.Reject();
					}
				};

				listener.PeerConnectedEvent += peer =>
				{
					LiteTransportClient<NetPeer> transportClient = new LiteTransportClient<NetPeer>(peer, peer);
					if (PeerList.TryAdd(peer, transportClient))
					{
						OnClientConnected?.Invoke(isServer, transportClient.NetworkPeer);
					}
					else
					{
						OmniLogger.PrintError("The client is already connected!");
					}
				};
			}

			listener.NetworkReceiveEvent += (peer, dataReader, deliveryMethod, channel) =>
			{
				Receive(isServer, peer, dataReader);
			};

			listener.NetworkReceiveUnconnectedEvent += (remoteEndPoint, dataReader, messageType) =>
			{
				Receive(isServer, LocalTransportClient.Peer, dataReader);
			};

			listener.PeerDisconnectedEvent += (peer, info) =>
			{
				Disconnect(peer);
			};

			IsInitialized = true;
		}

		private void Receive(bool isServer, NetPeer peer, NetPacketReader dataReader)
		{
			byte[] remainingBytes = dataReader.GetRemainingBytes();
			if (isServer)
			{
				if (PeerList.TryGetValue(peer, out LiteTransportClient<NetPeer> transportClient))
				{
					OnMessageReceived?.Invoke(IsServer, remainingBytes, remainingBytes.Length, transportClient.NetworkPeer);
				}
				else
				{
					OmniLogger.PrintError("Transport Receive Event: The client is not connected!");
				}
			}
			else
			{
				OnMessageReceived?.Invoke(IsServer, remainingBytes, remainingBytes.Length, LocalTransportClient.NetworkPeer);
			}
		}

		private async void WaitForOutgoing(EndPoint endPoint, NetPeer peer)
		{
			while (!IsConnected)
			{
				if (peer.ConnectionState == ConnectionState.Connected)
				{
					Stopwatch.Start();
					IsConnected = true;
					OnClientConnected?.Invoke(IsServer, LocalTransportClient.NetworkPeer);
				}

				// wait to exit the outgoing state!
				await Task.Delay(250);
			}
		}

		public void Connect(EndPoint endPoint)
		{
			if (!IsServer)
			{
				NetPeer peer = netManager.Connect((IPEndPoint)endPoint, "OmniNet");
				LocalTransportClient = new LiteTransportClient<NetPeer>(peer, endPoint);
				WaitForOutgoing(endPoint, peer);
			}
		}

		public async void ConnectAsync(EndPoint endPoint)
		{
			if (!IsServer)
			{
				NetPeer peer = await Task.Run<NetPeer>(() =>
				{
					return netManager.Connect((IPEndPoint)endPoint, "OmniNet");
				});

				LocalTransportClient = new LiteTransportClient<NetPeer>(peer, endPoint);
				WaitForOutgoing(endPoint, peer);
			}
		}

		public void Disconnect(EndPoint endPoint)
		{
			if (IsServer)
			{
				if (PeerList.Remove(endPoint, out LiteTransportClient<NetPeer> transportClient))
				{
					NetPeer peer = transportClient.Peer;
					OnClientDisconnected?.Invoke(IsServer, transportClient.NetworkPeer);
					peer.Disconnect();
				}
			}
			else
			{
				NetPeer peer = LocalTransportClient.Peer;
				if (peer.ConnectionState == ConnectionState.Connected && IsConnected)
				{
					IsConnected = false;
					OnClientDisconnected?.Invoke(IsServer, LocalTransportClient.NetworkPeer);
					peer.Disconnect();
				}
			}
		}

		public void Receive()
		{
			netManager.PollEvents();
		}

		public void Receive(Socket socket)
		{
			throw new NotImplementedException();
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
					DeliveryMethod deliveryMethod = GetDeliveryMethod(dataDeliveryMode);
					if (PeerList.TryGetValue(endPoint, out LiteTransportClient<NetPeer> transportClient))
					{
						NetPeer peer = transportClient.Peer;
						peer.Send(buffer, 0, size, sequenceChannel, deliveryMethod);
					}
					else
					{
						OmniLogger.PrintError("Lite Error: Failed to perform operation because the client is not connected to the server.");
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
					DeliveryMethod deliveryMethod = GetDeliveryMethod(dataDeliveryMode);
					if (IsConnected)
					{
						NetPeer peer = LocalTransportClient.Peer;
						if (peer.ConnectionState == ConnectionState.Connected)
						{
							peer.Send(buffer, 0, size, sequenceChannel, deliveryMethod);
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

		private DeliveryMethod GetDeliveryMethod(DataDeliveryMode dataDeliveryMode)
		{
			return dataDeliveryMode switch
			{
				DataDeliveryMode.Unreliable => DeliveryMethod.Unreliable,
				DataDeliveryMode.ReliableOrdered => DeliveryMethod.ReliableOrdered,
				DataDeliveryMode.ReliableUnordered => DeliveryMethod.ReliableUnordered,
				DataDeliveryMode.ReliableSequenced => DeliveryMethod.ReliableSequenced,
				// Cryptography is at a higher layer.
				DataDeliveryMode.ReliableEncryptedOrdered => DeliveryMethod.ReliableOrdered,
				DataDeliveryMode.ReliableEncryptedUnordered => DeliveryMethod.ReliableUnordered,
				DataDeliveryMode.ReliableEncryptedSequenced => DeliveryMethod.ReliableSequenced,
			};
		}

		public void Close()
		{
			CancellationTokenSource.Dispose();
			foreach ((EndPoint peer, LiteTransportClient<NetPeer> transportClient) in PeerList)
			{
				transportClient.Peer.Disconnect();
			}
			netManager.Stop();
		}
	}
}