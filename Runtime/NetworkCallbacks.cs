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
using System.Net.Sockets;

namespace Omni.Core
{
	public static class NetworkCallbacks
	{
		#region Server & Client
		public static event Action<bool, IDataReader, NetworkPeer> OnCustomMessageReceived;
		internal static void FireCustomMessage(bool isServer, IDataReader dataReader, NetworkPeer player) => OnCustomMessageReceived?.Invoke(isServer, dataReader, player);

		public static event Action<bool, int, IDataReader, NetworkPeer> OnLargeDataReceived;
		internal static void FireLargeData(bool isServer, int option, IDataReader dataReader, NetworkPeer player) => OnLargeDataReceived?.Invoke(isServer, option, dataReader, player);

		public static event Action<bool, NetworkIdentity, NetworkPeer> OnGameObjectInstantiated;
		internal static void FireGameObjectInstantiated(bool isServer, NetworkIdentity identity, NetworkPeer player) => OnGameObjectInstantiated?.Invoke(isServer, identity, player);

		public static event Action<bool, NetworkIdentity, NetworkPeer> OnGameObjectDestroyed;
		internal static void FireGameObjectDestroyed(bool isServer, NetworkIdentity identity, NetworkPeer player) => OnGameObjectDestroyed?.Invoke(isServer, identity, player);

		public static event Action<bool, NetworkPeer> OnClientConnected;
		internal static void FireClientConnected(bool isServer, NetworkPeer player) => OnClientConnected?.Invoke(isServer, player);

		public static event Action<bool, NetworkPeer, SocketError, string> OnClientDisconnected;
		internal static void FireClientDisconnected(bool isServer, NetworkPeer player, SocketError socketError, string reason) => OnClientDisconnected?.Invoke(isServer, player, socketError, reason);

		public static event Action<bool, NetworkPeer, int> OnChannelPlayerJoined;
		internal static void FireChannelPlayerJoined(bool isServer, NetworkPeer player, int channel) => OnChannelPlayerJoined?.Invoke(isServer, player, channel);

		public static event Action<bool, NetworkPeer, int> OnChannelPlayerLeft;
		internal static void FireChannelPlayerLeft(bool isServer, NetworkPeer player, int channel) => OnChannelPlayerLeft?.Invoke(isServer, player, channel);
		#endregion

		#region Internal
		internal static event Action<bool, IDataReader, NetworkPeer, DataDeliveryMode> Internal_OnCustomMessageReceived;
		internal static void Internal_FireCustomMessage(bool isServer, IDataReader dataReader, NetworkPeer player, DataDeliveryMode deliveryMode) => Internal_OnCustomMessageReceived?.Invoke(isServer, dataReader, player, deliveryMode);

		internal static event Action<bool, int, IDataReader, NetworkPeer, DataDeliveryMode> Internal_OnLargeDataReceived;
		internal static void Internal_FireLargeData(bool isServer, int option, IDataReader dataReader, NetworkPeer player, DataDeliveryMode deliveryMode) => Internal_OnLargeDataReceived?.Invoke(isServer, option, dataReader, player, deliveryMode);
		#endregion
	}
}