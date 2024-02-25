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

using System.Collections.Generic;

namespace Omni.Core.IMatchmaking
{
	public class Channel
	{
		public Dictionary<int, NetworkPeer> PeersById { get; } = new();
	}
}