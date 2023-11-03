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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static Omni.Core.Enums;

namespace Omni.Core
{
    public class OmniBehaviour : MonoBehaviour
    {
        protected const byte GLOBAL_SPAWN_ID = 255;
        private static readonly Dictionary<(byte, byte), Action<ByteStream, ushort, ushort, bool, RemoteStats>> remoteMethods = new(); // [rpc id, instance Id]

        protected virtual byte Id => 0;
        protected ByteStream Get => ByteStream.Get();

        // Start is called before the first frame update
        protected virtual void Awake() => GetRemoteAttributes();
        private void GetRemoteAttributes()
        {
            if (Id == 0)
                Logger.PrintError($"Override {nameof(Id)} property! [{GetType().Name} -> {nameof(OmniBehaviour)}]");
            else
            {
                #region Signature
                static MethodBase MethodSignature(ByteStream parameters, ushort fromId, ushort toId, bool isServer, RemoteStats stats) => MethodBase.GetCurrentMethod();
                MethodBase methodSignature = MethodSignature(default, default, default, default, default);
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
                                    var remote = method.CreateDelegate(typeof(Action<ByteStream, ushort, ushort, bool, RemoteStats>), this) as Action<ByteStream, ushort, ushort, bool, RemoteStats>;
                                    if (!remoteMethods.TryAdd((attr.id, Id), remote))
                                        Logger.PrintError($"The RPC {attr.id} is already registered.");
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
        }

        internal static Action<ByteStream, ushort, ushort, bool, RemoteStats> GetRpc(byte rpcId, byte instanceId, bool isServer)
        {
            if (!remoteMethods.TryGetValue((rpcId, instanceId), out Action<ByteStream, ushort, ushort, bool, RemoteStats> value))
                Logger.PrintWarning($"RPC does not exist! -> {rpcId} -> [IsServer]={isServer}");
            return value;
        }

        protected void Remote(byte id, ByteStream parameters, bool fromServer, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None)
        {
            OmniNetwork.Remote(id, Id, OmniHelper.GetPlayerId(fromServer), OmniHelper.GetPlayerId(fromServer), fromServer, parameters, channel, target, subTarget, cacheMode);
        }

        protected void Remote(byte id, ByteStream parameters, ushort fromId, ushort toId, bool fromServer, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None)
        {
            OmniNetwork.Remote(id, Id, fromId, toId, fromServer, parameters, channel, target, subTarget, cacheMode);
        }

        protected void Remote(byte id, ByteStream parameters, ushort toId, bool fromServer, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None)
        {
            OmniNetwork.Remote(id, Id, OmniHelper.GetPlayerId(fromServer), toId, fromServer, parameters, channel, target, subTarget, cacheMode);
        }

        protected void Remote(byte id, ushort fromId, ByteStream parameters, bool fromServer, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None)
        {
            OmniNetwork.Remote(id, Id, fromId, OmniHelper.GetPlayerId(fromServer), fromServer, parameters, channel, target, subTarget, cacheMode);
        }

        protected void SpawnRemote(Vector3 position, Quaternion rotation, Action<ByteStream> parameters = null, bool fromServer = false, Channel channel = Channel.Unreliable, Target target = Target.All, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None)
        {
            ByteStream message = ByteStream.Get();
            message.Write(position);
            message.Write(rotation);
            parameters?.Invoke(message);
            Remote(GLOBAL_SPAWN_ID, message, fromServer, channel, target, subTarget, cacheMode);
        }

        protected void SpawnRemote(ushort toId, Vector3 position, Quaternion rotation, Action<ByteStream> parameters = null, bool fromServer = false, Channel channel = Channel.Unreliable, Target target = Target.All, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None)
        {
            ByteStream message = ByteStream.Get();
            message.Write(position);
            message.Write(rotation);
            parameters?.Invoke(message);
            Remote(GLOBAL_SPAWN_ID, message, toId, fromServer, channel, target, subTarget, cacheMode);
        }

        protected void SpawnRemote(ushort fromId, ushort toId, Vector3 position, Quaternion rotation, Action<ByteStream> parameters = null, bool fromServer = false, Channel channel = Channel.Unreliable, Target target = Target.All, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None)
        {
            ByteStream message = ByteStream.Get();
            message.Write(position);
            message.Write(rotation);
            parameters?.Invoke(message);
            Remote(GLOBAL_SPAWN_ID, message, fromId, toId, fromServer, channel, target, subTarget, cacheMode);
        }

        [Remote(GLOBAL_SPAWN_ID)]
        internal void SpawnRemote(ByteStream parameters, ushort fromId, ushort toId, bool isServer, RemoteStats stats)
        {
            Vector3 position = parameters.ReadVector3();
            Quaternion rotation = parameters.ReadQuaternion();
            OmniIdentity identity = OnSpawnedObject(position, rotation, parameters, fromId, toId, isServer, stats);
            if (identity.objectType == ObjectType.Dynamic)
                throw new NotImplementedException("Dynamic object not supported Id!");
            else identity.Register(isServer, fromId);
        }

        protected virtual OmniIdentity OnSpawnedObject(Vector3 position, Quaternion rotation, ByteStream parameters, ushort fromId, ushort toId, bool isServer, RemoteStats stats)
        {
            throw new NotImplementedException($"Override the {nameof(OnSpawnedObject)} method!");
        }
    }
}
