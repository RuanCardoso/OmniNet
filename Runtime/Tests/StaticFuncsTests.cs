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
using static Neutron.Core.Enums;

namespace Neutron.Core.Tests
{
    [AddComponentMenu("")]
    public class StaticFuncsTests : NeutronObject
    {
        private void Start()
        {
            if (IsMine)
            {
                ByteStream byteStream = ByteStream.Get();
                Remote(1, byteStream, cacheMode: CacheMode.Overwrite);
            }
        }

        protected override void Update()
        {
            base.Update();

            if (Input.GetKeyDown(KeyCode.V) && IsMine)
            {
                NeutronNetwork.GetCache(CacheType.Remote, 1, identity.playerId, IsServer, Channel.Unreliable);
            }
        }

        [Remote(1)]
        void RPC(ByteStream parameters, ushort fromId, ushort toId, RemoteStats stats)
        {
            Logger.PrintError($"Sistema de Cache funcional e corrigido! -> {IsServer} ");
        }
    }
}
