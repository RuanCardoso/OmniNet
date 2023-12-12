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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static Omni.Core.Enums;
using static Omni.Core.OmniNetwork;

namespace Omni.Core
{
	internal sealed class UdpServer : UdpSocket
	{
		protected override string Name => "Omni_Server";
		protected override bool IsServer => true;

		private readonly Dictionary<ushort, UdpClient> socketClients = new();
		internal UdpClient Client { get; private set; }
		internal UdpServer()
		{
			OnBind += UdpServer_OnBind;
		}

		internal void Initialize(ushort playerId)
		{
			Client = new UdpClient(true);
			socketClients.TryAdd(playerId, Client);
		}

		private void UdpServer_OnBind()
		{
			InternalEventHandler.OnTcpDataReceived += InternalEventHandler_OnTcpDataReceived;
		}

		private async void InternalEventHandler_OnTcpDataReceived(DataIOHandler IOHandler, UdpEndPoint remoteEndPoint, bool isServer, DataTarget dataTarget, DataProcessingOption dataProcessingOption, DataCachingOption dataCachingOption)
		{
			if (isServer)
			{
				MessageType messageType = IOHandler.ReadPacket();
				switch (messageType)
				{
					case MessageType.RSAExchange:
						{
							ushort uniqueId = (ushort)remoteEndPoint.GetPort();
							if (uniqueId == Port)
							{
								OmniLogger.PrintError($"Warning: Client connection denied. The port is in exclusive use -> {OmniNetwork.Port}");
								return;
							}

							UdpClient socketClient = new(remoteEndPoint, socket);
							ushort socketId = (ushort)remoteEndPoint.GetPort();
							if (socketClients.TryAdd(socketId, socketClient))
							{
								string RsaPublicKey = IOHandler.ReadString();
								await socketClient.GenerateAuthKeysAsync();
								DataIOHandler RsaIO = new(RSACryptography.IOHandlerSize);
								RsaIO.WritePacket(MessageType.RSAExchange);
								RsaIO.Write(socketClient.RsaPublicKey);
								socketClient.RsaPublicKey = RsaPublicKey;
								tcpServer.SendToClient(RsaIO, remoteEndPoint);
							}
							else
							{
								OmniLogger.PrintError($"Warning: Authentication failed!");
							}

							break;
						}

					case MessageType.AESExchange:
						{
							ushort uniqueId = (ushort)remoteEndPoint.GetPort();
							if (socketClients.TryGetValue(uniqueId, out UdpClient _client_))
							{
								var aesKey = IOHandler.ReadNBits();
								_client_.AesPrivateKey = RSACryptography.Decrypt(aesKey, _client_.RsaPrivateKey);
								DataIOHandler _IOHandler_ = new(RSACryptography.IOHandlerSize);
								_IOHandler_.WritePacket(messageType);
								_IOHandler_.Write(uniqueId);
								_IOHandler_.WriteNBits(RSACryptography.Encrypt(_client_.AesPublicKey, _client_.RsaPublicKey));
								tcpServer.SendToClient(_IOHandler_, remoteEndPoint);

								// Chama o evento OnMessage do OmniNetwork com os par�metros fornecidos
								OmniNetwork.OnMessage(IOHandler, messageType, DataDeliveryMode.Secured, dataTarget, dataProcessingOption, dataCachingOption, remoteEndPoint, isServer);
							}
							else
							{
								OmniLogger.PrintError($"Warning: Client connection denied.");
							}

							break;
						}
				}
			}
		}

		protected override void OnMessage(DataIOHandler IOHandler, DataDeliveryMode deliveryMode, DataTarget target, DataProcessingOption processingOption, DataCachingOption cachingOption, MessageType messageType, UdpEndPoint remoteEndPoint)
		{
			switch (messageType)
			{
				case MessageType.Disconnect:
					{
						Disconnect(remoteEndPoint, "Info: The endpoint {0} has been successfully disconnected.");

						// Chama o evento OnMessage do OmniNetwork com os par�metros fornecidos
						OmniNetwork.OnMessage(IOHandler, messageType, deliveryMode, target, processingOption, cachingOption, remoteEndPoint, IsServer);
					}
					break;
				case MessageType.Ping:
					{
						UdpClient client = GetClient(remoteEndPoint);
						if (client != null)
						{
							client.lastTimeReceivedPing = OmniTime.LocalTime;
							double timeOfClient = IOHandler.ReadDouble();
							DataIOHandler _IOHandler_ = DataIOHandler.Get(messageType);
							_IOHandler_.Write(timeOfClient);
							_IOHandler_.Write(OmniTime.LocalTime);
							client.Send(_IOHandler_, deliveryMode, target);
							_IOHandler_.Release();

							// Reposiciona o IOHandler para a posi��o 0
							// Chama o evento OnMessage do OmniNetwork com os par�metros fornecidos
							IOHandler.Position = 0;
							OmniNetwork.OnMessage(IOHandler, messageType, deliveryMode, target, processingOption, cachingOption, remoteEndPoint, IsServer);
						}
						else
						{
							OmniLogger.PrintError("Error: Attempted to ping a disconnected client!");
						}
					}
					break;
				case MessageType.PacketLoss:
					{
						UdpClient client = GetClient(remoteEndPoint);
						client?.Send(IOHandler);
					}
					break;
				default:
					OmniNetwork.OnMessage(IOHandler, messageType, deliveryMode, target, processingOption, cachingOption, remoteEndPoint, IsServer);
					break;
			}
		}

