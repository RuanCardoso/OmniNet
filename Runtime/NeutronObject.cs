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

using NaughtyAttributes;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static Neutron.Core.Enums;

namespace Neutron.Core
{
    [AddComponentMenu("")]
    public class NeutronObject : ActionDispatcher
    {
        internal byte SYNC_BASE_ID = 1;
        private MessageType REMOTE_MSG_TYPE = MessageType.None;
        private MessageType SYNC_BASE_MSG_TYPE = MessageType.None;

        [Header("Registration")]
        [SerializeField][ReadOnly][Required("It is necessary to register neutron objects on the identity.")] internal NeutronIdentity identity;
        [SerializeField][ReadOnly] internal byte id;

        internal byte Id => id;
        protected ByteStream Get => ByteStream.Get();
        protected NeutronIdentity Identity => identity;
        protected bool IsItFromTheServer => identity.isRegistered && identity.isItFromTheServer;
        protected bool IsMine => identity.isRegistered && (identity.playerId == NeutronNetwork.Id) && !identity.isItFromTheServer;
        protected bool IsServer => identity.isRegistered && identity.isItFromTheServer;
        protected bool IsClient => identity.isRegistered && !identity.isItFromTheServer;
        protected bool IsFree => identity.isRegistered;

        #region OnSerializeView
        protected virtual bool OnSerializeViewAuthority => IsMine;
        protected virtual Channel OnSerializeViewChannel => Channel.Unreliable;
        protected virtual Target OnSerializeViewTarget => Target.Others;
        protected virtual SubTarget OnSerializeViewSubTarget => SubTarget.None;

        protected virtual bool OnSyncBaseAuthority => IsServer;
        protected virtual Channel OnSyncBaseChannel => Channel.Unreliable;
        protected virtual Target OnSyncBaseTarget => Target.All;
        protected virtual SubTarget OnSyncBaseSubTarget => SubTarget.None;
        #endregion

        protected virtual void Awake()
        {
            if (identity == null)
            {
                Logger.PrintError("Does this object not have an identity? Did you register the objects?");
                Destroy(gameObject);
            }
            else
            {
                REMOTE_MSG_TYPE = NeutronHelper.GetMessageTypeToRemote(identity.objectType);
                SYNC_BASE_MSG_TYPE = NeutronHelper.GetMessageTypeToOnSyncBase(identity.objectType);
                GetRemoteAttributes();
            }
        }

