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

using Omni.Core.Cryptography;
using Omni.Internal;
using Omni.Internal.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using UnityEngine;
using static Omni.Core.OmniNetwork;

namespace Omni.Core
{
	[DefaultExecutionOrder(-1000)]
	public class NetworkCommunicator : RealtimeTickBasedSystem
	{
		enum LargeDataOption
		{
			PublicKeyExchange = 4,
			AesKeyExchange = 6,
			NetworkPeerSync = 8
		}

		private Dictionary<int, NetworkPeer> PeersById { get; } = new();
		private ITransport ServerTransport => Main.ServerTransport;
		private ITransport ClientTransport => Main.ClientTransport;

		internal Dictionary<ValueTuple<int, bool>, NetworkIdentity> NetworkIdentities { get; } = new Dictionary<ValueTuple<int, bool>, NetworkIdentity>(); // (Network Identity Id, Is Server), Network Identity Instance

		public static DataWriterPool DataWriterPool { get; private set; }
		public static DataReaderPool DataReaderPool { get; private set; }

		public NetworkPeer LocalPeer
		{
			get
			{
				if (ClientTransport == null)
				{
					throw new NotSupportedException("Server does not have a client.");
				}

				return Main.TransportOption switch
				{
					TransportOption.TcpTransport => ClientTransport.TcpClient.NetworkPeer,
					TransportOption.LiteNetTransport => ClientTransport.LiteClient.NetworkPeer,
					TransportOption.WebSocketTransport => ClientTransport.WebClient.NetworkPeer,
					_ => throw new NotImplementedException("Invalid transport!")
				};
			}
		}

		internal void ProcessEvents()
		{
			if (ClientTransport != null)
			{
				ClientTransport.OnClientConnected += OnClientConnected;
				ClientTransport.OnMessageReceived += OnMessageReceived; // Exclusive
				ClientTransport.OnClientDisconnected += OnClientDisconnected;
			}

			if (ServerTransport != null)
			{
				ServerTransport.OnClientConnected += OnClientConnected;
				ServerTransport.OnMessageReceived += OnMessageReceived; // Exclusive
				ServerTransport.OnClientDisconnected += OnClientDisconnected;
			}
		}

		private void Awake()
		{
			DataWriterPool = new DataWriterPool(428);
			DataReaderPool = new DataReaderPool(428);
			NetworkCallbacks.Internal_OnLargeDataReceived += OnLargeDataReceived;
		}

		private void OnLargeDataReceived(bool isServer, int option, IDataReader reader, NetworkPeer peer, DataDeliveryMode deliveryMode)
		{
			LargeDataOption largeDataOption = option.ReadCustomMessage<LargeDataOption>();
			if (!isServer)
			{
				switch (largeDataOption)
				{
					// authentication
					case LargeDataOption.PublicKeyExchange:
						{
							try
							{
								// Send the Aes Key to the Server
								string publicKey = StringCipher.Decrypt(reader.ReadString(), Main.Guid);
								// Exchange Aes Keys, the client will send its aes key to the server.
								IDataWriter aesWriter = DataWriterPool.Get();
								byte[] key = RsaCryptography.Encrypt(Main.AesKey, publicKey);
								aesWriter.Write7BitEncodedInt(key.Length);
								aesWriter.Write(key);
								Internal_SendLargeBlocksOfData(LargeDataOption.AesKeyExchange, aesWriter, DataDeliveryMode.ReliableOrdered, 0, 64, 0); // 64 // Min data size can be sended
								DataWriterPool.Release(aesWriter);
								// Authenticated
								NetworkCallbacks.FireClientConnected(isServer, peer);
							}
							catch (Exception ex)
							{
								OmniLogger.LogStacktrace(ex);
								// Sessions take time to initialize in the Editor after Play, we will wait 1 second to ensure the session is ready before a sudden disconnection.
								Invoke(nameof(Disconnect), 1f);
							}
						}
						break;
					case LargeDataOption.NetworkPeerSync:
						break;
					default:
						throw new NotImplementedException("LargeDataOption not implemented!");
				}
			}
			else
			{
				if (largeDataOption == LargeDataOption.AesKeyExchange)
				{
					try
					{
						int length = reader.Read7BitEncodedInt();
						byte[] encryptedKey = new byte[length];
						reader.Read(encryptedKey, 0, length); // 128 bits -> 16 Bytes
						peer.AesKey = RsaCryptography.Decrypt(encryptedKey, Main.PrivateKey);
						// Authenticated
						NetworkCallbacks.FireClientConnected(isServer, peer);
					}
					catch (Exception ex)
					{
						OmniLogger.LogStacktrace(ex);
						DisconnectPeer(peer.EndPoint);
					}
				}
			}
		}

