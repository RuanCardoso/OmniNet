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
    public class AuthorityTests : NeutronObject
    {
        private void Awake()
        {
            Identity.OnAfterRegistered += () => Logger.Print($"{nameof(Awake)} -> {Identity.itIsRegistered}");
        }

        private void Start()
        {
            Logger.Print($"{nameof(Start)} -> {Identity.itIsRegistered}");
        }

        private void Update()
        {

        }

        private void OnEnable()
        {
            Identity.OnAfterRegistered += () => Logger.Print($"{nameof(OnEnable)} -> {Identity.itIsRegistered}");
        }
    }
}