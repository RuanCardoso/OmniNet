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
using Omni.Internal.Transport;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Omni.Internal.Interfaces
{
	internal interface ITransport
	{
		event Action<bool, NetworkPlayer> OnClientConnected; // IsServer? // Player
		event Action<bool, NetworkPlayer> OnClientDisconnected; // IsServer? // Player
		event Action<bool, byte[], int, NetworkPlayer> OnMessageReceived; // IsServer? // Data // Size Data // Player

		Stopwatch Stopwatch { get; }
		CancellationTokenSource CancellationTokenSource { get; }
		bool IsInitialized { get; }
		bool IsConnected { get; }
		ulong TotalMessagesSent { get; }
		ulong TotalMessagesReceived { get; }
		ulong TotalBytesSent { get; }
		ulong TotalBytesReceived { get; }
		ulong PacketLossPercent { get; }
		void InitializeTransport(bool isServer, EndPoint endPoint, TransportSettings settings);
		void Connect(EndPoint endPoint);
		void ConnectAsync(EndPoint endPoint);
		void Disconnect(EndPoint endPoint);
		void Receive();
		void Receive(Socket socket);
		void SendToClient(byte[] buffer, int size, EndPoint endPoint);
		void SendToServer(byte[] buffer, int size);
		void Close();

		Dictionary<EndPoint, TcpTransportClient<Socket>> TcpPeerList { get; }
		Dictionary<EndPoint, LiteTransportClient<NetPeer>> LitePeerList { get; }

		TcpTransportClient<Socket> TcpClient { get; }
		LiteTransportClient<NetPeer> LiteClient { get; }
	}
}