		public override void Start()
		{
			base.Start();
		}

		private void Update()
		{
			#region Unused
			switch (OmniNetwork.Main.TransportOption)
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
			#endregion
		}

		public void SendCustomMessage<T>(T uniqueId, IDataWriter writer, DataDeliveryMode dataDeliveryMode, DataTarget dataTarget, int peerId, byte sequenceChannel = 0) where T : unmanaged, IComparable, IConvertible, IFormattable
		{
			switch (dataTarget)
			{
				case DataTarget.Self:
					SendCustomMessage(uniqueId, writer, peerId, dataDeliveryMode, sequenceChannel);
					break;
				case DataTarget.Broadcast:
					{
						foreach ((int _, NetworkPeer peer) in GetPeers())
						{
							SendCustomMessage(uniqueId, writer, peer.Id, dataDeliveryMode, sequenceChannel);
						}
					}
					break;
				case DataTarget.BroadcastExcludingSelf:
					{
						foreach ((int _, NetworkPeer peer) in GetPeers())
						{
							if (peer.Id == peerId)
								continue;

							SendCustomMessage(uniqueId, writer, peer.Id, dataDeliveryMode, sequenceChannel);
						}
					}
					break;
				case DataTarget.Server:
					SendCustomMessage(uniqueId, writer, dataDeliveryMode, sequenceChannel);
					break;
			}
		}

		public void SendCustomMessage<T>(T uniqueId, IDataWriter writer, DataDeliveryMode dataDeliveryMode, byte sequenceChannel = 0) where T : unmanaged, IComparable, IConvertible, IFormattable
		{
			SendCustomMessage(uniqueId, NetMessage.Message, writer, 0, dataDeliveryMode, sequenceChannel);
		}

		public void SendCustomMessage<T>(T uniqueId, IDataWriter writer, int peerId, DataDeliveryMode dataDeliveryMode, byte sequenceChannel = 0) where T : unmanaged, IComparable, IConvertible, IFormattable
		{
			SendCustomMessage(uniqueId, NetMessage.Message, writer, peerId, dataDeliveryMode, sequenceChannel);
		}

		internal void Internal_SendCustomMessage<T>(T uniqueId, IDataWriter writer, DataDeliveryMode dataDeliveryMode, byte sequenceChannel = 0) where T : unmanaged, IComparable, IConvertible, IFormattable
		{
			SendCustomMessage(uniqueId, NetMessage.InternalMessage, writer, 0, dataDeliveryMode, sequenceChannel);
		}

		internal void Internal_SendCustomMessage<T>(T uniqueId, IDataWriter writer, int peerId, DataDeliveryMode dataDeliveryMode, byte sequenceChannel = 0) where T : unmanaged, IComparable, IConvertible, IFormattable
		{
			SendCustomMessage(uniqueId, NetMessage.InternalMessage, writer, peerId, dataDeliveryMode, sequenceChannel);
		}

		private void SendCustomMessage<T>(T uniqueId, NetMessage msgType, IDataWriter writer, int peerId, DataDeliveryMode dataDeliveryMode, byte sequenceChannel = 0) where T : unmanaged, IComparable, IConvertible, IFormattable
		{
			IDataWriter internalWriter = DataWriterPool.Get();
			internalWriter.Write((byte)msgType);
			internalWriter.Write7BitEncodedInt(NetworkHelper.GetInt32FromGenericEnum(uniqueId));
			internalWriter.Write(writer.Buffer, 0, writer.BytesWritten);
			if (peerId != 0) SendToClient(internalWriter, peerId, dataDeliveryMode, sequenceChannel);
			else SendToServer(internalWriter, dataDeliveryMode, sequenceChannel);
			DataWriterPool.Release(internalWriter);
		}

