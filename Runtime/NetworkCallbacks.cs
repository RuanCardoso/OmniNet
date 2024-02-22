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
		#region Server & Client
		public static event Action<bool, IDataReader, NetworkPeer> OnCustomMessageReceived;
		internal static void FireCustomMessage(bool isServer, IDataReader dataReader, NetworkPeer player) => OnCustomMessageReceived?.Invoke(isServer, dataReader, player);

		public static event Action<bool, int, IDataReader, NetworkPeer> OnLargeDataReceived;
		internal static void FireLargeData(bool isServer, int option, IDataReader dataReader, NetworkPeer player) => OnLargeDataReceived?.Invoke(isServer, option, dataReader, player);
		#endregion

		#region OnlyServer
		public static event Action<NetworkPeer> OnServerClientConnected;
		internal static void FireServerClientConnected(NetworkPeer player) => OnServerClientConnected?.Invoke(player);

		public static event Action<NetworkPeer> OnServerClientDisconnected;
		internal static void FireServerClientDisconnected(NetworkPeer player) => OnServerClientDisconnected?.Invoke(player);

		public static event Action<NetworkIdentity, NetworkPeer> OnServerGameObjectInstantiated;
		internal static void FireServerGameObjectInstantiated(NetworkIdentity identity, NetworkPeer player) => OnServerGameObjectInstantiated?.Invoke(identity, player);

		public static event Action<NetworkIdentity, NetworkPeer> OnServerGameObjectDestroyed;
		internal static void FireServerGameObjectDestroyed(NetworkIdentity identity, NetworkPeer player) => OnServerGameObjectDestroyed?.Invoke(identity, player);
		#endregion

		#region Internal
		internal static event Action<bool, IDataReader, NetworkPeer, DataDeliveryMode> Internal_OnCustomMessageReceived;
		internal static void Internal_FireCustomMessage(bool isServer, IDataReader dataReader, NetworkPeer player, DataDeliveryMode deliveryMode) => Internal_OnCustomMessageReceived?.Invoke(isServer, dataReader, player, deliveryMode);

		internal static event Action<bool, int, IDataReader, NetworkPeer, DataDeliveryMode> Internal_OnLargeDataReceived;
		internal static void Internal_FireLargeData(bool isServer, int option, IDataReader dataReader, NetworkPeer player, DataDeliveryMode deliveryMode) => Internal_OnLargeDataReceived?.Invoke(isServer, option, dataReader, player, deliveryMode);
		#endregion
	}
}