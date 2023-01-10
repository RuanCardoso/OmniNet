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
    public class GlobalMessagesTests : MonoBehaviour
    {
        private void Start()
        {
            NeutronNetwork.AddHandler<PlayerTests>(OnPlayerTests);
        }

        private void OnPlayerTests(ByteStream byteStream, bool isServer)
        {

        }

        private void Update()
        {
            if (Input.GetKeyUp(KeyCode.KeypadEnter))
            {
                PlayerTests player = new();
            }
        }
    }
}