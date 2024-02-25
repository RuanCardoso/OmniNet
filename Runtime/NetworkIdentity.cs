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

using Omni.Internal.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Omni.Core
{
	[DefaultExecutionOrder(-400)]
	public class NetworkIdentity : RealtimeTickBasedSystem
	{
		private ITransport ServerTransport => OmniNetwork.Main.ServerTransport;
		private ITransport ClientTransport => OmniNetwork.Main.ClientTransport;
		internal Dictionary<int, NetworkBehaviour> NetworkBehaviours { get; } = new Dictionary<int, NetworkBehaviour>(); // Network Behaviour Id, Network Behaviour Instance

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
		[ReadOnly]
		private int m_OwnerId; // 0 - Is Server
		[SerializeField]
		[ReadOnly]
		private bool m_IsServer;
		[SerializeField]
		[ReadOnly]
		private bool m_IsDynamic;

		public int Id
		{
			get => m_Id;
			internal set => m_Id = value;
		}

		public int OwnerId
		{
			get => m_OwnerId;
			internal set => m_OwnerId = value;
		}

		public bool IsServer
		{
			get
			{
				if (ServerTransport == null)
				{
					return false;
				}

				if (!ServerTransport.IsInitialized)
				{
					return false;
				}
				return m_IsServer;
			}
			internal set => m_IsServer = value;
		}

		public bool IsClient
		{
			get
			{
				if (ClientTransport == null)
				{
					return false;
				}

				if (!ClientTransport.IsInitialized || !ClientTransport.IsConnected)
				{
					return false;
				}
				return !m_IsServer;
			}
		}

		public bool IsDynamic
		{
			get => m_IsDynamic;
			internal set => m_IsDynamic = value;
		}

		public SpawnMode SpawnMode => m_SpawnMode;
		public bool IsStatic => m_IsStatic;
		public bool IsServerSimulation => m_IsServerSimulation;

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (Application.isPlaying)
			{
				return;
			}

			if (m_Id == 0)
			{
				int instanceId = gameObject.GetInstanceID();
				if (m_Id != instanceId)
				{
					m_Id = instanceId;
				}
			}
			else
			{
				IsDuplicated(FindObjectsOfType<NetworkIdentity>(true));
			}

			void IsDuplicated(NetworkIdentity[] identities)
			{
				int count = identities.Count(x => x.m_Id == m_Id);
				if (count > 1) // duplicated id...
				{
					m_Id = gameObject.GetInstanceID();
				}
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
            m_IsServerSimulation = true;
			IsServer = true;
#endif
#if UNITY_EDITOR
			gameObject.name = gameObject.name.Replace("(Clone)", "");
#endif
			if (!OmniNetwork.Communicator.NetworkIdentities.TryAdd(ValueTuple.Create(m_Id, IsServer), this))
			{
				OmniLogger.PrintError($"Error: Duplicated ID '{m_Id}'. Ensure unique IDs for each instance of Network Identity");
			}
		}

		public override void Start()
		{
			if (IsStatic)
			{
#if !UNITY_SERVER || UNITY_EDITOR
				CreateServerSimulation();
#endif
				return;
			}

			if (m_SpawnMode == SpawnMode.Dynamic)
			{
				if (!m_IsDynamic)
				{
					OmniLogger.PrintError("A dynamic object must be instantiated.");
					Destroy(gameObject);
				}
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
			yield return new WaitUntil(() => OmniNetwork.Main.IsConnected);
#if !UNITY_SERVER || UNITY_EDITOR
			if (m_SpawnMode == SpawnMode.Scene)
			{
				CreateServerSimulation();
			}
			MakeLabeledInfo();
#endif
			// Note: All initialization code should be here, because that's when the server and client are ready.
			// .......
		}

		private void CreateServerSimulation()
		{
			if (!IsServerSimulation && OmniNetwork.Main.HasServer) // Work only on the server object
			{
				// Assign exposed vars before instantiating prefab....
				m_IsServerSimulation = IsServer = true;
				GameObject serverObject = Instantiate(gameObject);
				// Unassign exposed vars after instantiating prefab....
				m_IsServerSimulation = IsServer = false;
				NetworkIdentity serverIdentity = serverObject.GetComponent<NetworkIdentity>();
				serverIdentity.IsServer = true;

				// Ignore physics between client object and server object
				SceneManager.MoveGameObjectToScene(serverObject, OmniNetwork.Main.EditorScene.GetValueOrDefault());

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

	public enum SpawnMode
	{
		Scene,
		Dynamic
	}
}