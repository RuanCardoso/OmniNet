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

namespace Neutron.Core
{
    [AddComponentMenu("Neutron/NeutronIdentity")]
    public class NeutronObject : MonoBehaviour
    {
        private bool hasIdentity = false;
        protected virtual void Awake()
        {

        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            NeutronIdentity neutronIdentity = transform.root.GetComponent<NeutronIdentity>();
            if (!(hasIdentity = neutronIdentity != null))
                Logger.PrintError($"The root object of {gameObject.name} must have a NeutronIdentity component.");
        }
#endif
    }
}