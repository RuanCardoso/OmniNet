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

namespace Neutron.Core
{
    public class NeutronClone : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField][HideInInspector] private bool loop = false;
#endif
#if UNITY_EDITOR
        private void Awake()
        {
            try
            {
                if (!loop)
                {
                    loop = true;
                    Instantiate(gameObject);
                }
                else SceneManager.MoveGameObjectToScene(gameObject, NeutronNetwork.Scene);
            }
            catch
            {
                Logger.PrintWarning("Unable to move object to target scene -> Server.");
            }
        }
#endif
    }
}
