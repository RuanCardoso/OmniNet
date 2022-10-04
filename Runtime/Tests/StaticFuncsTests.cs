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
        [Remote(1)]
        public void SpawnPlayer()
        {
            Logger.PrintError("received rpc");
        }

        protected override void Update()
        {
            if (!IsItFromTheServer)
            {
                if (Input.GetKeyDown(KeyCode.M))
                {
                    var stream = ByteStream.Get();
                    Remote(1, stream, Channel.Unreliable, Target.All);
                }
            }
        }
    }
}
