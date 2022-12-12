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
using UnityEngine.SceneManagement;

namespace Neutron.Core.Tests
{
    [AddComponentMenu("")]
    public class StaticFuncsTests : NeutronObject
    {
        private void Start()
        {
            if (IsMine) SceneManager.LoadScene("Lobby 2", LoadSceneMode.Additive);
        }
    }
}
