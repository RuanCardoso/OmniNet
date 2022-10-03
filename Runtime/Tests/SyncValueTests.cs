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
    public class SyncValueTests : NeutronObject
    {
        [SerializeField] private SyncValue<int> life = new();

        protected override void Awake()
        {
            base.Awake();
            life.Set(101);
        }

        private void Start()
        {
            int life = this.life;
            Debug.LogError($"Life: {life} - {life == 99}");
            Debug.LogError($"Life: {life} - {life == 101}");
            Debug.LogError($"Life: {this.life} - {life.Equals(100)}");
        }
    }
}