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
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Omni.Core
{
	public class NetworkIdentity : RealtimeTickBasedSystem
	{
		internal static Dictionary<(bool, int), NetworkIdentity> NetworkIdentities { get; } = new(); // Is Server, Instance Id

		// Note: Byte allow only 255 SyncVars per identity.
		// Note: Use another identity if you need more!
		internal byte OnSyncBaseId { get; set; }
		internal Action<byte, IDataReader> OnSyncBase { get; set; }

		[SerializeField]
		[InfoBox("Static: This object has no interaction with the network, but a copy of it will be generated on the server side(Debug Mode)")]
		private bool m_IsStatic;
		[SerializeField]
		[DisableIf("m_IsStatic")] private SpawnMode m_SpawnMode;
		[SerializeField]
		[HideInInspector]
		private bool m_IsServerSimulation;
		[SerializeField]
		[ReadOnly]
		private int m_Id;
		[SerializeField]
		[ReadOnly]
		private bool m_IsServer;

		public bool IsStatic { get => m_IsStatic; private set => m_IsStatic = value; }
		public bool IsServerSimulation { get => m_IsServerSimulation; private set => m_IsServerSimulation = value; }
		public int Id { get => m_Id; private set => m_Id = value; }
		public bool IsServer { get => m_IsServer; private set => m_IsServer = value; }

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (Application.isPlaying)
			{
				return;
			}

			if (m_SpawnMode == SpawnMode.Scene)
			{
				int instanceId = GetInstanceID();
				if (m_Id != instanceId)
				{
					m_Id = instanceId;
				}
			}
			else
			{
				m_Id = 0;
			}
		}

		private void Reset()
		{
			OnValidate();
		}
#endif

		private void Awake()
		{
#if UNITY_SERVER && !UNITY_EDITOR
			IsServer = true;
#endif
		}

		protected override void Start()
		{
			if (IsStatic)
			{
#if !UNITY_SERVER || UNITY_EDITOR
				CreateServerSimulation();
#endif
				return;
			}

			base.Start();
			if (transform.root == transform)
			{
				NetworkIdentity[] networkIdentities = GetComponentsInChildren<NetworkIdentity>();
				if (networkIdentities.Length > 1)
				{
					OmniLogger.PrintError("Multiple NetworkIdentity components found. Ensure there is only one per GameObject for proper network functionality.");
					OmniLogger.PrintError("Only the root object should have a NetworkIdentity component.");
					Destroy(gameObject);
					return;
				}
			}
			StartCoroutine(Initialize());
		}

		private IEnumerator Initialize()
		{
			yield return new WaitUntil(() => OmniNetwork.Omni.IsConnected);
#if !UNITY_SERVER || UNITY_EDITOR
			if (m_SpawnMode == SpawnMode.Scene)
			{
				CreateServerSimulation();
			}
#endif
			// Note: All initialization code should be here, because that's when the server and client are ready.
			RequestGameObjectId();
		}

		private void RequestGameObjectId()
		{
			if (m_SpawnMode == SpawnMode.Dynamic)
			{
				gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
				gameObject.SetActive(false);

				// Instantiate the player
				try
				{
					if (m_Id == 0)
					{
						//IDataWriter writer = NetworkCommunicator.DataWriterPool.Get();
						//writer.Write((byte)InternalNetMessage.DynamicSpawn);
						//IDataReader reader = await OmniNetwork.Communicator.Internal_SendCustomMessageAsync(writer, DataDeliveryMode.Secured, 0);
						//NetworkCommunicator.DataWriterPool.Release(writer);
						Debug.LogError("Instantiate");
					}
				}
				catch (TaskCanceledException) { }
			}
			else
			{
#if !UNITY_SERVER || UNITY_EDITOR
				MakeLabeledInfo();
#endif
			}
		}

		private void CreateServerSimulation()
		{
			if (!IsServerSimulation && OmniNetwork.Omni.HasServer) // Work only on the server object
			{
				// Assign exposed vars before instantiating prefab....
				IsServerSimulation = IsServer = true;
				GameObject serverObject = Instantiate(gameObject);
				// Unassign exposed vars after instantiating prefab....
				IsServerSimulation = IsServer = false;
				NetworkIdentity serverIdentity = serverObject.GetComponent<NetworkIdentity>();
				serverIdentity.IsServer = true;

				// Ignore physics between client object and server object
				SceneManager.MoveGameObjectToScene(serverObject, OmniNetwork.Omni.ServerScene.GetValueOrDefault());

				// Disable shadows for server simulation;
				Renderer[] renderers = serverObject.GetComponentsInChildren<Renderer>();
				foreach (var renderer in renderers)
				{
					renderer.receiveShadows = false;
					renderer.shadowCastingMode = ShadowCastingMode.Off;
					renderer.material.color = !IsStatic ? Color.black : new Color(0f, 97f / 255f, 26f / 255f);
				}
			}
		}

		private void MakeLabeledInfo()
		{
			// Create a label
			Vector3 labelPos = transform.position;
			if (!IsServer)
			{
				labelPos.y += 0.2f;
			}
			else
			{
				labelPos.y -= 0.2f;
			}

			GameObject @object = new("@canvas");
			@object.transform.position = labelPos;
			@object.transform.localScale = new(0.02f, 0.02f, 0.02f);
			@object.transform.parent = transform;
			//----------------------------------------------------------
			Canvas canvas = @object.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.WorldSpace;
			//----------------------------------------------------------
			TextMesh text = @object.AddComponent<TextMesh>();
			text.text = $"{(IsServer ? "Server -> " : "Client -> ")} {m_Id}";
			text.fontStyle = FontStyle.Bold;
			text.fontSize = 90;
			text.color = !IsServer ? Color.white : !IsStatic ? Color.red : new Color(0f, 97f / 255f, 26f / 255f);
		}
	}

	enum SpawnMode
	{
		Scene,
		Dynamic
	}
}