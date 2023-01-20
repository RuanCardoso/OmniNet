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
    [DisallowMultipleComponent]
    public class NeutronServerOwnership : MonoBehaviour
    {
        [SerializeField] private bool toDestroy = false;
        [SerializeField] private Component[] components;

        private void Start()
        {
#if UNITY_SERVER && !UNITY_EDITOR
            for (int i = 0; i < components.Length; i++)
            {
                if (toDestroy)
                    Destroy(components[i] is Transform ? components[i].gameObject : components[i]);
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
#endif
        }
    }
}