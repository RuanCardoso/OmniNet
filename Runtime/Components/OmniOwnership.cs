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
using static Omni.Core.Enums;

#pragma warning disable

namespace Omni.Core
{
    [DisallowMultipleComponent]
    public class OmniOwnership : OmniObject
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
                    AuthorityMode.Custom => IsCustom,
                    _ => default,
                };
            }
        }

        [SerializeField] private bool toDestroy = false;
        [SerializeField] private bool inverse;
        [SerializeField] private AuthorityMode authority = AuthorityMode.Mine;
        [SerializeField] private Component[] components;

        private void Start()
        {
            bool conditional = !inverse ? HasAuthority : !HasAuthority;
            for (int i = 0; i < components.Length && conditional; i++)
            {
                if (toDestroy)
                {
                    Destroy(components[i] is Transform ? components[i].gameObject : components[i]);
                }
                else
                {
                    if (components[i] is Transform) // Is a game object?
                    {
                        GameObject gO = components[i].gameObject;
                        gO.SetActive(false);
                    }
                    else
                    {
                        MonoBehaviour component = components[i] as MonoBehaviour;
                        component.enabled = false;
                    }
                }
            }
        }
    }
}