		public void Rpc(IDataWriter writer, DataDeliveryMode dataDeliveryMode, int identityId, byte networkBehaviourId, byte rpcId, byte sequenceChannel = 0)
		{
			InternalRpc(writer, dataDeliveryMode, 0, identityId, networkBehaviourId, rpcId, sequenceChannel);
		}

		public void Rpc(IDataWriter writer, DataDeliveryMode dataDeliveryMode, int peerId, int identityId, byte networkBehaviourId, byte rpcId, byte sequenceChannel = 0)
		{
			InternalRpc(writer, dataDeliveryMode, peerId, identityId, networkBehaviourId, rpcId, sequenceChannel);
		}

		private void InternalRpc(IDataWriter writer, DataDeliveryMode dataDeliveryMode, int peerId, int identityId, byte networkBehaviourId, byte rpcId, byte sequenceChannel = 0)
		{
			IDataWriter internalWriter = DataWriterPool.Get();
			internalWriter.Write((byte)NetMessage.Rpc);
			internalWriter.Write7BitEncodedInt(identityId);
			internalWriter.Write(networkBehaviourId);
			internalWriter.Write(rpcId);
			internalWriter.Write(writer.Buffer, 0, writer.BytesWritten);
			if (peerId != 0) SendToClient(internalWriter, peerId, dataDeliveryMode, sequenceChannel);
			else SendToServer(internalWriter, dataDeliveryMode, sequenceChannel);
			DataWriterPool.Release(internalWriter);
		}

		public void SyncVariable(IDataWriter writer, DataDeliveryMode dataDeliveryMode, int identityId, byte networkBehaviourId, byte netVarId, byte sequenceChannel = 0)
		{
			InternalSyncVariable(writer, dataDeliveryMode, 0, identityId, networkBehaviourId, netVarId, sequenceChannel);
		}

		public void SyncVariable(IDataWriter writer, DataDeliveryMode dataDeliveryMode, int peerId, int identityId, byte networkBehaviourId, byte netVarId, byte sequenceChannel = 0)
		{
			InternalSyncVariable(writer, dataDeliveryMode, peerId, identityId, networkBehaviourId, netVarId, sequenceChannel);
		}

		private void InternalSyncVariable(IDataWriter writer, DataDeliveryMode dataDeliveryMode, int peerId, int identityId, byte networkBehaviourId, byte netVarId, byte sequenceChannel = 0)
		{
			IDataWriter internalWriter = DataWriterPool.Get();
			internalWriter.Write((byte)NetMessage.NetVar);
			internalWriter.Write7BitEncodedInt(identityId);
			internalWriter.Write(networkBehaviourId);
			internalWriter.Write(netVarId);
			internalWriter.Write(writer.Buffer, 0, writer.BytesWritten);
			if (peerId != 0) SendToClient(internalWriter, peerId, dataDeliveryMode, sequenceChannel);
			else SendToServer(internalWriter, dataDeliveryMode, sequenceChannel);
			DataWriterPool.Release(internalWriter);
		}

		public void SerializeView(IDataWriter writer, DataDeliveryMode dataDeliveryMode, int identityId, byte networkBehaviourId, byte sequenceChannel = 0)
		{
			InternalSerializeView(writer, dataDeliveryMode, 0, identityId, networkBehaviourId, sequenceChannel);
		}

		public void SerializeView(IDataWriter writer, DataDeliveryMode dataDeliveryMode, int peerId, int identityId, byte networkBehaviourId, byte sequenceChannel = 0)
		{
			InternalSerializeView(writer, dataDeliveryMode, peerId, identityId, networkBehaviourId, sequenceChannel);
		}

		private void InternalSerializeView(IDataWriter writer, DataDeliveryMode dataDeliveryMode, int peerId, int identityId, byte networkBehaviourId, byte sequenceChannel = 0)
		{
			IDataWriter internalWriter = DataWriterPool.Get();
			internalWriter.Write((byte)NetMessage.SerializeView);
			internalWriter.Write7BitEncodedInt(identityId);
			internalWriter.Write(networkBehaviourId);
			internalWriter.Write(writer.Buffer, 0, writer.BytesWritten);
			if (peerId != 0) SendToClient(internalWriter, peerId, dataDeliveryMode, sequenceChannel);
			else SendToServer(internalWriter, dataDeliveryMode, sequenceChannel);
			DataWriterPool.Release(internalWriter);
		}

