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
using static Omni.Core.Enums;

namespace Omni.Core
{
    public class OmniBehaviour : MonoBehaviour
    {
        protected const byte GLOBAL_SPAWN_ID = 255;
        protected virtual byte Id => 0;

        /// <summary>
        /// Returns the <see cref="DataIOHandler"/> instance from the pool.
        /// </summary>
        protected DataIOHandler Get => DataIOHandler.Get();

        // Start is called before the first frame update
        protected virtual void Awake() => GetRemoteAttributes();
        /// <summary>
        /// Get all methods with the <see cref="RemoteAttribute"/> attribute and add them to the <see cref="Dictionaries.RPCMethods"/> dictionary.<br/>
        /// For best performance the method marked is converted to a delegate and added to the dictionary to prevent the high cost of reflection.
        /// Call via delegates are as fast as calling a method directly.
        /// </summary>
        private void GetRemoteAttributes()
        {
            if (Id == 0)
            {
                OmniLogger.PrintError($"Override {nameof(Id)} property! [{GetType().Name} -> {nameof(OmniBehaviour)}]");
            }
            else
            {
                #region Signature
                static MethodBase MethodSignature(DataIOHandler IOHandler, ushort fromId, ushort toId, bool isServer, RemoteStats stats) => MethodBase.GetCurrentMethod();
                MethodBase methodSignature = MethodSignature(default, default, default, default, default);
                ParameterInfo[] parametersSignature = methodSignature.GetParameters();
                int parametersCount = parametersSignature.Length;

                void ThrowErrorIfSignatureIsIncorret(byte id, string name)
                {
                    OmniLogger.PrintError($"Error: The signature of the method with ID: {id} and name: '{name}' in the type '{GetType().Name}' is incorrect.");
                    OmniLogger.PrintError("Correct Signature: ");
                    OmniLogger.PrintError($"private void {name}({string.Join(", ", parametersSignature.Select(param => $"{param.ParameterType} {param.Name}"))});");
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
                                    var remote = method.CreateDelegate(typeof(Action<DataIOHandler, ushort, ushort, bool, RemoteStats>), this) as Action<DataIOHandler, ushort, ushort, bool, RemoteStats>;
                                    if (!Dictionaries.RPCMethods.TryAdd((attr.id, Id), remote))
                                    {
                                        OmniLogger.PrintError($"Error: The RPC ID {attr.id} is already registered for this script instance. Solution: Ensure that each RPC ID is unique within the script instance.");
                                    }
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

        internal static Action<DataIOHandler, ushort, ushort, bool, RemoteStats> GetRpc(byte rpcId, byte instanceId, bool isServer)
        {
            if (!Dictionaries.RPCMethods.TryGetValue((rpcId, instanceId), out Action<DataIOHandler, ushort, ushort, bool, RemoteStats> value))
            {
                OmniLogger.PrintError($"Global Remote does not exist! RPC ID: {rpcId}, Instance ID: {instanceId}, IsServer: {isServer}");
            }

            return value;
        }

        protected void Remote(byte id, DataIOHandler IOHandler, bool fromServer, DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured, DataTarget target = DataTarget.Self, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None)
        {
            OmniNetwork.Remote(id, Id, OmniHelper.GetPlayerId(fromServer), OmniHelper.GetPlayerId(fromServer), fromServer, IOHandler, deliveryMode, target, processingOption, cachingOption);
        }

        protected void Remote(byte id, DataIOHandler IOHandler, ushort fromId, ushort toId, bool fromServer, DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured, DataTarget target = DataTarget.Self, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None)
        {
            OmniNetwork.Remote(id, Id, fromId, toId, fromServer, IOHandler, deliveryMode, target, processingOption, cachingOption);
        }

        protected void Remote(byte id, DataIOHandler IOHandler, ushort toId, bool fromServer, DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured, DataTarget target = DataTarget.Self, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None)
        {
            OmniNetwork.Remote(id, Id, OmniHelper.GetPlayerId(fromServer), toId, fromServer, IOHandler, deliveryMode, target, processingOption, cachingOption);
        }

        protected void Remote(byte id, ushort fromId, DataIOHandler IOHandler, bool fromServer, DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured, DataTarget target = DataTarget.Self, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None)
        {
            OmniNetwork.Remote(id, Id, fromId, OmniHelper.GetPlayerId(fromServer), fromServer, IOHandler, deliveryMode, target, processingOption, cachingOption);
        }

        protected void SpawnRemote(Vector3 position, Quaternion rotation, Action<DataIOHandler> _IOHandler_ = null, bool fromServer = false, DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured, DataTarget target = DataTarget.Broadcast, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None)
        {
            DataIOHandler IOHandler = DataIOHandler.Get();
            IOHandler.Write(position);
            IOHandler.Write(rotation);
            _IOHandler_?.Invoke(IOHandler);
            Remote(GLOBAL_SPAWN_ID, IOHandler, fromServer, deliveryMode, target, processingOption, cachingOption);
        }

        protected void SpawnRemote(ushort toId, Vector3 position, Quaternion rotation, Action<DataIOHandler> _IOHandler_ = null, bool fromServer = false, DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured, DataTarget target = DataTarget.Broadcast, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None)
        {
            DataIOHandler IOHandler = DataIOHandler.Get();
            IOHandler.Write(position);
            IOHandler.Write(rotation);
            _IOHandler_?.Invoke(IOHandler);
            Remote(GLOBAL_SPAWN_ID, IOHandler, toId, fromServer, deliveryMode, target, processingOption, cachingOption);
        }

        protected void SpawnRemote(ushort fromId, ushort toId, Vector3 position, Quaternion rotation, Action<DataIOHandler> _IOHandler_ = null, bool fromServer = false, DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured, DataTarget target = DataTarget.Broadcast, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None)
        {
            DataIOHandler IOHandler = DataIOHandler.Get();
            IOHandler.Write(position);
            IOHandler.Write(rotation);
            _IOHandler_?.Invoke(IOHandler);
            Remote(GLOBAL_SPAWN_ID, IOHandler, fromId, toId, fromServer, deliveryMode, target, processingOption, cachingOption);
        }

        [Remote(GLOBAL_SPAWN_ID)]
        internal void SpawnRemote(DataIOHandler IOHandler, ushort fromId, ushort toId, bool isServer, RemoteStats stats)
        {
            Vector3 position = IOHandler.ReadVector3();
            Quaternion rotation = IOHandler.ReadQuaternion();
            OmniIdentity omniIdentity = OnSpawnedObject(position, rotation, IOHandler, fromId, toId, isServer, stats);
            if (omniIdentity != null)
            {
                if (omniIdentity.objectType == ObjectType.Dynamic)
                {
                    // Falta dar suporte a objetos din�micos.... Encontrar uma maneira de atribuir um Id....
                    throw new NotImplementedException("Error: Creating dynamic objects is not supported in this context. Please use a different object type.");
                }
                else
                {
                    omniIdentity.Register(isServer, fromId);
                }
            }
            else
            {
                OmniLogger.PrintError("Error: Failed to create an OmniIdentity for the spawned object.");
            }
        }

        protected virtual OmniIdentity OnSpawnedObject(Vector3 position, Quaternion rotation, DataIOHandler IOHandler, ushort fromId, ushort toId, bool isServer, RemoteStats stats)
        {
            throw new NotImplementedException($"Override the {nameof(OnSpawnedObject)} method!");
        }
    }
}
