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
    public class NeutronObject : ActionDispatcher
    {
        private bool hasIdentity;
        [SerializeField] private NeutronIdentity identity;
        [SerializeField] private byte id;

        internal byte Id => id;
        protected bool IsItFromTheServer { get; private set; }

        protected virtual void Awake()
        {
            void Init()
            {
                hasIdentity = true;
                IsItFromTheServer = identity.isItFromTheServer;
                GetAttributes();
            }

            if (identity == null)
            {
                if (!transform.root.TryGetComponent(out identity))
                    Logger.PrintError($"{gameObject.name} -> The NeutronIdentity component is missing.");
                else Init();
            }
            else Init();
        }

        private void GetAttributes()
        {
            Type typeOf = GetType();
            MethodInfo[] methods = typeOf.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method != null)
                {
                    RemoteAttribute attr = method.GetCustomAttribute<RemoteAttribute>(true);
                    if (attr != null)
                    {
                        if (method.GetParameters().Length < 0)
                            Logger.PrintError($"Remote method with id: {attr.id} -> name: {method.Name} -> requires the (ByteStream, bool, int) parameter in the same order as the method signature.");
                        else
                        {
                            Action remote = method.CreateDelegate(typeof(Action), this) as Action;
                            identity.AddRpc(id, attr.id, remote);
                        }
                    }
                    else continue;
                }
                else continue;
            }
        }

        private void GetSyncValues()
        {

        }

#pragma warning disable IDE1006
        protected void Remote(ByteStream parameters, Channel channel, Target target)
#pragma warning restore IDE1006
        {
            if (hasIdentity && identity.isRegistered)
            {
                int playerId = IsItFromTheServer ? identity.playerId : 0;
                switch (identity.objectType)
                {
                    case ObjectType.Player:
                        NeutronNetwork.Remote(parameters, MessageType.RemotePlayer, channel, target, playerId);
                        break;
                    case ObjectType.Scene:
                        NeutronNetwork.Remote(parameters, MessageType.RemoteScene, channel, target, playerId);
                        break;
                    case ObjectType.Instantiated:
                        NeutronNetwork.Remote(parameters, MessageType.RemoteInstantiated, channel, target, playerId);
                        break;
                    case ObjectType.Static:
                        NeutronNetwork.Remote(parameters, MessageType.RemoteStatic, channel, target, playerId);
                        break;
                }
            }
            else
            {
                parameters.Release();
                Logger.PrintError("This object has no identity or is not registered.");
            }
        }

#if UNITY_EDITOR
        protected virtual void Reset() => OnValidate();
        protected internal virtual void OnValidate()
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
                        Logger.PrintError($"Only {byte.MaxValue} NeutronObject are allowed in a NeutronIdentity!");
                }
            }
        }
#endif
    }
}