		public void SendLargeBlocksOfData<T>(T uniqueId, IDataWriter data, DataDeliveryMode dataDeliveryMode, DataTarget dataTarget, int peerId, int blockSize = 64, byte sequenceChannel = 0) where T : unmanaged, IComparable, IConvertible, IFormattable
		{
			switch (dataTarget)
			{
				case DataTarget.Self:
					SendLargeBlocksOfData(uniqueId, data, dataDeliveryMode, peerId, blockSize, sequenceChannel);
					break;
				case DataTarget.Broadcast:
					{
						foreach ((int _, NetworkPeer peer) in GetPeers())
						{
							SendLargeBlocksOfData(uniqueId, data, dataDeliveryMode, peer.Id, blockSize, sequenceChannel);
						}
					}
					break;
				case DataTarget.BroadcastExcludingSelf:
					{
						foreach ((int _, NetworkPeer peer) in GetPeers())
						{
							if (peer.Id == peerId)
								continue;

							SendLargeBlocksOfData(uniqueId, data, dataDeliveryMode, peer.Id, blockSize, sequenceChannel);
						}
					}
					break;
				case DataTarget.Server:
					SendLargeBlocksOfData(uniqueId, data, dataDeliveryMode, blockSize, sequenceChannel);
					break;
			}
		}

		public void SendLargeBlocksOfData<T>(T uniqueId, IDataWriter data, DataDeliveryMode dataDeliveryMode, int blockSize = 64, byte sequenceChannel = 0) where T : unmanaged, IComparable, IConvertible, IFormattable
		{
			SendLargeBlocksOfData(uniqueId, NetMessage.LargeBlockOfBytes, data, dataDeliveryMode, 0, blockSize, sequenceChannel);
		}

		public void SendLargeBlocksOfData<T>(T uniqueId, IDataWriter data, DataDeliveryMode dataDeliveryMode, int peerId, int blockSize = 64, byte sequenceChannel = 0) where T : unmanaged, IComparable, IConvertible, IFormattable
		{
			SendLargeBlocksOfData(uniqueId, NetMessage.LargeBlockOfBytes, data, dataDeliveryMode, peerId, blockSize, sequenceChannel);
		}

		internal void Internal_SendLargeBlocksOfData<T>(T uniqueId, IDataWriter data, DataDeliveryMode dataDeliveryMode, int blockSize = 64, byte sequenceChannel = 0) where T : unmanaged, IComparable, IConvertible, IFormattable
		{
			SendLargeBlocksOfData(uniqueId, NetMessage.InternalLargeBlockOfBytes, data, dataDeliveryMode, 0, blockSize, sequenceChannel);
		}

		internal void Internal_SendLargeBlocksOfData<T>(T uniqueId, IDataWriter data, DataDeliveryMode dataDeliveryMode, int peerId, int blockSize = 64, byte sequenceChannel = 0) where T : unmanaged, IComparable, IConvertible, IFormattable
		{
			SendLargeBlocksOfData(uniqueId, NetMessage.InternalLargeBlockOfBytes, data, dataDeliveryMode, peerId, blockSize, sequenceChannel);
		}

