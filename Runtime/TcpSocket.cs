using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static Omni.Core.Enums;
using static Omni.Core.OmniNetwork;
using static Omni.Core.PlatformSettings;

#pragma warning disable

namespace Omni.Core
{
	/// <summary>
	/// Represents a TCP socket used for asynchronous network communication.
	/// </summary>
	internal class TcpSocket
	{
		private bool isServer;
		private Socket tcpSocket;
		private CancellationTokenSource cts;
		private readonly Dictionary<UdpEndPoint, SocketClient> tcpSockets = new();

		internal async void Bind(UdpEndPoint localEndPoint, CancellationTokenSource cts, bool isServer)
		{
			try
			{
				this.cts = cts;
				this.isServer = isServer;
				tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				tcpSocket.NoDelay = true;
				tcpSocket.Bind(localEndPoint);
				if (isServer)
				{
					tcpSocket.Listen(5);
					while (!cts.IsCancellationRequested)
					{
						try
						{
							Socket acceptedSocket = await tcpSocket.AcceptAsync();
							acceptedSocket.NoDelay = true;
							SocketClient socketClient = new(acceptedSocket, true);
							UdpEndPoint remoteEndPoint = (UdpEndPoint)acceptedSocket.RemoteEndPoint;
							if (tcpSockets.TryAdd(remoteEndPoint, socketClient))
							{
								Receive(socketClient);
							}
							else
							{
								OmniLogger.PrintError("The client is already connected");
								socketClient.Free();
							}
						}
						catch (ObjectDisposedException) { continue; }
					}
				}
			}
			catch (SocketException ex)
			{
				if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
				{
					OmniLogger.PrintError("Operation failed: The TCP server is already running. Please stop the current server before attempting to start a new one.");
				}
				else
				{
					OmniLogger.PrintError(ex.Message);
				}
			}
		}

		SocketClient clientStream;
		internal async void Connect(UdpEndPoint remoteEndPoint)
		{
			try
			{
				await tcpSocket.ConnectAsync(remoteEndPoint);
				clientStream = new(tcpSocket, true);
				Receive(clientStream);
			}
			catch (SocketException ex)
			{
				if (ex.SocketErrorCode == SocketError.ConnectionRefused)
				{
					OmniLogger.PrintError("The connection was actively refused by the remote host. Please check if the server is running and accessible.");
				}
				else
				{
					OmniLogger.PrintError(ex.Message);
				}
			}
		}

		internal async void SendToServer(DataIOHandler IOHandler, DataTarget target = DataTarget.Self, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None, int offset = 0)
		{
			Send(IOHandler, clientStream, target, processingOption, cachingOption, offset);
		}

		internal async void SendToClient(DataIOHandler IOHandler, UdpEndPoint remoteEndPoint, DataTarget target = DataTarget.Self, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None, int offset = 0)
		{
			if (tcpSockets.TryGetValue(remoteEndPoint, out SocketClient stream))
			{
				Send(IOHandler, stream, target, processingOption, cachingOption, offset);
			}
			else
			{
				OmniLogger.PrintError("TCP Error: Failed to perform operation because the client is not connected to the server.");
			}
		}

		private async void Send(DataIOHandler IOHandler, SocketClient socketClient, DataTarget target = DataTarget.Self, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None, int offset = 0)
		{
			DataIOHandler data = DataIOHandler.Get();
			try
			{
				data.WritePayload(DataDeliveryMode.Secured, target, processingOption, cachingOption);
				data.Write(IOHandler.BytesWritten); // TCP Message Size -> 4 Bytes(int) - Message Framing (https://blog.stephencleary.com/2009/04/message-framing.html)
				data.Write(IOHandler);
				IOHandler.Release();
				await socketClient.WriteAsync(data.Buffer, offset, data.BytesWritten - offset, cts.Token); // << possible exception
				data.Release();
			}
			catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException || ex is SocketException || ex is Exception)
			{
				data.Release();
			}
		}

