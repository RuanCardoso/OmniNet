using Omni.Core;
using System;
using UnityEngine;

namespace Omni.Internal.Samples
{
	[Remote(Id = 10, Name = "SyncColor", Self = false)]
	public partial class RpcWithSourceGeneratos : NetworkBehaviour
	{
		partial void SyncColor_Client(IDataReader reader, NetworkPeer peer)
		{
			throw new System.NotImplementedException();
		}

		partial void SyncColor_Server(IDataReader reader, NetworkPeer peer)
		{
			throw new System.NotImplementedException();
		}

		[SerializeField]
		private bool IsServerAuthority = false;

		[NetVar(SerializeAsJson = true)]
		private Action<float, byte[]> OnSync;

		public override void Awake()
		{
			base.Awake();

			OnSync += (health, arr) =>
			{
				OmniLogger.PrintError($"Chegou evento do: {IsServer} : {health} : {arr.Length}");
			};
		}

		public override void Start()
		{
			base.Start();
		}

		private void Update()
		{
			// Use the generated property to sync!
			if (IsServerAuthority)
			{
				if (IsServer)
				{
					//SyncColor(DataWriter.Empty, DataDeliveryMode.Unreliable, 1);
				}
			}
			else
			{
				if (IsClient)
				{
					if (Input.GetKeyDown(KeyCode.L))
					{
						OnSyncInvoke(10f, new byte[] { 10, 10 });
					}
				}
			}
		}

		public override void OnNetworkStart()
		{

		}
	}
}