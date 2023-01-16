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
using static Neutron.Core.Enums;

namespace Neutron.Core
{
    public class NeutronBehaviour : MonoBehaviour
    {
        private static readonly Dictionary<byte, Action<ByteStream, ushort, ushort, bool, RemoteStats>> remoteMethods = new(); // [rpc id, instanceId]
        // Start is called before the first frame update
        protected virtual void Awake() => GetRemoteAttributes();
        private void GetRemoteAttributes()
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
                                if (!remoteMethods.TryAdd(attr.id, remote))
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

        internal static Action<ByteStream, ushort, ushort, bool, RemoteStats> GetRpc(byte rpcId, bool isServer)
        {
            if (!remoteMethods.TryGetValue(rpcId, out Action<ByteStream, ushort, ushort, bool, RemoteStats> value))
                Logger.PrintWarning($"RPC does not exist! -> {rpcId} -> [IsServer]={isServer}");
            return value;
        }

        protected void Remote(byte id, ByteStream parameters, bool fromServer, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None)
        {
            NeutronNetwork.Remote(id, NeutronHelper.GetPlayerId(fromServer), NeutronHelper.GetPlayerId(fromServer), fromServer, parameters, channel, target, subTarget, cacheMode);
        }

        protected void Remote(byte id, ByteStream parameters, ushort fromId, ushort toId, bool fromServer, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None)
        {
            NeutronNetwork.Remote(id, fromId, toId, fromServer, parameters, channel, target, subTarget, cacheMode);
        }

        protected void Remote(byte id, ByteStream parameters, ushort toId, bool fromServer, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None)
        {
            NeutronNetwork.Remote(id, NeutronHelper.GetPlayerId(fromServer), toId, fromServer, parameters, channel, target, subTarget, cacheMode);
        }

        protected void Remote(byte id, ushort fromId, ByteStream parameters, bool fromServer, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None)
        {
            NeutronNetwork.Remote(id, fromId, NeutronHelper.GetPlayerId(fromServer), fromServer, parameters, channel, target, subTarget, cacheMode);
        }
    }
}
