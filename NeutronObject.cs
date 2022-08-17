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
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Neutron.Core
{
    [AddComponentMenu("")]
    public class NeutronObject : MonoBehaviour
    {
        [SerializeField] NeutronIdentity identity;
        [SerializeField] private byte id;
        private bool hasIdentity;
        internal byte Id => id;

        protected virtual void Awake()
        {
            if (identity == null)
            {
                identity = transform.root.GetComponent<NeutronIdentity>();
                if (identity == null)
                    Logger.PrintError($"{gameObject.name} -> The NeutronIdentity component is missing.");
                else hasIdentity = true;
            }
        }

        protected virtual void Start()
        {
            if (hasIdentity)
                GetAttributes();
        }

        private void GetAttributes()
        {
            Type typeOf = this.GetType();
            MethodInfo[] methods = typeOf.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Logger.Print(methods.Length);
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
                        identity.AddRpc(id, attr.id, iRPC);
                    }
                    else continue;
                }
                else continue;
            }
        }

#if UNITY_EDITOR
        protected virtual void Reset() => OnValidate();
        protected virtual void OnValidate()
        {
            if (!Application.isPlaying)
            {
                identity = transform.root.GetComponent<NeutronIdentity>();
                if (!(hasIdentity = identity != null))
                    Logger.PrintError($"The root object of {gameObject.name} must have a NeutronIdentity component.");
                if (hasIdentity)
                {
                    var neutronObjects = transform.root.GetComponentsInChildren<NeutronObject>(true);
                    if (neutronObjects.Length <= byte.MaxValue)
                    {
                        if (id == 0) id = (byte)Helper.GetAvailableId(neutronObjects, x => x.Id, byte.MaxValue);
                        else
                        {
                            int countIds = neutronObjects.Count(x => x.Id == id);
                            if (countIds > 1) id = 0;
                        }
                    }
                    else
                        Logger.PrintError($"Only {byte.MaxValue} NeutronIdentity are allowed in a  NeutronIdentity!");
                }
            }
        }
#endif
    }
}