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
using Logger = Neutron.Core.Logger;

namespace Neutron.Tests
{
    [AddComponentMenu("")]
    public class Tests : NeutronBehaviour
    {
        [Remote(1)]
        private void TestGlobalRPC(ByteStream parameters, ushort fromId, ushort toId, RemoteStats stats)
        {
            Logger.PrintError("AHHAHAHHA");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.U))
            {
                Remote(1, new(0), false, cacheMode: Enums.CacheMode.Overwrite, subTarget: Enums.SubTarget.Server);
            }

            if (Input.GetKeyDown(KeyCode.V))
            {
                NeutronNetwork.GetCache(Enums.CacheType.GlobalRemote, true, 1, false, Enums.Channel.Unreliable);
            }
        }
    }
}