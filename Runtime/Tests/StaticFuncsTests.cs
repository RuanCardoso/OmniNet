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
        public void SpawnPlayer(ByteStream parameters, bool isServer, ushort fromId, ushort toId)
        {
            Logger.Print($"Instantiate Player! Server={isServer}");
            //ushort uniqueId = parameters.ReadUShort();
            //NeutronIdentity identity = Instantiate(Player);
            //identity.Register(isServer, playerId, uniqueId);


            //Logger.LogError("Recebido RPC de: " + playerId);
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
            var stream = ByteStream.Get();
            stream.Write(dynamicId++);
            Remote(1, stream, Channel.Unreliable, Target.All, SubTarget.Server);
        }
    }
}
