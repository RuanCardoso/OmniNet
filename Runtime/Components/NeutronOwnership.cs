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

namespace Neutron.Core
{
    public class NeutronOwnership : NeutronObject
    {
        private bool HasAuthority
        {
            get
            {
                return authority switch
                {
                    AuthorityMode.Mine => IsMine,
                    AuthorityMode.Server => IsServer,
                    AuthorityMode.Client => IsClient,
                    AuthorityMode.Free => IsFree,
                    _ => default,
                };
            }
        }

        [SerializeField] private bool inverse;
        [SerializeField] private AuthorityMode authority = AuthorityMode.Mine;
        [SerializeField] private Component[] components;

        private void Start()
        {
            bool conditional = !inverse ? HasAuthority : !HasAuthority;
            for (int i = 0; i < components.Length && conditional; i++)
                Destroy(components[i] is Transform ? components[i].gameObject : components[i]);
        }
    }
}