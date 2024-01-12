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

		protected byte Id => id;
		protected ushort PlayerId => identity.playerId;
		protected OmniIdentity Identity => identity;
		protected DataIOHandler Get => DataIOHandler.Get();
		protected internal bool IsItFromTheServer => identity.isItFromTheServer && identity.itIsRegistered;
		protected internal bool IsMine => !identity.isItFromTheServer && identity.playerId == OmniNetwork.Id && identity.itIsRegistered;
		protected internal bool IsServer => identity.isItFromTheServer && identity.itIsRegistered;
		protected internal bool IsClient => !identity.isItFromTheServer && identity.itIsRegistered;
		protected internal bool IsCustom => OnCustomAuthority() && identity.itIsRegistered;

		#region OnSerializeView
		protected virtual bool OnSerializeViewAuthority => IsMine;
		protected virtual DataDeliveryMode OnSerializeViewChannel => DataDeliveryMode.Unsecured;
		protected virtual DataTarget OnSerializeViewTarget => DataTarget.BroadcastExcludingSelf;
		protected virtual DataProcessingOption OnSerializeViewSubTarget => DataProcessingOption.DoNotProcessOnServer;
		protected virtual DataCachingOption OnSerializeViewCacheMode => DataCachingOption.None;
		#endregion

		private readonly Dictionary<int, Action<ReadOnlyMemory<byte>, ushort, bool, RemoteStats>> handlers = new();
		internal byte OnSyncBaseId = 0;
		internal Action<byte, DataIOHandler> OnSyncBase;

		internal void OnAwake()
		{
			if (identity == null)
			{
				OmniLogger.PrintError("Error: This instance must be registered in the OmniIdentity.");
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
			static MethodBase MethodSignature(DataIOHandler IOHandler, ushort fromId, ushort toId, RemoteStats stats) => MethodBase.GetCurrentMethod();
			MethodBase methodSignature = MethodSignature(default, default, default, default);
			ParameterInfo[] parametersSignature = methodSignature.GetParameters();
			int parametersCount = parametersSignature.Length;

			void ThrowErrorIfSignatureIsIncorret(byte id, string name)
			{
				OmniLogger.PrintError($"Error: The signature of the method with ID: {id} and name: '{name}' in the type '{GetType().Name}' is incorrect.");
				OmniLogger.PrintError("Correct Signature: ");
				OmniLogger.PrintError($"private void {name}({string.Join(", ", parametersSignature.Select(param => $"{param.ParameterType} {param.Name}"))});");
			}
			#endregion

			Type typeOf = GetType().BaseType;
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
								var remote = method.CreateDelegate(typeof(Action<DataIOHandler, ushort, ushort, RemoteStats>), this) as Action<DataIOHandler, ushort, ushort, RemoteStats>;
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
				OmniLogger.PrintError($"Error: Failed to add a handler for ID={instance.Id}.");
				OmniLogger.PrintError("Please make sure the handler for this ID does not already exist.");
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
					OmniLogger.PrintError($"Error: Failed to serialize {typeof(T).Name}: {ex.Message}");
					OmniLogger.PrintError("Hint: It may be necessary to generate Ahead-of-Time (AOT) code and register the type resolver.");
				}
			}
		}

		protected virtual bool OnCustomAuthority() => throw new NotImplementedException($"Override the {nameof(OnCustomAuthority)} method!");
		protected void OnSerializeView(WaitForSeconds seconds) => StartCoroutine(SentOnSerializeView(seconds));
		protected void GetCache(DataStorageType cacheType, byte cacheId, bool ownerCache = false, DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured)
		{
			OmniNetwork.GetCache(cacheType, ownerCache, cacheId, identity.playerId, IsItFromTheServer, deliveryMode);
		}

		private void Intern_Remote(byte id, byte sceneId, ushort fromId, ushort toId, DataIOHandler IOHandler, DataDeliveryMode deliveryMode, DataTarget target, DataProcessingOption processingOption, DataCachingOption cachingOption)
		{
			OmniNetwork.Remote(id, sceneId, identity.id, this.id, fromId, toId, IsItFromTheServer, IOHandler, REMOTE_MSG_TYPE, deliveryMode, target, processingOption, cachingOption);
		}

		internal void Intern_Message(DataIOHandler IOHandler, byte id, ushort playerId, DataDeliveryMode deliveryMode, DataTarget target, DataProcessingOption processingOption, DataCachingOption cachingOption)
		{
			OmniNetwork.LocalMessage(IOHandler, id, identity.id, this.id, playerId, identity.sceneId, IsItFromTheServer, LOCAL_MESSAGE_MSG_TYPE, deliveryMode, target, processingOption, cachingOption);
		}

		protected void Remote(byte id, DataIOHandler IOHandler, DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured, DataTarget target = DataTarget.Self, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None) => Intern_Remote(id, identity.sceneId, identity.playerId, identity.playerId, IOHandler, deliveryMode, target, processingOption, cachingOption);
		protected void Remote(byte id, DataIOHandler IOHandler, OmniIdentity fromIdentity, DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured, DataTarget target = DataTarget.Self, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None) => Intern_Remote(id, identity.sceneId, fromIdentity.playerId, identity.playerId, IOHandler, deliveryMode, target, processingOption, cachingOption);
		protected void Remote(byte id, DataIOHandler IOHandler, OmniIdentity fromIdentity, OmniIdentity toIdentity, DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured, DataTarget target = DataTarget.Self, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None) => Intern_Remote(id, toIdentity.sceneId, fromIdentity.playerId, toIdentity.playerId, IOHandler, deliveryMode, target, processingOption, cachingOption);
		protected void Remote(byte id, OmniIdentity toIdentity, DataIOHandler IOHandler, DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured, DataTarget target = DataTarget.Self, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None) => Intern_Remote(id, toIdentity.sceneId, identity.playerId, toIdentity.playerId, IOHandler, deliveryMode, target, processingOption, cachingOption);
		protected void Remote(byte id, DataIOHandler IOHandler, ushort toId, DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured, DataTarget target = DataTarget.Self, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None) => Intern_Remote(id, identity.sceneId, identity.playerId, toId, IOHandler, deliveryMode, target, processingOption, cachingOption);
		protected void Remote(byte id, DataIOHandler IOHandler, byte sceneId, ushort toId, DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured, DataTarget target = DataTarget.Self, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None) => Intern_Remote(id, sceneId, identity.playerId, toId, IOHandler, deliveryMode, target, processingOption, cachingOption);
		protected void Remote(byte id, ushort fromId, DataIOHandler IOHandler, DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured, DataTarget target = DataTarget.Self, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None) => Intern_Remote(id, identity.sceneId, fromId, identity.playerId, IOHandler, deliveryMode, target, processingOption, cachingOption);
		protected void Remote(byte id, ushort fromId, ushort toId, DataIOHandler IOHandler, DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured, DataTarget target = DataTarget.Self, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None) => Intern_Remote(id, identity.sceneId, fromId, toId, IOHandler, deliveryMode, target, processingOption, cachingOption);
		protected void Remote(byte id, byte sceneId, ushort fromId, ushort toId, DataIOHandler IOHandler, DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured, DataTarget target = DataTarget.Self, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None) => Intern_Remote(id, sceneId, fromId, toId, IOHandler, deliveryMode, target, processingOption, cachingOption);

		#region Intern Network Methods
		protected void SpawnRemote(Vector3 position, Quaternion rotation, Action<DataIOHandler> _IOHandler_ = null, DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured, DataTarget target = DataTarget.Broadcast, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None)
		{
			DataIOHandler IOHandler = DataIOHandler.Get();
			IOHandler.Write(position);
			IOHandler.Write(rotation);
			_IOHandler_?.Invoke(IOHandler);
			Remote(SPAWN_ID, IOHandler, deliveryMode, target, processingOption, cachingOption);
		}

		protected void SpawnRemote(ushort toId, Vector3 position, Quaternion rotation, Action<DataIOHandler> _IOHandler_ = null, DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured, DataTarget target = DataTarget.Broadcast, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None)
		{
			DataIOHandler IOHandler = DataIOHandler.Get();
			IOHandler.Write(position);
			IOHandler.Write(rotation);
			_IOHandler_?.Invoke(IOHandler);
			Remote(SPAWN_ID, IOHandler, toId, deliveryMode, target, processingOption, cachingOption);
		}

		protected void SpawnRemote(ushort fromId, ushort toId, Vector3 position, Quaternion rotation, Action<DataIOHandler> _IOHandler_ = null, DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured, DataTarget target = DataTarget.Broadcast, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None)
		{
			DataIOHandler IOHandler = DataIOHandler.Get();
			IOHandler.Write(position);
			IOHandler.Write(rotation);
			_IOHandler_?.Invoke(IOHandler);
			Remote(SPAWN_ID, fromId, toId, IOHandler, deliveryMode, target, processingOption, cachingOption);
		}

		[Remote(SPAWN_ID)]
		internal void SpawnRemote(DataIOHandler IOHandler, ushort fromId, ushort toId, RemoteStats stats)
		{
			Vector3 position = IOHandler.ReadVector3();
			Quaternion rotation = IOHandler.ReadQuaternion();
			OmniIdentity identity = OnSpawnedObject(position, rotation, IOHandler, fromId, toId, stats);
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

		protected virtual OmniIdentity OnSpawnedObject(Vector3 position, Quaternion rotation, DataIOHandler IOHandler, ushort fromId, ushort toId, RemoteStats stats)
		{
			throw new NotImplementedException($"Override the {nameof(OnSpawnedObject)} method!");
		}

		protected internal virtual void OnSerializeView(DataIOHandler IOHandler, bool isWriting, RemoteStats stats)
		{
			throw new NotImplementedException($"Override the {nameof(OnSerializeView)} method!");
		}

		internal void SentOnSyncBase(byte id, DataIOHandler IOHandler, bool hasAuthority, DataDeliveryMode deliveryMode, DataTarget target, DataProcessingOption processingOption, DataCachingOption cachingOption)
		{
			if (hasAuthority)
			{
				OmniNetwork.OnSyncBase(IOHandler, id, identity.id, this.id, identity.playerId, identity.sceneId, IsItFromTheServer, SYNC_BASE_MSG_TYPE, deliveryMode, target, processingOption, cachingOption);
			}
		}

		private IEnumerator SentOnSerializeView(WaitForSeconds seconds)
		{
			MessageType msgType = OmniHelper.GetMessageTypeToOnSerialize(identity.objectType);
			while (OnSerializeViewAuthority)
			{
				if (OnSerializeViewAuthority)
				{
					DataIOHandler IOHandler = DataIOHandler.Get();
					OnSerializeView(IOHandler, true, default);
					OmniNetwork.OnSerializeView(IOHandler, identity.id, id, identity.playerId, identity.sceneId, IsItFromTheServer, msgType, OnSerializeViewChannel, OnSerializeViewTarget, OnSerializeViewSubTarget, OnSerializeViewCacheMode);
				}
				else
					break;
				yield return seconds;
			}
		}
		#endregion
	}
}