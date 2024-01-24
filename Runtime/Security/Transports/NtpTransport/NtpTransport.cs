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

using System;
using System.Net;
using System.Net.Sockets;

namespace Omni.Internal.Transport
{
	internal class NtpTransport
	{
		private Socket udpSocket;
		private readonly byte[] buffer = new byte[1500]; // MTU Size
		private EndPoint peer = new IPEndPoint(IPAddress.Any, 0);

		public event Action<byte[], int, EndPoint> OnDataReceived;

		public void Bind(EndPoint endPoint)
		{
			udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			udpSocket.Bind(endPoint);
		}

		public void Receive()
		{
			if (udpSocket != null)
			{
				if (udpSocket.Available > 0)
				{
					int len = udpSocket.ReceiveFrom(buffer, SocketFlags.None, ref peer);
					OnDataReceived?.Invoke(buffer, len, peer);
				}
			}
		}

		public void Send(byte[] buffer, int size, EndPoint endPoint)
		{
			int offset = 0;
			while (offset < size)
			{
				int len = udpSocket.SendTo(buffer, offset, size - offset, SocketFlags.None, endPoint);
				offset += len;
			}
		}

		public void Close()
		{
			if (udpSocket != null)
			{
				udpSocket.Close();
			}
		}
	}
}