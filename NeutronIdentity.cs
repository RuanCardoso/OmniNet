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

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neutron.Core
{
    public class NeutronIdentity : MonoBehaviour
    {
        private bool isInRoot = false;
        private readonly Dictionary<byte, Action> iRPCMethods = new();

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!(isInRoot = transform == transform.root))
                Logger.PrintError($"{gameObject.name} -> Only root objects can have a NeutronIdentity component.");
        }
#endif
    }
}