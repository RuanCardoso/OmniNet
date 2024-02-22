using Omni.Internal;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Omni.Core
{
	public static class NetworkExtensions
	{
		public static NetworkIdentity Instantiate(this NetworkIdentity prefab, NetworkPeer owner, bool isServer, Vector3 position, Quaternion rotation, Action<NetworkIdentity> OnBeforeInstantiating = null)
		{
			int uniqueId = NetworkHelper.GetUniqueNetworkIdentityId();
			return Instantiate(prefab, uniqueId, owner, isServer, position, rotation, OnBeforeInstantiating);
		}

		public static NetworkIdentity Instantiate(this NetworkIdentity prefab, int identityId, NetworkPeer owner, bool isServer, Vector3 position, Quaternion rotation, Action<NetworkIdentity> OnBeforeInstantiating = null)
		{
			// Assign exposed vars before instantiating prefab....
			prefab.gameObject.SetActive(false);
			if (prefab.SpawnMode == SpawnMode.Scene)
			{
				throw new Exception("Error: Instantiated NetworkIdentity should not have SpawnMode set to Scene. Use dynamic instead.");
			}

			NetworkIdentity identity = MonoBehaviour.Instantiate(prefab, position, rotation);
			identity.Id = identityId;
			identity.OwnerId = owner.Id;
			identity.IsDynamic = true;
			identity.IsServer = isServer;
			OnBeforeInstantiating?.Invoke(identity);
			identity.gameObject.SetActive(true);
#if !UNITY_SERVER || UNITY_EDITOR
			if (isServer && OmniNetwork.Main.HasServer)
			{
				// Ignore physics between client object and server object
				SceneManager.MoveGameObjectToScene(identity.gameObject, OmniNetwork.Main.ServerScene.GetValueOrDefault());
			}
#endif
			// Unassign exposed vars after instantiating prefab....
			prefab.gameObject.SetActive(true);
			NetworkCallbacks.FireServerGameObjectInstantiated(identity, owner);
			return identity;
		}

		public static T ReadCustomMessage<T>(this int value) where T : unmanaged, IComparable, IConvertible, IFormattable
		{
			return Unsafe.As<int, T>(ref value);
		}
	}
}