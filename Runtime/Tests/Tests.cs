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

using Neutron.Core;
using UnityEngine;

namespace Neutron.Tests
{
    [AddComponentMenu("")]
    public class Tests : NeutronBehaviour
    {
        protected override byte Id => 1;

        [Remote(1)]
        private void TestGlobalRPC(ByteStream parameters, ushort fromId, ushort toId, bool isServer, RemoteStats stats)
        {
        }

        private void FixedUpdate()
        {
#if !UNITY_SERVER || UNITY_EDITOR
            Remote(1, new(0), false, target: Enums.Target.All, cacheMode: Enums.CacheMode.None, subTarget: Enums.SubTarget.Server);
#endif
        }
    }
}