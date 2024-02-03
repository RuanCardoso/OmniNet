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

namespace Omni.Core
{
	public class NetworkPeer
	{
		public int Id { get; }
		public string Name { get; private set; }
		public int DatabaseId { get; private set; }
		public EndPoint EndPoint { get; }
		public Dictionary<int, object> Properties { get; } = new();

		internal NetworkPeer(int id, EndPoint endPoint)
		{
			Id = id;
			DatabaseId = -1;
			EndPoint = endPoint;
		}

		public void SetDatabaseId(int databaseId) => DatabaseId = databaseId;
		public void SetName(string name) => Name = name;
	}
}