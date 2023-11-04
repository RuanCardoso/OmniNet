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

using MessagePack;
using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static Omni.Core.Enums;

namespace Omni.Core
{
    [AddComponentMenu("")]
    [DefaultExecutionOrder(-0x62)]
    public class OmniObject : OmniDispatcher
    {
        protected const byte SPAWN_ID = 75;
        private const int SEPARATOR_HEIGHT = 1;
        private const int SEPARATOR = -(20 - SEPARATOR_HEIGHT);

        private MessageType REMOTE_MSG_TYPE = MessageType.None;
        private MessageType SYNC_BASE_MSG_TYPE = MessageType.None;
        private MessageType LOCAL_MESSAGE_MSG_TYPE = MessageType.None;

        [Header("Registration")]
        [SerializeField][ReadOnly][Required("Error: This instance must be registered in the OmniIdentity.")] internal OmniIdentity identity;
        [SerializeField][ReadOnly][HorizontalLine(SEPARATOR_HEIGHT, below: true)][Space(SEPARATOR)] internal byte id;

        internal byte Id => id;
        protected ByteStream Get => ByteStream.Get();
        protected OmniIdentity Identity => identity;
        protected internal bool IsItFromTheServer => identity.isItFromTheServer && identity.itIsRegistered;
        protected internal bool IsMine => !identity.isItFromTheServer && identity.playerId == OmniNetwork.Id && identity.itIsRegistered;
        protected internal bool IsServer => identity.isItFromTheServer && identity.itIsRegistered;
        protected internal bool IsClient => !identity.isItFromTheServer && identity.itIsRegistered;
        protected internal bool IsCustom => OnCustomAuthority() && identity.itIsRegistered;

        #region OnSerializeView
        protected virtual bool OnSerializeViewAuthority => IsMine;
        protected virtual Channel OnSerializeViewChannel => Channel.Unreliable;
        protected virtual Target OnSerializeViewTarget => Target.Others;
        protected virtual SubTarget OnSerializeViewSubTarget => SubTarget.None;
        protected virtual CacheMode OnSerializeViewCacheMode => CacheMode.None;
        #endregion

        private readonly Dictionary<int, Action<ReadOnlyMemory<byte>, ushort, bool, RemoteStats>> handlers = new();
        internal byte OnSyncBaseId = 0;
        internal Action<byte, ByteStream> OnSyncBase;

        internal void OnAwake()
        {
            if (identity == null)
            {
                Logger.PrintError("Error: This instance must be registered in the OmniIdentity.");
                return;
            }

            REMOTE_MSG_TYPE = OmniHelper.GetMessageTypeToRemote(identity.objectType);
            SYNC_BASE_MSG_TYPE = OmniHelper.GetMessageTypeToOnSyncBase(identity.objectType);
            LOCAL_MESSAGE_MSG_TYPE = OmniHelper.GetMessageTypeToLocalMessage(identity.objectType);
            // Reflection: Get all network methods and create a delegate for each one.
            // Reflection: All methods are stored in a dictionary to avoid invocation bottlenecks.
            GetRemoteAttributes();
        }

