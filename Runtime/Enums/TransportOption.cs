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

using UnityEngine;

namespace Omni.Core
{
	public enum TransportOption
	{
		TcpTransport = 0,
		[InspectorName("Lite Transport")]
		LiteNetTransport = 1,
		[InspectorName("Web Transport")]
		WebSocketTransport = 2,
	}
}