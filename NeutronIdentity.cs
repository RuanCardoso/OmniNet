using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class NeutronIdentity : MonoBehaviour
{
    private readonly Dictionary<byte, Action> iRPCMethods = new();
    private void Awake()
    {
        Type typeOf = this.GetType();
        MethodInfo[] methods = typeOf.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo method = methods[i];
            if (method != null)
            {
                iRPCAttribute attr = method.GetCustomAttribute<iRPCAttribute>(true);
                if (attr != null)
                {
                    if (method.GetParameters().Length < 0)
                        throw new Exception($"iRPC method with id: {attr.id} -> name: {method.Name} -> requires the (AtomStream, bool, int) parameter in the same order as the method signature.");

                    Action iRPC = method.CreateDelegate(typeof(Action), this) as Action;
                    if (!iRPCMethods.TryAdd(attr.id, iRPC))
                        throw new Exception($"iRPC method with id: {attr.id} -> name: {method.Name} -> already exists. Obs: Don't add this to multi-instance objects, eg: Your player. A unique id is required.");
                }
            }
        }
    }
}