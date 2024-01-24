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
using System.Net;

namespace Omni.Internal.Interfaces
{
	internal interface ITransportClient<T>
	{
		Dictionary<EndPoint, T> PeerList { get; }
		T LocalTransportClient { get; }
	}
}
