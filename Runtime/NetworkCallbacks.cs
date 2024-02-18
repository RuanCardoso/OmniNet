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

namespace Omni.Core
{
	public static class NetworkCallbacks
	{
		public static event Action<bool, IDataReader, NetworkPeer> OnCustomMessageReceived;
		internal static void FireCustomMessage(bool isServer, IDataReader dataReader, NetworkPeer player) => OnCustomMessageReceived?.Invoke(isServer, dataReader, player);

		public static event Action<bool, int, IDataReader, NetworkPeer> OnLargeDataReceived;
		internal static void FireLargeData(bool isServer, int option, IDataReader dataReader, NetworkPeer player) => OnLargeDataReceived?.Invoke(isServer, option, dataReader, player);

		internal static event Action<bool, IDataReader, NetworkPeer> Internal_OnCustomMessageReceived;
		internal static void Internal_FireCustomMessage(bool isServer, IDataReader dataReader, NetworkPeer player) => Internal_OnCustomMessageReceived?.Invoke(isServer, dataReader, player);

		internal static event Action<bool, int, IDataReader, NetworkPeer> Internal_OnLargeDataReceived;
		internal static void Internal_FireLargeData(bool isServer, int option, IDataReader dataReader, NetworkPeer player) => Internal_OnLargeDataReceived?.Invoke(isServer, option, dataReader, player);
	}
}