		private void SendLargeBlocksOfData<T>(T uniqueId, NetMessage msgType, IDataWriter data, DataDeliveryMode dataDeliveryMode, int peerId, int blockSize = 64, byte sequenceChannel = 0) where T : unmanaged, IComparable, IConvertible, IFormattable
		{
			if (dataDeliveryMode == DataDeliveryMode.Unreliable)
			{
				throw new NotSupportedException("Error: Unsecured data delivery is not supported. Reliable transmission is required for data integrity and security.");
			}

			int x = blockSize;
			if (!((x != 0) && ((x & (x - 1)) == 0)))
			{
				throw new Exception("Block size must be a power of 2.");
			}

			if (blockSize > Main.TransportSettings.MaxMessageSize)
			{
				throw new Exception("Block size exceeds the maximum allowed message size.");
			}

			if (data.BytesWritten > 524288000) // 500 MB Max
			{
				throw new Exception("The file size exceeds the maximum allowed limit of 500 MB.");
			}

			int dataSize = data.BytesWritten;
			if (dataSize > blockSize)
			{
				int offset = 0;
				int messages = 0;
				while (offset < dataSize)
				{
					messages++;
					// Determine the starting index of the data block to be sent.
					int from = offset;
					// Calculate the length of the data block to be sent.
					int length = offset + blockSize;
					// Adjust the offset for the next block, ensuring it does not exceed the total size of the data.
					offset += length > dataSize ? dataSize - offset : blockSize;

					// Prepare the data block to be sent.
					// Create a read-only view (ReadOnlySpan) of the data block to be sent.
					ReadOnlySpan<byte> blockSpan = new(data.Buffer, from, offset - from);
					// Acquire a data writer (IDataWriter) from the writer pool.
					IDataWriter blockWriter = DataWriterPool.Get();
					// Write the message type (NetMessage.LargeBlockOfBytes), the size of the data, and the data itself into the writer.
					blockWriter.Write((byte)msgType);
					blockWriter.Write7BitEncodedInt(dataSize);
					blockWriter.Write(NetworkHelper.GetInt32FromGenericEnum(uniqueId));
					blockWriter.Write(blockSpan);
					if (peerId != 0) SendToClient(blockWriter, peerId, dataDeliveryMode, sequenceChannel);
					else SendToServer(blockWriter, dataDeliveryMode, sequenceChannel);
					DataWriterPool.Release(blockWriter);
				}
			}
			else
			{
				IDataWriter blockWriter = DataWriterPool.Get();
				blockWriter.Write((byte)msgType);
				blockWriter.Write7BitEncodedInt(dataSize);
				blockWriter.Write(NetworkHelper.GetInt32FromGenericEnum(uniqueId));
				blockWriter.Write(data.Buffer, 0, data.BytesWritten);
				if (peerId != 0) SendToClient(blockWriter, peerId, dataDeliveryMode, sequenceChannel);
				else SendToServer(blockWriter, dataDeliveryMode, sequenceChannel);
				DataWriterPool.Release(blockWriter);
			}
		}

		private IEnumerator Ping()
		{
			while (true)
			{
				IDataWriter pingRequest = DataWriterPool.Get();
				pingRequest.Write((byte)NetMessage.Ping);
				SendToServer(pingRequest, DataDeliveryMode.Unreliable, 0); // Unreliable, not important!
				DataWriterPool.Release(pingRequest);
				yield return new WaitForSeconds(1);
			}
		}

		public Dictionary<int, NetworkPeer> GetPeers()
		{
			return PeersById;
		}

		public NetworkPeer GetPeerById(int id)
		{
			return PeersById[id];
		}

		public bool IsMine(int peerId)
		{
			return LocalPeer.Id == peerId;
		}

		public IDataWriter GetWriter() => DataWriterPool.Get();
		public IDataReader GetReader() => DataReaderPool.Get();

		public void Release(IDataWriter writer) => DataWriterPool.Release(writer);
		public void Release(IDataReader reader) => DataReaderPool.Release(reader);

		private void OnClientConnected(bool isServer, NetworkPeer peer)
		{
#if UNITY_EDITOR
			NetworkHelper.ThrowAnErrorIfConcurrent();
#endif
			if (isServer)
			{
				if (PeersById.TryAdd(peer.Id, peer))
				{
					// Exchange Rsa Keys, the server will send its public key to the client.
					IDataWriter rsaWriter = DataWriterPool.Get();
					rsaWriter.Write(Main.PublicKey);
					Internal_SendLargeBlocksOfData(LargeDataOption.PublicKeyExchange, rsaWriter, DataDeliveryMode.ReliableOrdered, peer.Id, 64, 0); // 64 // Min data size can be sended
					DataWriterPool.Release(rsaWriter);

					OmniLogger.Print($"Player successfully connected to the server. Endpoint: {peer.EndPoint} -> Id: {peer.Id}");
				}
				else
				{
					OmniLogger.PrintError($"Connection failed. Endpoint: {peer.EndPoint} is already connected! -> Id: {peer.Id}");
				}
			}
			else
			{
				switch (Main.TransportOption)
				{
					case TransportOption.TcpTransport:
						StartCoroutine(Ping()); // Tcp Keep Alives
						break;
					case TransportOption.WebSocketTransport:
						OmniLogger.Print($"Client successfully connected! Endpoint: {Main.TransportSettings.Host}:{Main.TransportSettings.ServerPort}");
						return;
				}

				OmniLogger.Print($"Client successfully connected! Endpoint: {peer.EndPoint}");
			}
		}

