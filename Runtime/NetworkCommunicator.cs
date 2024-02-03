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
using UnityEngine;

namespace Omni.Core
{
	[DefaultExecutionOrder(-1000)]
	public class NetworkCommunicator : RealtimeTickBasedSystem
	{
		private int m_GameObjectId = int.MinValue;

		private Dictionary<int, NetworkPeer> PeersById { get; } = new();
		private ITransport ServerTransport => OmniNetwork.Omni.ServerTransport;
		private ITransport ClientTransport => OmniNetwork.Omni.ClientTransport;

		public static DataWriterPool DataWriterPool { get; private set; }
		public static DataReaderPool DataReaderPool { get; private set; }

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

		private void Awake()
		{
			DataWriterPool = new DataWriterPool(128);
			DataReaderPool = new DataReaderPool(128);
		}

		protected override void Start()
		{
			base.Start();
		}

		private void Update()
		{
			switch (OmniNetwork.Omni.TransportOption)
			{
				case TransportOption.TcpTransport:
					{
						if (IsClientInitialized())
						{

						}
					}
					break;
				case TransportOption.LiteNetTransport:
					{
						if (IsClientInitialized())
						{

						}
					}
					break;
				case TransportOption.WebSocketTransport:
					{
						if (IsClientInitialized())
						{

						}
					}
					break;
				default:
					throw new NotImplementedException("Communicator: Invalid transport!");
			}
		}

		public void SendCustomMessage<T>(T uniqueId, IDataWriter writer, DataDeliveryMode dataDeliveryMode, byte channel) where T : unmanaged, IComparable, IConvertible, IFormattable
		{
			IDataWriter internalWriter = DataWriterPool.Get();
			internalWriter.Write((byte)NetMessage.Message);
			internalWriter.Write7BitEncodedInt(NetworkHelper.GetInt32FromGenericEnum(uniqueId));
			internalWriter.Write(writer.Buffer, 0, writer.BytesWritten);
			SendToServer(internalWriter, dataDeliveryMode, channel);
			DataWriterPool.Release(internalWriter);
		}

		public void SendCustomMessage<T>(T uniqueId, IDataWriter writer, int playerId, DataDeliveryMode dataDeliveryMode, byte channel) where T : unmanaged, IComparable, IConvertible, IFormattable
		{
			IDataWriter internalWriter = DataWriterPool.Get();
			internalWriter.Write((byte)NetMessage.Message);
			internalWriter.Write7BitEncodedInt(NetworkHelper.GetInt32FromGenericEnum(uniqueId));
			internalWriter.Write(writer.Buffer, 0, writer.BytesWritten);
			SendToClient(internalWriter, playerId, dataDeliveryMode, channel);
			DataWriterPool.Release(internalWriter);
		}

		internal void Internal_SendCustomMessage<T>(T uniqueId, IDataWriter writer, DataDeliveryMode dataDeliveryMode, byte channel) where T : unmanaged, IComparable, IConvertible, IFormattable
		{
			IDataWriter internalWriter = DataWriterPool.Get();
			internalWriter.Write((byte)NetMessage.InternalMessage);
			internalWriter.Write7BitEncodedInt(NetworkHelper.GetInt32FromGenericEnum(uniqueId));
			internalWriter.Write(writer.Buffer, 0, writer.BytesWritten);
			SendToServer(internalWriter, dataDeliveryMode, channel);
			DataWriterPool.Release(internalWriter);
		}

		internal void Internal_SendCustomMessage<T>(T uniqueId, IDataWriter writer, int playerId, DataDeliveryMode dataDeliveryMode, byte channel) where T : unmanaged, IComparable, IConvertible, IFormattable
		{
			IDataWriter internalWriter = DataWriterPool.Get();
			internalWriter.Write((byte)NetMessage.InternalMessage);
			internalWriter.Write7BitEncodedInt(NetworkHelper.GetInt32FromGenericEnum(uniqueId));
			internalWriter.Write(writer.Buffer, 0, writer.BytesWritten);
			SendToClient(internalWriter, playerId, dataDeliveryMode, channel);
			DataWriterPool.Release(internalWriter);
		}

