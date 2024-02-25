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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Omni.Core
{
	[DefaultExecutionOrder(-300)]
	public abstract class NetworkBehaviour : RealtimeTickBasedSystem
	{
		private static HashSet<ValueTuple<int, int>> RegisteredBehaviours { get; } = new HashSet<ValueTuple<int, int>>();
		internal Dictionary<int, Action<IDataReader, NetworkPeer>> RemoteFuncs { get; } = new Dictionary<int, Action<IDataReader, NetworkPeer>>(); // RPC Id, RPC Func

		[SerializeField][Required("It cannot be null!")] private NetworkIdentity m_Identity;
		[SerializeField][ReadOnly] private byte m_Id;

		public byte Id => m_Id;
		public NetworkIdentity Identity => m_Identity;
		public bool IsMine => OmniNetwork.Communicator.IsMine(m_Identity.OwnerId);
		public bool IsServer => m_Identity.IsServer;
		public bool IsClient => m_Identity.IsClient;

		public virtual void Awake()
		{
			if (m_Identity == null)
			{
				OmniLogger.PrintError($"Error: Identity not assigned! -> {gameObject.name} : {transform.root.name}");
				Destroy(this);
				return;
			}

			if (m_Identity.NetworkBehaviours.TryAdd(m_Id, this))
			{
				GetRemoteAtributes();
			}
			else
			{
				OmniLogger.PrintError($"Error: Duplicated ID '{m_Id}'. Ensure unique IDs for each instance of Network Behaviour");
			}

#if UNITY_EDITOR
			if (RegisteredBehaviours.Add(ValueTuple.Create(m_Identity.Id, m_Id)))
			{
				OnNetworkEventsRegister();
			}
#endif
		}

		public override void Start()
		{
			base.Start();
			StartCoroutine(Internal_OnNetworkStart());
		}

		public virtual void OnNetworkEventsRegister()
		{

		}

		public abstract void OnNetworkStart();
		private IEnumerator Internal_OnNetworkStart()
		{
			yield return new WaitUntil(() => m_Identity.IsServerSimulation ? IsServer : IsClient);
			OnNetworkStart();
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (Application.isPlaying)
			{
				return;
			}

			if (m_Identity != null)
			{
				NetworkBehaviour[] behaviours = m_Identity.GetComponentsInChildren<NetworkBehaviour>(true);
				if (m_Id == 0)
				{
					if (!(behaviours.Length > (byte.MaxValue - 1)))
					{
						for (int i = 0; i < behaviours.Length; i++)
						{
							if (behaviours[i] == this)
							{
								byte instanceId = (byte)(i + 1);
								if (m_Id != instanceId)
								{
									m_Id = instanceId;
									IsDuplicated(behaviours);
								}
							}
						}
					}
					else
					{
						OmniLogger.PrintError("An identity can only have 255 network behaviors.");
						Destroy(gameObject);
					}
				}
				else
				{
					IsDuplicated(behaviours);
				}
			}

			void IsDuplicated(NetworkBehaviour[] behaviours)
			{
				int count = behaviours.Count(x => x.m_Id == m_Id);
				if (count > 1) // duplicated id...
				{
					for (int i = 0; i < behaviours.Length; i++)
					{
						NetworkBehaviour behaviour = behaviours[i];
						behaviour.m_Id = (byte)(i + 1);
					}
				}
			}
		}

		private void Reset()
		{
			OnValidate();
		}
#endif

		protected NetworkPeer GetPeerByid(int peerId)
		{
			return OmniNetwork.Communicator.GetPeerById(peerId);
		}

		protected Dictionary<int, NetworkPeer> GetPeers()
		{
			return OmniNetwork.Communicator.GetPeers();
		}

		protected IDataWriter GetWriter() => NetworkCommunicator.DataWriterPool.Get();
		protected IDataReader GetReader() => NetworkCommunicator.DataReaderPool.Get();

		protected void Release(IDataWriter writer) => NetworkCommunicator.DataWriterPool.Release(writer);
		protected void Release(IDataReader reader) => NetworkCommunicator.DataReaderPool.Release(reader);

		// Sync NetVar with Roslyn Generators (:
		// Roslyn methods!
		protected virtual void OnCustomSerialize(byte id, IDataWriter writer, int argIndex = 0)
		{
			throw new NotImplementedException("OnCustomSerialize must be overridden in a derived class.");
		}

		protected virtual void OnCustomDeserialize(byte id, IDataReader reader, int argIndex = 0)
		{
			throw new NotImplementedException("OnCustomDeserialize must be overridden in a derived class.");
		}

		protected virtual void OnPropertyChanged(byte id) { }
		protected virtual void OnClientDefaultSettings(byte id, out DataDeliveryMode deliveryMode, out byte sequenceChannel)
		{
			// Default
			deliveryMode = DataDeliveryMode.ReliableOrdered;
			sequenceChannel = 0;
		}

		protected virtual void OnServerDefaultSettings(byte id, IDataWriter writer)
		{
			// Default
			NetVar(writer, DataDeliveryMode.ReliableOrdered, DataTarget.Broadcast, id, 0);
		}
#pragma warning disable CA1822
#pragma warning disable IDE1006
		internal void Internal___2205032023(byte id, IDataReader dataReader)
		{
			___2205032023(id, dataReader); // Deserialize
			OnPropertyChanged(id);
		}
		// Deserialize method implemented by source generator!
		protected virtual void ___2205032023(byte id, IDataReader dataReader) { throw new NotImplementedException("___2205032023 must be overridden in a derived class."); }
		protected void ___2205032024(IDataWriter writer, byte id)
#pragma warning restore IDE1006
#pragma warning restore CA1822
		{
			if (IsClient)
			{
				OnPropertyChanged(id);
				OnClientDefaultSettings(id, out DataDeliveryMode deliveryMode, out byte sequenceChannel);
				OmniNetwork.Communicator.SyncVariable(writer, deliveryMode, m_Identity.Id, m_Id, id, sequenceChannel);
			}
			else
			{
				OnPropertyChanged(id);
				OnServerDefaultSettings(id, writer);
			}
		}

		private void GetRemoteAtributes()
		{
			Type type = GetType();
			MethodInfo[] methodInfos = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
			for (int i = 0; i < methodInfos.Length; i++)
			{
				MethodInfo method = methodInfos[i];
				IEnumerable<RemoteAttribute> attributes = method.GetCustomAttributes<RemoteAttribute>();
				foreach (RemoteAttribute attr in attributes)
				{
					if (attr != null)
					{
						Action<IDataReader, NetworkPeer> func = (_, _) => { };
						try
						{
							func = (Action<IDataReader, NetworkPeer>)method.CreateDelegate(typeof(Action<IDataReader, NetworkPeer>), this);
							if (!RemoteFuncs.TryAdd(attr.Id, func))
							{
								OmniLogger.PrintError($"Error: Duplicated remote ID '{attr.Id}'. Ensure unique IDs for each method with the Remote attribute.");
							}
						}
						catch (TargetParameterCountException ex)
						{
							var expectedArguments = string.Join(", ", func.Method.GetParameters().Select(param => param.ParameterType.Name));
							OmniLogger.PrintError($"Error: Failed to create delegate for method with Remote attribute. {ex.Message} -> Id: {attr.Id} | expected arguments: {expectedArguments}");
						}
					}
				}
			}
		}

		protected void NetVar(IDataWriter writer, DataDeliveryMode dataDeliveryMode, int peerId, byte netVarId, byte sequenceChannel = 0)
		{
			OmniNetwork.Communicator.SyncVariable(writer, dataDeliveryMode, peerId, m_Identity.Id, m_Id, netVarId, sequenceChannel);
		}

		protected void NetVar(IDataWriter writer, DataDeliveryMode dataDeliveryMode, DataTarget dataTarget, byte netVarId, byte sequenceChannel = 0)
		{
			switch (dataTarget)
			{
				case DataTarget.Self:
					NetVar(writer, dataDeliveryMode, m_Identity.OwnerId, netVarId, sequenceChannel);
					break;
				case DataTarget.Broadcast:
					{
						foreach ((int _, NetworkPeer peer) in OmniNetwork.Communicator.GetPeers())
						{
							NetVar(writer, dataDeliveryMode, peer.Id, netVarId, sequenceChannel);
						}
					}
					break;
				case DataTarget.BroadcastExcludingSelf:
					{
						foreach ((int _, NetworkPeer peer) in OmniNetwork.Communicator.GetPeers())
						{
							if (peer.Id == m_Identity.OwnerId)
								continue;

							NetVar(writer, dataDeliveryMode, peer.Id, netVarId, sequenceChannel);
						}
					}
					break;
				case DataTarget.Server:
					throw new NotSupportedException("DataTarget.Server is not supported!");
			}
		}

		public void Rpc(IDataWriter writer, DataDeliveryMode dataDeliveryMode, DataTarget dataTarget, byte rpcId, byte sequenceChannel = 0)
		{
			switch (dataTarget)
			{
				case DataTarget.Self:
					Rpc(writer, dataDeliveryMode, m_Identity.OwnerId, rpcId, sequenceChannel);
					break;
				case DataTarget.Broadcast:
					{
						foreach ((int _, NetworkPeer peer) in OmniNetwork.Communicator.GetPeers())
						{
							Rpc(writer, dataDeliveryMode, peer.Id, rpcId, sequenceChannel);
						}
					}
					break;
				case DataTarget.BroadcastExcludingSelf:
					{
						foreach ((int _, NetworkPeer peer) in OmniNetwork.Communicator.GetPeers())
						{
							if (peer.Id == m_Identity.OwnerId)
								continue;

							Rpc(writer, dataDeliveryMode, peer.Id, rpcId, sequenceChannel);
						}
					}
					break;
				case DataTarget.Server:
					Rpc(writer, dataDeliveryMode, rpcId, sequenceChannel);
					break;
			}
		}

		public void Rpc(IDataWriter writer, DataDeliveryMode dataDeliveryMode, byte rpcId, byte sequenceChannel = 0)
		{
			if (!IsServer)
			{
				OmniNetwork.Communicator.Rpc(writer, dataDeliveryMode, m_Identity.Id, m_Id, rpcId, sequenceChannel);
			}
			else
			{
				OmniLogger.PrintError("Error: Server cannot use client-side RPC invocation. Use server-side RPC methods instead.");
			}
		}

		public void Rpc(IDataWriter writer, DataDeliveryMode dataDeliveryMode, int peerId, byte rpcId, byte sequenceChannel = 0)
		{
			if (IsServer)
			{
				OmniNetwork.Communicator.Rpc(writer, dataDeliveryMode, peerId, m_Identity.Id, m_Id, rpcId, sequenceChannel);
			}
			else
			{
				OmniLogger.PrintError("Error: Client cannot use server-side RPC invocation. Use client-side RPC methods instead.");
			}
		}
	}
}