		private void OnMessageReceived(bool isServer, byte[] data, int length, NetworkPeer peer)
		{
#if UNITY_EDITOR
			NetworkHelper.ThrowAnErrorIfConcurrent();
#endif
			IDataReader header = DataReaderPool.Get();
			header.Write(data, 0, length);
			byte dType = header.ReadByte();
			// Mount header
			DataDeliveryMode deliveryMode = Unsafe.As<byte, DataDeliveryMode>(ref dType);
			// Check if header is valid!
			// .....................................
			// .....................................
			// .....................................
			// We will process the header and decrypt the data if necessary.
			// We will use the aes key that was exchanged between client and server using the rsa public key.
			if (deliveryMode == DataDeliveryMode.ReliableEncryptedOrdered || deliveryMode == DataDeliveryMode.ReliableEncryptedUnordered || deliveryMode == DataDeliveryMode.ReliableEncryptedSequenced)
			{
				int ivLength = header.Read7BitEncodedInt();
				byte[] ivKey = new byte[ivLength];
				header.Read(ivKey, 0, ivLength);

				int datalength = header.Read7BitEncodedInt();
				byte[] encryptedData = new byte[datalength];
				header.Read(encryptedData, 0, datalength);
				byte[] decryptedData = AesCryptography.Decrypt(encryptedData, 0, datalength, isServer ? peer.AesKey : Main.AesKey, ivKey);

				header.Clear();
				header.Write(decryptedData, 0, decryptedData.Length);
			}

			// After processing the header, we will process the messages
			byte bMessage = header.ReadByte();
			NetMessage msgType = Unsafe.As<byte, NetMessage>(ref bMessage);
			switch (msgType)
			{
				case NetMessage.Ping:
					{
						if (isServer)
						{
							IDataWriter pingResponse = DataWriterPool.Get();
							pingResponse.Write((byte)NetMessage.Ping);
							SendToClient(pingResponse, peer.Id, DataDeliveryMode.Unreliable, 0);
							DataWriterPool.Release(pingResponse);
						}
					}
					break;
				case NetMessage.Message:
				case NetMessage.InternalMessage:
					{
						IDataReader reader = DataReaderPool.Get();
						reader.Write(header.Buffer, header.Position, header.BytesWritten);
						if (msgType == NetMessage.Message) NetworkCallbacks.FireCustomMessage(isServer, reader, peer);
						else if (msgType == NetMessage.InternalMessage) NetworkCallbacks.Internal_FireCustomMessage(isServer, reader, peer, deliveryMode);
						DataReaderPool.Release(reader);
					}
					break;
				case NetMessage.LargeBlockOfBytes:
				case NetMessage.InternalLargeBlockOfBytes:
					{
						// Retrieve the data block from the peer.
						NetworkPeer.Block dataBlock = peer.DataBlock;
						// Read the size of the data block.
						int dataSize = header.Read7BitEncodedInt();
						// LargeDataOption enum
						int option = header.ReadInt();
						// Initialize the data block with the expected size.
						// Write the received data to the data block's reader
						dataBlock.Initialize(dataSize);
						IDataReader reader = dataBlock.Reader;
						reader.Write(header.Buffer, header.Position, header.BytesWritten);
						// Check if the data block is complete.
						if (dataBlock.IsCompleted())
						{
							// After the fragmented chunks are complete, let's set the position to 0.
							// Position 0 so we can read the data.
							reader.Position = 0;
							// The message is complete!
							if (msgType == NetMessage.LargeBlockOfBytes) NetworkCallbacks.FireLargeData(isServer, option, reader, peer);
							else if (msgType == NetMessage.InternalLargeBlockOfBytes) NetworkCallbacks.Internal_FireLargeData(isServer, option, reader, peer, deliveryMode);
							dataBlock.Finish(); // Clear the data block.
						}
					}
					break;
				case NetMessage.Rpc:
					{
						int identityId = header.Read7BitEncodedInt();
						byte networkBehaviourId = header.ReadByte();
						byte rpcId = header.ReadByte();
						// We will execute the delegate responsible for the RPC received.
						if (NetworkIdentities.TryGetValue(ValueTuple.Create(identityId, isServer), out NetworkIdentity identity))
						{
							if (identity.NetworkBehaviours.TryGetValue(networkBehaviourId, out NetworkBehaviour networkBehaviour))
							{
								if (networkBehaviour.RemoteFuncs.TryGetValue(rpcId, out Action<IDataReader, NetworkPeer> func))
								{
									IDataReader reader = DataReaderPool.Get();
									reader.Write(header.Buffer, header.Position, header.BytesWritten);
									func(reader, peer);
									DataReaderPool.Release(reader);
								}
								else
								{
									OmniLogger.PrintError($"Error: Remote function with ID '{rpcId}' not found in NetworkBehaviour.");
								}
							}
							else
							{
								OmniLogger.PrintError($"Error: NetworkBehaviour with ID '{networkBehaviourId}' not found in NetworkIdentity.");
							}
						}
						else
						{
							OmniLogger.PrintError($"Error: NetworkIdentity with ID '{identityId}' and IsServer '{isServer}' not found.");
						}
					}
					break;
				case NetMessage.NetVar:
					{
						int identityId = header.Read7BitEncodedInt();
						byte networkBehaviourId = header.ReadByte();
						byte netVarId = header.ReadByte();
						// We will execute the delegate responsible for the NetVar received.
						if (NetworkIdentities.TryGetValue(ValueTuple.Create(identityId, isServer), out NetworkIdentity identity))
						{
							if (identity.NetworkBehaviours.TryGetValue(networkBehaviourId, out NetworkBehaviour networkBehaviour))
							{
								IDataReader reader = DataReaderPool.Get();
								reader.Write(header.Buffer, header.Position, header.BytesWritten);
								networkBehaviour.Internal___2205032023(netVarId, reader); // Roslyn Generated //
								DataReaderPool.Release(reader);
							}
							else
							{
								OmniLogger.PrintError($"Error: NetworkBehaviour with ID '{networkBehaviourId}' not found in NetworkIdentity.");
							}
						}
						else
						{
							OmniLogger.PrintError($"Error: NetworkIdentity with ID '{identityId}' and IsServer '{isServer}' not found.");
						}
					}
					break;
				case NetMessage.SerializeView:
					{
						int identityId = header.Read7BitEncodedInt();
						byte networkBehaviourId = header.ReadByte();
						// We will execute the delegate responsible for the OnSerializeView received.
						if (NetworkIdentities.TryGetValue(ValueTuple.Create(identityId, isServer), out NetworkIdentity identity))
						{
							if (identity.NetworkBehaviours.TryGetValue(networkBehaviourId, out NetworkBehaviour networkBehaviour))
							{
								IDataReader reader = DataReaderPool.Get();
								reader.Write(header.Buffer, header.Position, header.BytesWritten);
								networkBehaviour.OnDeserializeView(reader, peer);
								DataReaderPool.Release(reader);
							}
							else
							{
								OmniLogger.PrintError($"Error: NetworkBehaviour with ID '{networkBehaviourId}' not found in NetworkIdentity.");
							}
						}
						else
						{
							OmniLogger.PrintError($"Error: NetworkIdentity with ID '{identityId}' and IsServer '{isServer}' not found.");
						}
					}
					break;
				default:
					OmniLogger.PrintError("Not implemented message!");
					break;
			}
			DataReaderPool.Release(header);
		}

