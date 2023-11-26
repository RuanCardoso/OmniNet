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
    public class OmniPlayer
    {
        public int Id { get; }
        public string Name { get; private set; }
        public IPEndPoint IPEndPoint { get; }
        public int DatabaseId { get; private set; }
        public OmniIdentity Identity { get; private set; }
        public readonly Dictionary<ushort, object> properties = new();

        internal OmniPlayer(int id, UdpEndPoint endPoint)
        {
            Id = id;
            DatabaseId = -1;
            IPEndPoint = new(endPoint.GetIPAddress(), endPoint.GetPort());
        }

        public void SetDbId(int databaseId) => DatabaseId = databaseId;
        public void SetName(string name) => Name = name;
        public void SetIdentity(OmniIdentity identity) => Identity = identity;
    }
}