using Neutron.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NeutronServerObject : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField][HideInInspector] private bool avoidCloneLoop = false;
#endif
#if UNITY_EDITOR
    private void Awake()
    {
        if (!avoidCloneLoop)
        {
            avoidCloneLoop = true;
            Instantiate(gameObject);
        }
        else SceneManager.MoveGameObjectToScene(gameObject, NeutronNetwork.ServerScene);
    }
#endif
}
