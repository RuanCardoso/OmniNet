using Neutron.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = Neutron.Core.Logger;

public class NeutronServerObject : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField][HideInInspector] private bool avoidCloneLoop = false;
#endif
#if UNITY_EDITOR
    private void Awake()
    {
        try
        {
            if (!avoidCloneLoop)
            {
                avoidCloneLoop = true;
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