		private void OnClientDisconnected(bool isServer, NetworkPeer peer, SocketError socketError, string reason)
		{
#if UNITY_EDITOR
			NetworkHelper.ThrowAnErrorIfConcurrent();
#endif
			if (isServer)
			{
				if (PeersById.Remove(peer.Id))
				{
					OmniLogger.Print($"The server disconnected the player: {peer.EndPoint} -> Id: {peer.Id} | code: {socketError} reason: {reason}");
					if (peer.Channel != 0)
						Matchmaking.LeaveChannel(isServer, peer, peer.Channel);
					NetworkCallbacks.FireClientDisconnected(isServer, peer, socketError, reason);
				}
				else
				{
					OmniLogger.PrintError($"Failed to disconnect. Endpoint {peer.EndPoint} is not connected! -> Id: {peer.Id}");
				}
			}
			else
			{
				NetworkCallbacks.FireClientDisconnected(isServer, peer, socketError, reason);
				if (Main.TransportOption == TransportOption.WebSocketTransport)
				{
					OmniLogger.Print($"Client disconnected. Endpoint: {Main.TransportSettings.Host}:{Main.TransportSettings.ServerPort} | code: {socketError} reason: {reason}");
					return;
				}

				OmniLogger.Print($"Client disconnected. Endpoint: {peer.EndPoint} | code: {socketError} reason: {reason}");
			}
		}

