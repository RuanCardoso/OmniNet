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
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;

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

		internal static void ThrowAnErrorIfConcurrent()
		{
			if (Thread.CurrentThread.ManagedThreadId != OmniNetwork.Omni.ManagedThreadId)
			{
				OmniLogger.LogError("Omni: Unity does not support operations outside the main thread.");
				throw new AccessViolationException("Omni: Unity does not support operations outside the main thread.");
			}
		}

		internal static unsafe int GetInt32FromGenericEnum<T>(T Enum) where T : unmanaged, IComparable, IConvertible, IFormattable
		{
			if (Unsafe.SizeOf<T>() != Unsafe.SizeOf<int>())
			{
				OmniLogger.PrintError($"Error: Cannot retrieve Int32 value from the generic enum. The size of type {typeof(T)} is not compatible with Int32 size.");
				return 0;
			}

			return *(int*)(&Enum);
		}
	}
}