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
