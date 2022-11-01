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

namespace Neutron.Core.Tests
{
    public class StaticFuncsTests : NeutronObject
    {
        public NeutronIdentity Player;

        [Remote(1)]
        public void SpawnPlayer(ByteStream parameters, ushort fromId, ushort toId, RemoteStats stats)
        {
            Logger.Print($"Instantiate Player! Server={IsServer} {fromId} | {toId} | {IsServer}");
        }

        ushort dynamicId = 1;
        protected override void Update()
        {
            if (IsItFromTheServer)
            {
                if (Input.GetKeyDown(KeyCode.P))
                {
                    SpawnPlayer(NeutronNetwork.Id);
                }
            }
        }

        private void SpawnPlayer(int id)
        {
            var stream = Get;
            stream.Write(dynamicId++);
            Remote(1, (ushort)20, (ushort)id, stream, Channel.Unreliable, Target.All, SubTarget.Server);
            stream.Release();
        }
    }
}