		private async void Receive(SocketClient socketClient)
		{
			byte[] buffer = socketClient.buffer;
			while (!cts.IsCancellationRequested && !socketClient.cts.IsCancellationRequested)
			{
				try
				{
					DataIOHandler IOHandler = DataIOHandler.Get();
					if (await ReadExactlyAsync(socketClient, sizeof(byte)))
					{
						IOHandler.WritePayload(buffer, sizeof(byte));
						IOHandler.ReadPayload(out _, out DataTarget target, out DataProcessingOption processingOption, out DataCachingOption cachingOption);
						if (await ReadExactlyAsync(socketClient, sizeof(int))) // Part of Message Framing (https://blog.stephencleary.com/2009/04/message-framing.html) - Read 4 Bytes(int) -> TCP Message Size
						{
							IOHandler.WritePayload(buffer, sizeof(int));
							int length = IOHandler.ReadInt();
							if (await ReadExactlyAsync(socketClient, length))
							{
								IOHandler.WritePayload(buffer, length);
								InternalEventHandler.FireTcpDataReceived(IOHandler, socketClient.remoteEndPoint, isServer, target, processingOption, cachingOption);
							}
							else
							{
								Disconnect(socketClient.remoteEndPoint);
							}
						}
						else
						{
							Disconnect(socketClient.remoteEndPoint);
						}
					}
					else
					{
						Disconnect(socketClient.remoteEndPoint);
					}
					IOHandler.Release();
				}
				catch (IOException) { continue; }
				catch (ObjectDisposedException) { continue; }
				catch (SocketException) { continue; }
				catch (Exception) { continue; }
			}
		}

		/// <summary>
		/// Message Framing (https://blog.stephencleary.com/2009/04/message-framing.html)
		/// Read exactly the number of bytes specified by the size parameter.
		/// </summary>
		private async Task<bool> ReadExactlyAsync(SocketClient socket, int size)
		{
			int offset = 0;
			while (offset < size)
			{
				try
				{
					int len = await socket.ReadAsync(socket.buffer, offset, size - offset, socket.cts.Token);
					offset += len;

					// The client has disconnected
					if (len == 0)
					{
						return false;
					}
				}
				catch (IOException) { return false; }
				catch (ObjectDisposedException) { return false; }
				catch (SocketException) { return false; }
				catch (Exception) { return false; }
			}
			return offset == size;
		}

		internal void Disconnect(UdpEndPoint remoteEndPoint)
		{
			if (tcpSockets.Remove(remoteEndPoint, out SocketClient socketClient))
			{
				OmniLogger.Print($"The client has disconnected from the server: {remoteEndPoint}");
				socketClient.Free();
			}
			else
			{
				OmniLogger.PrintError("The client is not connected to the server");
			}
		}

		internal void Disconnect()
		{
			clientStream.Free();
		}

		internal void Close()
		{
			foreach (var (_, socketClient) in tcpSockets)
			{
				socketClient.Free();
			}

			// Free the remaining resources!
			tcpSocket.Close();
			tcpSocket.Dispose();
		}
	}

	internal class SocketClient : NetworkStream
	{
		public Socket socket;
		public UdpEndPoint remoteEndPoint;
		public CancellationTokenSource cts;
		public byte[] buffer = new byte[1500];
		public SocketClient(Socket socket) : base(socket)
		{
			this.socket = socket;
			remoteEndPoint = (UdpEndPoint)socket.RemoteEndPoint;
			cts = new CancellationTokenSource();
		}

		public SocketClient(Socket socket, bool ownsSocket) : base(socket, ownsSocket)
		{
			this.socket = socket;
			remoteEndPoint = (UdpEndPoint)socket.RemoteEndPoint;
			cts = new CancellationTokenSource();
		}

		public SocketClient(Socket socket, FileAccess access) : base(socket, access)
		{
			this.socket = socket;
			remoteEndPoint = (UdpEndPoint)socket.RemoteEndPoint;
			cts = new CancellationTokenSource();
		}

		public SocketClient(Socket socket, FileAccess access, bool ownsSocket) : base(socket, access, ownsSocket)
		{
			this.socket = socket;
			remoteEndPoint = (UdpEndPoint)socket.RemoteEndPoint;
			cts = new CancellationTokenSource();
		}

		public void Free()
		{
			try
			{
				cts.Cancel();
				Close();
			}
			finally
			{
				cts.Dispose();
				Dispose();
			}
		}
	}
}