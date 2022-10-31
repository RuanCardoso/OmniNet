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
    [AddComponentMenu("")]
    public class PlayerControllerTests : NeutronObject
    {
        [Remote(1)]
        public void SpawnPlayer(ByteStream parameters, bool isServer, ushort playerId)
        {
            Debug.LogError("Players" + identity.id + isServer);
        }

        protected override void Update()
        {
            if (IsMine)
            {
                if (Input.GetKeyDown(KeyCode.O))
                {
                    Debug.LogError("enviado! " + IsItFromTheServer);
                    var stream = ByteStream.Get();
                    //Remote(1, stream, Channel.Unreliable, Target.All);
                }
            }
        }
    }
}