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

namespace Omni.Internal
{
	public static class NetworkHelper
	{
		internal static bool IsAvailablePort(int port)
		{
			Socket sck1 = null, sck2 = null;
			try
			{
				// TCP
				sck1 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				sck1.Bind(new IPEndPoint(IPAddress.Any, port));
				sck1.Close();
				// UDP
				sck2 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				sck2.Bind(new IPEndPoint(IPAddress.Any, port));
				sck2.Close();
				/////////////
				return true;
			}
			catch (Exception)
			{
				sck1?.Close();
				sck2?.Close();
				return false;
			}
		}
	}
}