		internal void SendToClient(IDataWriter writer, int peerId, DataDeliveryMode dataDeliveryMode, byte sequenceChannel)
		{
			if (PeersById.TryGetValue(peerId, out NetworkPeer peer))
			{
				SendToClient(writer, peer.EndPoint, dataDeliveryMode, sequenceChannel, peer.AesKey);
			}
			else
			{
				OmniLogger.PrintError($"The player with the id: {peerId} is not connected to the server!");
			}
		}

		internal void SendToClient(IDataWriter writer, EndPoint endPoint, DataDeliveryMode dataDeliveryMode, byte sequenceChannel, byte[] key)
		{
			IDataWriter header = CreateHeader(writer, dataDeliveryMode, key);
			ServerTransport.SendToClient(header.Buffer, header.BytesWritten, endPoint, dataDeliveryMode, sequenceChannel);
			DataWriterPool.Release(header);
		}

		internal void SendToServer(IDataWriter writer, DataDeliveryMode dataDeliveryMode, byte sequenceChannel)
		{
			IDataWriter header = CreateHeader(writer, dataDeliveryMode, Main.AesKey);
			ClientTransport.SendToServer(header.Buffer, header.BytesWritten, dataDeliveryMode, sequenceChannel);
			DataWriterPool.Release(header);
		}

		private IDataWriter CreateHeader(IDataWriter writer, DataDeliveryMode dataDeliveryMode, byte[] key)
		{
			IDataWriter header = DataWriterPool.Get();
			header.Write((byte)dataDeliveryMode);
			if (dataDeliveryMode == DataDeliveryMode.ReliableEncryptedOrdered || dataDeliveryMode == DataDeliveryMode.ReliableEncryptedUnordered || dataDeliveryMode == DataDeliveryMode.ReliableEncryptedSequenced)
			{
				byte[] encryptedData = AesCryptography.Encrypt(writer.Buffer, 0, writer.BytesWritten, key, out byte[] Iv);
				header.Write7BitEncodedInt(Iv.Length);
				header.Write(Iv);
				header.Write7BitEncodedInt(encryptedData.Length);
				header.Write(encryptedData);
			}
			else header.Write(writer.Buffer, 0, writer.BytesWritten);
			return header;
		}

		public void Disconnect()
		{
			if (IsClientInitialized())
			{
				ClientTransport.Disconnect(LocalPeer.EndPoint, SocketError.Shutdown, "The client has closed the connection.");
			}
			else
			{
				throw new InvalidOperationException("Error: Client is not initialized. Cannot disconnect.");
			}
		}

		public void DisconnectPeer(EndPoint endPoint)
		{
			if (IsServerInitialized())
			{
				ServerTransport.Disconnect(endPoint, SocketError.Shutdown, "The server closed the peer connection.");
			}
			else
			{
				throw new InvalidOperationException("Error: Server is not initialized. Cannot disconnect peer.");
			}
		}

		public void DisconnectPeer(int peerId)
		{
			if (PeersById.TryGetValue(peerId, out NetworkPeer peer))
			{
				DisconnectPeer(peer.EndPoint);
			}
			else
			{
				OmniLogger.PrintError($"The player with the id: {peerId} is not connected to the server!");
			}
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
	}
}
