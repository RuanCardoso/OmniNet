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
using Omni.Internal.Interfaces;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using UnityEngine;

namespace Omni.Core
{
	[DefaultExecutionOrder(-1000)]
	public class NetworkCommunicator : RealtimeTickBasedSystem
	{
		private Dictionary<int, NetworkPlayer> PeersById { get; } = new();
		private ITransport ServerTransport => OmniNetwork.Omni.ServerTransport;
		private ITransport ClientTransport => OmniNetwork.Omni.ClientTransport;

		internal void ProcessEvents()
		{
			if (ClientTransport != null)
			{
				ClientTransport.OnClientConnected += OnClientConnected;
				ClientTransport.OnMessageReceived += OnMessageReceived;
				ClientTransport.OnClientDisconnected += OnClientDisconnected;
			}

			if (ServerTransport != null)
			{
				ServerTransport.OnClientConnected += OnClientConnected;
				ServerTransport.OnMessageReceived += OnMessageReceived;
				ServerTransport.OnClientDisconnected += OnClientDisconnected;
			}
		}

		public override void Start()
		{
			base.Start();
		}

		private void Update()
		{
			switch (OmniNetwork.Omni.TransportOption)
			{
				case TransportOption.TcpTransport:
					{
						if (ClientTransport != null)
						{
							if (ClientTransport.IsInitialized && ClientTransport.IsConnected)
							{

							}
						}
					}
					break;
				case TransportOption.LiteNetTransport:
					{
						if (ClientTransport != null)
						{

						}
					}
					break;
				default:
					throw new NotImplementedException("Communicator: Invalid transport!");
			}
		}

		public void SendCustomMessage(IDataWriter writer)
		{
			IDataWriter internalWriter = new DataWriter(100);
			internalWriter.Write((byte)NetMessage.Message);
			internalWriter.Write(writer.Buffer, 0, writer.BytesWritten);
			SendToServer(internalWriter);
		}

		public void SendCustomMessage(IDataWriter writer, int playerId)
		{
			IDataWriter internalWriter = new DataWriter(100);
			internalWriter.Write((byte)NetMessage.Message);
			internalWriter.Write(writer.Buffer, 0, writer.BytesWritten);
			SendToClient(internalWriter, playerId);
		}

		private void OnClientConnected(bool isServer, NetworkPlayer player)
		{
			if (isServer)
			{
				if (PeersById.TryAdd(player.Id, player))
				{
					OmniLogger.Print($"Server connected successfully. Endpoint: {player.EndPoint}");
				}
				else
				{
					OmniLogger.PrintError($"Connection failed. Endpoint: {player.EndPoint} is already connected!");
				}
			}
			else
			{
				OmniLogger.Print($"Client connected successfully. Endpoint: {player.EndPoint}");
			}
		}

		private void OnMessageReceived(bool isServer, byte[] data, int length, NetworkPlayer player)
		{
#if UNITY_EDITOR
			if (Thread.CurrentThread.ManagedThreadId != OmniNetwork.Omni.ManagedThreadId)
			{
				OmniLogger.PrintError("Unity does not support operations outside the main thread.");
				return;
			}
#endif
			IDataReader reader = new DataReader(100);
			reader.Write(data, 0, length);
			byte message = reader.ReadByte();
			// Process the messages to both(client and server)
			if (isServer)
			{
				switch ((NetMessage)message)
				{
					case NetMessage.Ping:
						{

						}
						break;
					case NetMessage.Message:
						{
							IDataReader internalReader = new DataReader(100);
							internalReader.Write(data, reader.Position, length);
							NetworkCallbacks.FireCustomMessage(isServer, internalReader, player);
						}
						break;
				}
			}
			else
			{
				switch ((NetMessage)message)
				{
					case NetMessage.Ping:
						{

						}
						break;
					case NetMessage.Message:
						{
							IDataReader internalReader = new DataReader(100);
							internalReader.Write(data, reader.Position, length);
							NetworkCallbacks.FireCustomMessage(isServer, internalReader, player);
						}
						break;
				}
			}
		}

		private void OnClientDisconnected(bool isServer, NetworkPlayer player)
		{
			if (isServer)
			{
				if (PeersById.Remove(player.Id))
				{
					OmniLogger.Print($"Server disconnected. Endpoint: {player.EndPoint}");
				}
				else
				{
					OmniLogger.PrintError($"Disconnection failed. Endpoint: {player.EndPoint} is not connected!");
				}
			}
			else
			{
				OmniLogger.Print($"Client disconnected. Endpoint: {player.EndPoint}");
			}
		}

		internal void SendToClient(IDataWriter writer, int playerId)
		{
			if (PeersById.TryGetValue(playerId, out NetworkPlayer peer))
			{
				SendToClient(writer, peer.EndPoint);
			}
			else
			{
				OmniLogger.PrintError($"The player with the id: {playerId} is not connected to the server!");
			}
		}

		internal void SendToClient(IDataWriter writer, EndPoint endPoint)
		{
			ServerTransport.SendToClient(writer.Buffer, writer.BytesWritten, endPoint);
		}

		internal void SendToServer(IDataWriter writer)
		{
			ClientTransport.SendToServer(writer.Buffer, writer.BytesWritten);
		}
	}
}