        protected void OnSerializeView(WaitForSeconds seconds) => StartCoroutine(SentOnSerializeView(seconds));
        private void GetRemoteAttributes()
        {
            #region Signature
            static MethodBase MethodSignature(ByteStream parameters, ushort fromId, ushort toId, RemoteStats stats) => MethodBase.GetCurrentMethod();
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
                                var remote = method.CreateDelegate(typeof(Action<ByteStream, ushort, ushort, RemoteStats>), this) as Action<ByteStream, ushort, ushort, RemoteStats>;
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



        private void Intern_Remote(byte id, byte sceneId, ushort fromId, ushort toId, ByteStream parameters, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode)
        {
            if (identity.isRegistered)
                NeutronNetwork.Remote(id, sceneId, identity.id, this.id, fromId, toId, IsItFromTheServer, parameters, REMOTE_MSG_TYPE, channel, target, subTarget, cacheMode);
            else parameters.Release();
        }

        protected void Remote(byte id, ByteStream parameters, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None) => Intern_Remote(id, identity.sceneId, identity.playerId, identity.playerId, parameters, channel, target, subTarget, cacheMode);
        protected void Remote(byte id, ByteStream parameters, NeutronIdentity fromIdentity, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None) => Intern_Remote(id, identity.sceneId, fromIdentity.playerId, identity.playerId, parameters, channel, target, subTarget, cacheMode);
        protected void Remote(byte id, ByteStream parameters, NeutronIdentity fromIdentity, NeutronIdentity toIdentity, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None) => Intern_Remote(id, toIdentity.sceneId, fromIdentity.playerId, toIdentity.playerId, parameters, channel, target, subTarget, cacheMode);
        protected void Remote(byte id, NeutronIdentity toIdentity, ByteStream parameters, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None) => Intern_Remote(id, toIdentity.sceneId, identity.playerId, toIdentity.playerId, parameters, channel, target, subTarget, cacheMode);
        protected void Remote(byte id, ByteStream parameters, ushort toId, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None) => Intern_Remote(id, identity.sceneId, identity.playerId, toId, parameters, channel, target, subTarget, cacheMode);
        protected void Remote(byte id, ByteStream parameters, byte sceneId, ushort toId, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None) => Intern_Remote(id, sceneId, identity.playerId, toId, parameters, channel, target, subTarget, cacheMode);
        protected void Remote(byte id, ushort fromId, ByteStream parameters, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None) => Intern_Remote(id, identity.sceneId, fromId, identity.playerId, parameters, channel, target, subTarget, cacheMode);
        protected void Remote(byte id, ushort fromId, ushort toId, ByteStream parameters, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None) => Intern_Remote(id, identity.sceneId, fromId, toId, parameters, channel, target, subTarget, cacheMode);
        protected void Remote(byte id, byte sceneId, ushort fromId, ushort toId, ByteStream parameters, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None) => Intern_Remote(id, sceneId, fromId, toId, parameters, channel, target, subTarget, cacheMode);

        #region Intern Network Methods
        private const byte SPAWN = 75;
        protected void SpawnRemote(Vector3 position, Quaternion rotation, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None)
        {
            ByteStream message = ByteStream.Get();
            message.Write(position);
            message.Write(rotation);
            Remote(SPAWN, message, channel, target, subTarget);
        }

        protected void SpawnRemote(ushort toId, Vector3 position, Quaternion rotation, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None)
        {
            ByteStream message = ByteStream.Get();
            message.Write(position);
            message.Write(rotation);
            Remote(SPAWN, message, toId, channel, target, subTarget);
        }

        protected void SpawnRemote(ushort fromId, ushort toId, Vector3 position, Quaternion rotation, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None)
        {
            ByteStream message = ByteStream.Get();
            message.Write(position);
            message.Write(rotation);
            Remote(SPAWN, fromId, toId, message, channel, target, subTarget);
        }

        [Remote(SPAWN)]
        protected virtual void SpawnRemote(ByteStream parameters, ushort fromId, ushort toId, RemoteStats stats)
        {
            throw new NotImplementedException("Override the SpawnRemote method!");
        }

        protected internal virtual void OnSerializeView(ByteStream parameters, bool isWriting, RemoteStats stats)
        {
            throw new NotImplementedException("Override the OnSerializeView(ByteStream, bool, RemoteStats) method!");
        }

        protected internal virtual void OnSerializeView(byte id, ByteStream parameters)
        {
            throw new NotImplementedException("Override the OnSerializeView(byte, ByteStream) method!");
        }

        internal void SentOnSyncBase(byte id, ByteStream parameters)
        {
            if (OnSyncBaseAuthority)
                NeutronNetwork.OnSyncBase(parameters, id, identity.id, this.id, identity.playerId, identity.sceneId, IsItFromTheServer, SYNC_BASE_MSG_TYPE, OnSyncBaseChannel, OnSyncBaseTarget, OnSyncBaseSubTarget, CacheMode.None);
            else { }
        }

        private IEnumerator SentOnSerializeView(WaitForSeconds seconds)
        {
            MessageType msgType = NeutronHelper.GetMessageTypeToOnSerialize(identity.objectType);
            while (OnSerializeViewAuthority)
            {
                if (OnSerializeViewAuthority)
                {
                    ByteStream message = ByteStream.Get();
                    OnSerializeView(message, true, default);
                    NeutronNetwork.OnSerializeView(message, identity.id, id, identity.playerId, identity.sceneId, IsItFromTheServer, msgType, OnSerializeViewChannel, OnSerializeViewTarget, OnSerializeViewSubTarget, CacheMode.None);
                }
                else
                    break;
                yield return seconds;
            }
        }
        #endregion
    }
}