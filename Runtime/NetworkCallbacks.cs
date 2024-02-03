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
		public static event Action<bool, IDataReader, NetworkPeer> OnCustomMessage; // Is Server // Player Id
		internal static void FireCustomMessage(bool isServer, IDataReader dataReader, NetworkPeer player) => OnCustomMessage?.Invoke(isServer, dataReader, player);

		internal static event Action<bool, IDataReader, NetworkPeer> Internal_OnCustomMessage; // Is Server // Player Id
		internal static void Internal_FireCustomMessage(bool isServer, IDataReader dataReader, NetworkPeer player) => Internal_OnCustomMessage?.Invoke(isServer, dataReader, player);
	}
}