        private void GetRemoteAttributes()
        {
            #region Signature
            static MethodBase MethodSignature(ByteStream parameters, ushort fromId, ushort toId, RemoteStats stats) => MethodBase.GetCurrentMethod();
            MethodBase methodSignature = MethodSignature(default, default, default, default);
            ParameterInfo[] parametersSignature = methodSignature.GetParameters();
            int parametersCount = parametersSignature.Length;

            void ThrowErrorIfSignatureIsIncorret(byte id, string name)
            {
                Logger.PrintError($"Error: The signature of the method with ID: {id} and name: '{name}' in the type '{GetType().Name}' is incorrect.");
                Logger.PrintError("Correct Signature: ");
                Logger.PrintError($"private void {name}({string.Join(", ", parametersSignature.Select(param => $"{param.ParameterType} {param.Name}"))});");
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
                        if (parameters.Length != parametersCount)
                        {
                            ThrowErrorIfSignatureIsIncorret(attr.id, method.Name);
                        }
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

        internal Action<ReadOnlyMemory<byte>, ushort, bool, RemoteStats> GetHandler(byte id) => handlers.TryGetValue(id, out var handler) ? handler : null;
        protected void AddHandler<T>(Action<ReadOnlyMemory<byte>, ushort, bool, RemoteStats> handler) where T : IMessage, new()
        {
            T instance = new();
            if (!handlers.TryAdd(instance.Id, handler))
            {
                Logger.PrintError($"Error: Failed to add a handler for ID={instance.Id}.");
                Logger.PrintError("Please make sure the handler for this ID does not already exist.");
            }
            else
            {
                try
                {
                    MessagePackSerializer.Serialize(instance);
                }
                catch (Exception ex)
                {
                    ex = ex.InnerException;
                    Logger.PrintError($"Error: Failed to serialize {typeof(T).Name}: {ex.Message}");
                    Logger.PrintError("Hint: It may be necessary to generate Ahead-of-Time (AOT) code and register the type resolver.");
                }
            }
        }

        protected virtual bool OnCustomAuthority() => throw new NotImplementedException($"Override the {nameof(OnCustomAuthority)} method!");
        protected void OnSerializeView(WaitForSeconds seconds) => StartCoroutine(SentOnSerializeView(seconds));
        protected void GetCache(CacheType cacheType, byte cacheId, bool ownerCache = false, Channel channel = Channel.Unreliable)
        {
            OmniNetwork.GetCache(cacheType, ownerCache, cacheId, identity.playerId, IsItFromTheServer, channel);
        }

        private void Intern_Remote(byte id, byte sceneId, ushort fromId, ushort toId, ByteStream parameters, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode)
        {
            OmniNetwork.Remote(id, sceneId, identity.id, this.id, fromId, toId, IsItFromTheServer, parameters, REMOTE_MSG_TYPE, channel, target, subTarget, cacheMode);
        }

        internal void Intern_Message(ByteStream msg, byte id, ushort playerId, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode)
        {
            OmniNetwork.LocalMessage(msg, id, identity.id, this.id, playerId, identity.sceneId, IsItFromTheServer, LOCAL_MESSAGE_MSG_TYPE, channel, target, subTarget, cacheMode);
        }

        protected void Remote(byte id, ByteStream parameters, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None) => Intern_Remote(id, identity.sceneId, identity.playerId, identity.playerId, parameters, channel, target, subTarget, cacheMode);
        protected void Remote(byte id, ByteStream parameters, OmniIdentity fromIdentity, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None) => Intern_Remote(id, identity.sceneId, fromIdentity.playerId, identity.playerId, parameters, channel, target, subTarget, cacheMode);
        protected void Remote(byte id, ByteStream parameters, OmniIdentity fromIdentity, OmniIdentity toIdentity, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None) => Intern_Remote(id, toIdentity.sceneId, fromIdentity.playerId, toIdentity.playerId, parameters, channel, target, subTarget, cacheMode);
        protected void Remote(byte id, OmniIdentity toIdentity, ByteStream parameters, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None) => Intern_Remote(id, toIdentity.sceneId, identity.playerId, toIdentity.playerId, parameters, channel, target, subTarget, cacheMode);
        protected void Remote(byte id, ByteStream parameters, ushort toId, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None) => Intern_Remote(id, identity.sceneId, identity.playerId, toId, parameters, channel, target, subTarget, cacheMode);
        protected void Remote(byte id, ByteStream parameters, byte sceneId, ushort toId, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None) => Intern_Remote(id, sceneId, identity.playerId, toId, parameters, channel, target, subTarget, cacheMode);
        protected void Remote(byte id, ushort fromId, ByteStream parameters, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None) => Intern_Remote(id, identity.sceneId, fromId, identity.playerId, parameters, channel, target, subTarget, cacheMode);
        protected void Remote(byte id, ushort fromId, ushort toId, ByteStream parameters, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None) => Intern_Remote(id, identity.sceneId, fromId, toId, parameters, channel, target, subTarget, cacheMode);
        protected void Remote(byte id, byte sceneId, ushort fromId, ushort toId, ByteStream parameters, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None) => Intern_Remote(id, sceneId, fromId, toId, parameters, channel, target, subTarget, cacheMode);

        #region Intern Network Methods
        protected void SpawnRemote(Vector3 position, Quaternion rotation, Action<ByteStream> parameters = null, Channel channel = Channel.Unreliable, Target target = Target.All, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None)
        {
            ByteStream message = ByteStream.Get();
            message.Write(position);
            message.Write(rotation);
            parameters?.Invoke(message);
            Remote(SPAWN_ID, message, channel, target, subTarget, cacheMode);
        }

        protected void SpawnRemote(ushort toId, Vector3 position, Quaternion rotation, Action<ByteStream> parameters = null, Channel channel = Channel.Unreliable, Target target = Target.All, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None)
        {
            ByteStream message = ByteStream.Get();
            message.Write(position);
            message.Write(rotation);
            parameters?.Invoke(message);
            Remote(SPAWN_ID, message, toId, channel, target, subTarget, cacheMode);
        }

        protected void SpawnRemote(ushort fromId, ushort toId, Vector3 position, Quaternion rotation, Action<ByteStream> parameters = null, Channel channel = Channel.Unreliable, Target target = Target.All, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None)
        {
            ByteStream message = ByteStream.Get();
            message.Write(position);
            message.Write(rotation);
            parameters?.Invoke(message);
            Remote(SPAWN_ID, fromId, toId, message, channel, target, subTarget, cacheMode);
        }

        [Remote(SPAWN_ID)]
        internal void SpawnRemote(ByteStream parameters, ushort fromId, ushort toId, RemoteStats stats)
        {
            Vector3 position = parameters.ReadVector3();
            Quaternion rotation = parameters.ReadQuaternion();
            OmniIdentity identity = OnSpawnedObject(position, rotation, parameters, fromId, toId, stats);
            if (identity != null)
            {
                if (identity.objectType == ObjectType.Dynamic)
                {
                    throw new NotImplementedException("Error: Dynamic objects are not supported.");
                }
                else
                {
                    identity.Register(IsServer, fromId);
                }
            }
        }

        protected virtual OmniIdentity OnSpawnedObject(Vector3 position, Quaternion rotation, ByteStream parameters, ushort fromId, ushort toId, RemoteStats stats)
        {
            throw new NotImplementedException($"Override the {nameof(OnSpawnedObject)} method!");
        }

        protected internal virtual void OnSerializeView(ByteStream parameters, bool isWriting, RemoteStats stats)
        {
            throw new NotImplementedException($"Override the {nameof(OnSerializeView)} method!");
        }

        internal void SentOnSyncBase(byte id, ByteStream parameters, bool hasAuthority, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode)
        {
            if (hasAuthority)
            {
                OmniNetwork.OnSyncBase(parameters, id, identity.id, this.id, identity.playerId, identity.sceneId, IsItFromTheServer, SYNC_BASE_MSG_TYPE, channel, target, subTarget, cacheMode);
            }
        }

        private IEnumerator SentOnSerializeView(WaitForSeconds seconds)
        {
            MessageType msgType = OmniHelper.GetMessageTypeToOnSerialize(identity.objectType);
            while (OnSerializeViewAuthority)
            {
                if (OnSerializeViewAuthority)
                {
                    ByteStream message = ByteStream.Get();
                    OnSerializeView(message, true, default);
                    OmniNetwork.OnSerializeView(message, identity.id, id, identity.playerId, identity.sceneId, IsItFromTheServer, msgType, OnSerializeViewChannel, OnSerializeViewTarget, OnSerializeViewSubTarget, OnSerializeViewCacheMode);
                }
                else
                    break;
                yield return seconds;
            }
        }
        #endregion
    }
}