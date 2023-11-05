using UnityEngine;
using Omni.Core;
using Omni.Resolvers;

[DefaultExecutionOrder(-250)]
public class Omni_Tests_Resolver_Instance_MonoBehaviour : MonoBehaviour
{
    private void Awake()
    {
        OmniNetwork.AddResolver(Omni_Tests_Resolver.Instance);
    }
}
