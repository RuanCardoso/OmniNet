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

using Omni.Core;
using System;
using System.Net;

namespace Omni.Internal.Transport
{
	internal class TcpTransportClient<T> : TransportClient<T>
	{
		internal byte[] Buffer { get; }
		internal int ExpectedLength { get; private set; } = 2; // 2 Bytes(prefix) - ushort(65535) - 65kb(max receive)
		internal bool PendingMessage { get; private set; }

		internal TcpTransportClient(byte[] buffer, T peer, EndPoint endPoint) : base(endPoint, peer)
		{
			Buffer = buffer;
		}

		internal void SetExpectedLength(int length, bool pendingMessage)
		{
			ExpectedLength = length;
			PendingMessage = pendingMessage;
		}
	}

	internal class LiteTransportClient<T> : TransportClient<T>
	{
		internal LiteTransportClient(T peer, EndPoint endPoint) : base(endPoint, peer)
		{
		}
	}

	internal class TransportClient<T>
	{
		public TransportClient(EndPoint endPoint, T peer)
		{
			Peer = peer;
			if (endPoint != null)
			{
				EndPoint = endPoint;
				NetworkPlayer = new NetworkPlayer(((IPEndPoint)endPoint).Port, endPoint);
			}
			else
			{
				throw new Exception("Transport: EndPoint cannot be null!");
			}
		}

		internal NetworkPlayer NetworkPlayer { get; }
		internal EndPoint EndPoint { get; }
		internal T Peer { get; }
	}
}