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

using System.Linq;
using UnityEngine;

namespace Neutron.Core
{
    [AddComponentMenu("")]
    public class NeutronObject : MonoBehaviour
    {
        [SerializeField] private ushort id;
        private bool hasIdentity;
        internal ushort Id => id;

        protected virtual void Awake()
        {

        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                NeutronIdentity neutronIdentity = transform.root.GetComponent<NeutronIdentity>();
                if (!(hasIdentity = neutronIdentity != null))
                    Logger.PrintError($"The root object of {gameObject.name} must have a NeutronIdentity component.");
                if (hasIdentity)
                {
                    var behaviours = transform.root.GetComponentsInChildren<NeutronObject>();
                    if (behaviours.Length <= byte.MaxValue)
                    {
                        if (id == 0)
                            id = (byte)Helper.GetAvailableId(behaviours, x => x.Id, byte.MaxValue);
                        else
                        {
                            if (!(id >= byte.MaxValue))
                            {
                                int count = behaviours.Count(x => x.Id == id);
                                if (count > 1)
                                    id = 0;
                            }
                            else
                                Logger.PrintError($"The id of {gameObject.name} is greater than the {byte.MaxValue}.");
                        }
                    }
                    else
                        Logger.PrintError($"Only {byte.MaxValue} Neutron Behaviours are allowed in a Neutron View!");
                }
            }
        }
#endif
    }
}