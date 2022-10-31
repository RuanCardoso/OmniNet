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

using JetBrains.Annotations;
using Mono.Cecil;
using System;
using System.Linq;
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
        protected bool IsItFromTheServer => identity.isRegistered && identity.isItFromTheServer;
        protected bool IsMine => identity.isRegistered && (identity.playerId == NeutronNetwork.Id) && !identity.isItFromTheServer;
        protected bool IsServer => identity.isRegistered && identity.isItFromTheServer;
        protected bool IsClient => identity.isRegistered && !identity.isItFromTheServer;
        protected bool IsFree => identity.isRegistered;

        protected virtual void Awake()
        {
            if (identity == null)
            {
                Logger.PrintError("Does this object not have an identity? Did you register the objects?");
                Destroy(gameObject);
            }
            else GetAttributes();
        }

        private void GetAttributes()
        {
            #region Signature
            static MethodBase MethodSignature(ByteStream parameters, bool isServer, ushort fromId, ushort toId) => MethodBase.GetCurrentMethod();
            MethodBase methodSignature = MethodSignature(default, default, default, default);
            ParameterInfo[] parametersSignature = methodSignature.GetParameters();
            int parametersCount = parametersSignature.Length;

            void ThrowErrorIfSignatureIsIncorret(byte id, string name)
            {
                Logger.PrintError($"The signature of method with Id: {id} | name: {name} | type: {GetType().Name} is incorrect!");
                Logger.PrintError($"Correct -> private public void METHOD_NAME({string.Join(",", parametersSignature.Select(x => x.ToString()))});");
            }
            #endregion

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
                        ParameterInfo[] parameters = method.GetParameters();
                        if (parameters.Length != parametersCount) ThrowErrorIfSignatureIsIncorret(attr.id, method.Name);
                        else
                        {
                            try
                            {
                                var remote = method.CreateDelegate(typeof(Action<ByteStream, bool, ushort, ushort>), this) as Action<ByteStream, bool, ushort, ushort>;
                                identity.AddRpc(id, attr.id, remote);
                            }
                            catch (ArgumentException)
                            {
                                ThrowErrorIfSignatureIsIncorret(attr.id, method.Name);
                            }
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

        public void Remote(byte id, ByteStream parameters, Channel channel, Target target, SubTarget subTarget = SubTarget.None)
        {
            if (identity.isRegistered)
            {
                switch (identity.objectType)
                {
                    case ObjectType.Player:
                        NeutronNetwork.Remote(id, identity.id, this.id, identity.playerId, identity.playerId, identity.isItFromTheServer, parameters, MessageType.RemotePlayer, channel, target, subTarget);
                        break;
                    case ObjectType.Scene:
                        NeutronNetwork.Remote(id, identity.id, this.id, identity.playerId, identity.playerId, identity.isItFromTheServer, parameters, MessageType.RemoteScene, channel, target, subTarget);
                        break;
                    case ObjectType.Instantiated:
                        NeutronNetwork.Remote(id, identity.id, this.id, identity.playerId, identity.playerId, identity.isItFromTheServer, parameters, MessageType.RemoteInstantiated, channel, target, subTarget);
                        break;
                    case ObjectType.Static:
                        NeutronNetwork.Remote(id, identity.id, this.id, identity.playerId, identity.playerId, identity.isItFromTheServer, parameters, MessageType.RemoteStatic, channel, target, subTarget);
                        break;
                }
            }
            else
            {
                parameters.Release();
                Logger.PrintError("This object has no identity or is not registered.");
            }
        }

        public void Remote(byte id, ByteStream parameters, NeutronIdentity fromIdentity, Channel channel, Target target, SubTarget subTarget = SubTarget.None)
        {
            if (identity.isRegistered)
            {
                switch (identity.objectType)
                {
                    case ObjectType.Player:
                        NeutronNetwork.Remote(id, identity.id, this.id, fromIdentity.playerId, identity.playerId, IsItFromTheServer, parameters, MessageType.RemotePlayer, channel, target, subTarget);
                        break;
                    case ObjectType.Scene:
                        NeutronNetwork.Remote(id, identity.id, this.id, fromIdentity.playerId, identity.playerId, IsItFromTheServer, parameters, MessageType.RemoteScene, channel, target, subTarget);
                        break;
                    case ObjectType.Instantiated:
                        NeutronNetwork.Remote(id, identity.id, this.id, fromIdentity.playerId, identity.playerId, IsItFromTheServer, parameters, MessageType.RemoteInstantiated, channel, target, subTarget);
                        break;
                    case ObjectType.Static:
                        NeutronNetwork.Remote(id, identity.id, this.id, fromIdentity.playerId, identity.playerId, IsItFromTheServer, parameters, MessageType.RemoteStatic, channel, target, subTarget);
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