		internal override UdpClient GetClient(UdpEndPoint remoteEndPoint) => GetClient((ushort)remoteEndPoint.GetPort());
		internal UdpClient GetClient(ushort playerId) => socketClients.TryGetValue(playerId, out UdpClient udpClient) ? udpClient : null;

		internal void Send(DataIOHandler IOHandler, DataDeliveryMode deliveryMode, DataTarget target, DataProcessingOption processingOption, DataCachingOption cachingOption, ushort playerId) => Send(IOHandler, deliveryMode, target, processingOption, cachingOption, GetClient(playerId));
		internal void Send(DataIOHandler IOHandler, DataDeliveryMode deliveryMode, DataTarget target, DataProcessingOption processingOption, DataCachingOption cachingOption, UdpEndPoint remoteEndPoint) => Send(IOHandler, deliveryMode, target, processingOption, cachingOption, GetClient(remoteEndPoint));
		internal void Send(DataIOHandler IOHandler, DataDeliveryMode deliveryMode, DataTarget target, DataProcessingOption processingOption, DataCachingOption cachingOption, UdpClient sender)
		{
			// When we send the data to itself, we will always use the Unreliable deliveryMode.
			// LocalHost(Loopback), there are no risks of drops or clutter.
			if (sender != null)
			{
				switch (target)
				{
					case DataTarget.Self:
						{
							if (processingOption == DataProcessingOption.ProcessOnServer) // Defines whether to execute the instruction on the server when the server itself is the sender.
							{
								IOSend(IOHandler, Client.remoteEndPoint, DataDeliveryMode.Unsecured, target, processingOption, cachingOption);
							}

							if (!sender.isServer)
							{
								if (!IOHandler.isRawBytes)
								{
									sender.Send(IOHandler, deliveryMode, target, processingOption, cachingOption);
								}
								else
								{
									sender.Send(IOHandler);
								}
							}
							else if (processingOption != DataProcessingOption.ProcessOnServer)
							{
								OmniLogger.PrintError("Are you trying to execute this instruction on yourself? Please use DataProcessingOption.ProcessOnServer or verify if 'IsMine' is being used correctly.");
							}
						}
						break;
					case DataTarget.Broadcast:
						{
							if (processingOption == DataProcessingOption.ProcessOnServer)
							{
								IOSend(IOHandler, Client.remoteEndPoint, DataDeliveryMode.Unsecured, target, processingOption, cachingOption);
							}

							foreach (var (_, client) in socketClients)
							{
								if (client.isServer)
									continue;
								else
								{
									if (!IOHandler.isRawBytes)
									{
										client.Send(IOHandler, deliveryMode, target, processingOption, cachingOption);
									}
									else
									{
										client.Send(IOHandler);
									}
								}
							}
						}
						break;
					case DataTarget.BroadcastExcludingSelf:
						{
							if (processingOption == DataProcessingOption.ProcessOnServer)
							{
								IOSend(IOHandler, Client.remoteEndPoint, DataDeliveryMode.Unsecured, target, processingOption, cachingOption);
							}

							foreach (var (id, client) in socketClients)
							{
								if (client.isServer)
									continue;
								else
								{
									if (id != sender.Id)
									{
										if (!IOHandler.isRawBytes)
										{
											client.Send(IOHandler, deliveryMode, target, processingOption, cachingOption);
										}
										else
										{
											client.Send(IOHandler);
										}
									}
									else continue;
								}
							}
						}
						break;
					case DataTarget.Server:
						break;
					default:
						OmniLogger.PrintError($"Invalid target -> {target}");
						break;
				}
			}
			else
			{
				OmniLogger.PrintError("Error: Client not found. Ensure that the client is not mistakenly sending data through the server socket.");
			}
		}

		private bool RemoveClient(ushort uniqueId, out UdpClient disconnected)
		{
			return socketClients.Remove(uniqueId, out disconnected);
		}

		internal override void Close(bool fromServer = false)
		{
			base.Close();
			foreach (var udpClient in socketClients.Values)
			{
				udpClient.Close();
			}
		}

		protected override void Disconnect(UdpEndPoint endPoint, string msg = "")
		{
			ushort uniqueId = (ushort)endPoint.GetPort();
			if (RemoveClient(uniqueId, out UdpClient disconnected))
			{
				OmniLogger.Print(string.Format(msg, endPoint));
				Dictionaries.ClearDataCache(uniqueId);
				disconnected.Close(true);
			}
			else
			{
				OmniLogger.PrintError("Error: Failed to disconnect the client.");
			}
		}

		internal IEnumerator CheckTheLastReceivedPing(double maxPingRequestTime)
		{
			while (true)
			{
				UdpClient[] clients = this.socketClients.Values.ToArray();
				for (int i = 0; i < clients.Length; i++)
				{
					UdpClient client = clients[i];
					if (client.isServer)
					{
						continue;
					}
					else
					{
						if ((OmniTime.LocalTime - client.lastTimeReceivedPing) >= maxPingRequestTime)
						{
							Disconnect(client.remoteEndPoint, "Info: The endpoint {0} has been disconnected due to lack of response from the client.");
						}
						else
						{
							continue;
						}
					}
				}

				yield return OmniNetwork.WAIT_FOR_CHECK_REC_PING;
			}
		}
	}
}