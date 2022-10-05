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
using System.Reflection;
using UnityEngine;

namespace Neutron.Core
{
    [AddComponentMenu("")]
    public class NeutronObject : ActionDispatcher
    {
        [SerializeField] internal NeutronIdentity identity;
        [SerializeField] internal byte id;

        internal byte Id => id;
        protected bool IsItFromTheServer { get; private set; }

        protected virtual void Awake()
        {
            if (identity == null)
            {
                Logger.PrintError("Does this object not have an identity? Did you register the objects?");
                Destroy(gameObject);
            }
            else
            {
                IsItFromTheServer = identity.isItFromTheServer;
                GetAttributes();
            }
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
        protected void Remote(byte id, ByteStream parameters, Channel channel, Target target)
#pragma warning restore IDE1006
        {
            if (identity.isRegistered)
            {
                int playerId = IsItFromTheServer ? identity.playerId : 0;
                switch (identity.objectType)
                {
                    case ObjectType.Player:
                        NeutronNetwork.Remote(id, identity.id, this.id, parameters, MessageType.RemotePlayer, channel, target, playerId);
                        break;
                    case ObjectType.Scene:
                        NeutronNetwork.Remote(id, identity.id, this.id, parameters, MessageType.RemoteScene, channel, target, playerId);
                        break;
                    case ObjectType.Instantiated:
                        NeutronNetwork.Remote(id, identity.id, this.id, parameters, MessageType.RemoteInstantiated, channel, target, playerId);
                        break;
                    case ObjectType.Static:
                        NeutronNetwork.Remote(id, identity.id, this.id, parameters, MessageType.RemoteStatic, channel, target, playerId);
                        break;
                }
            }
            else
            {
                parameters.Release();
                Logger.PrintError("This object has no identity or is not registered.");
            }
        }
    }
}