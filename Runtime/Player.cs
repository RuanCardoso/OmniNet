using System.Collections.Generic;
using System.Net;

namespace Neutron.Core
{
    public class Player
    {
        public int Id { get; }
        public IPEndPoint IPEndPoint { get; }
        public int DatabaseId { get; private set; }
#if NEUTRON_MULTI_THREADED
        public readonly ConcurrentDictionary<ushort, object> properties = new();
#else
        public readonly Dictionary<ushort, object> properties = new();
#endif
        internal Player(int id, UdpEndPoint endPoint)
        {
            Id = id;
            DatabaseId = -1;
            IPEndPoint = new(endPoint.GetIPAddress(), endPoint.GetPort());
        }

        public void SetDatabaseId(int databaseId) => DatabaseId = databaseId;
    }
}