		private void OnClientConnected(bool isServer, NetworkPeer player)
		{
#if UNITY_EDITOR
			NetworkHelper.ThrowAnErrorIfConcurrent();
#endif
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
				if (OmniNetwork.Omni.TransportOption == TransportOption.WebSocketTransport)
				{
					OmniLogger.Print($"Client connected successfully. Host: {OmniNetwork.Omni.TransportSettings.Host}:{OmniNetwork.Omni.TransportSettings.ServerPort}");
					return;
				}

				OmniLogger.Print($"Client connected successfully. Endpoint: {player.EndPoint}");
			}
		}

		private void OnMessageReceived(bool isServer, byte[] data, int length, NetworkPeer player)
		{
#if UNITY_EDITOR
			NetworkHelper.ThrowAnErrorIfConcurrent();
#endif
			IDataReader reader = DataReaderPool.Get();
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
							IDataReader internalReader = DataReaderPool.Get();
							internalReader.Write(data, reader.Position, length);
							NetworkCallbacks.FireCustomMessage(isServer, internalReader, player);
							DataReaderPool.Release(internalReader);
						}
						break;
					case NetMessage.InternalMessage:
						{
							IDataReader internalReader = DataReaderPool.Get();
							internalReader.Write(data, reader.Position, length);
							NetworkCallbacks.Internal_FireCustomMessage(isServer, internalReader, player);
							DataReaderPool.Release(internalReader);
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
							IDataReader internalReader = DataReaderPool.Get();
							internalReader.Write(data, reader.Position, length);
							NetworkCallbacks.FireCustomMessage(isServer, internalReader, player);
							DataReaderPool.Release(internalReader);
						}
						break;
					case NetMessage.InternalMessage:
						{
							IDataReader internalReader = DataReaderPool.Get();
							internalReader.Write(data, reader.Position, length);
							NetworkCallbacks.Internal_FireCustomMessage(isServer, internalReader, player);
							DataReaderPool.Release(internalReader);
						}
						break;
				}
			}
			DataReaderPool.Release(reader);
		}

		private void OnClientDisconnected(bool isServer, NetworkPeer player)
		{
#if UNITY_EDITOR
			NetworkHelper.ThrowAnErrorIfConcurrent();
#endif
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

		internal void SendToClient(IDataWriter writer, int playerId, DataDeliveryMode dataDeliveryMode, byte channel)
		{
			if (PeersById.TryGetValue(playerId, out NetworkPeer peer))
			{
				SendToClient(writer, peer.EndPoint, dataDeliveryMode, channel);
			}
			else
			{
				OmniLogger.PrintError($"The player with the id: {playerId} is not connected to the server!");
			}
		}

		internal void SendToClient(IDataWriter writer, EndPoint endPoint, DataDeliveryMode dataDeliveryMode, byte channel)
		{
			ServerTransport.SendToClient(writer.Buffer, writer.BytesWritten, endPoint, dataDeliveryMode, channel);
		}

		internal void SendToServer(IDataWriter writer, DataDeliveryMode dataDeliveryMode, byte channel)
		{
			ClientTransport.SendToServer(writer.Buffer, writer.BytesWritten, dataDeliveryMode, channel);
		}

		internal int GetUniqueNetworkIdentityId()
		{
			return m_GameObjectId++;
		}

		private bool IsClientInitialized()
		{
			if (ClientTransport != null)
			{
				if (ClientTransport.IsInitialized && ClientTransport.IsConnected)
				{
					return true;
				}
			}
			return false;
		}

		private bool IsServerInitialized()
		{
			if (ServerTransport != null)
			{
				if (ServerTransport.IsInitialized)
				{
					return true;
				}
			}
			return false;
		}

		protected override void OnApplicationQuit()
		{
			base.OnApplicationQuit();
			//if (PeersById.Count > 0)
			//{
			//	PeersById.Clear();
			//}
		}
	}
}
