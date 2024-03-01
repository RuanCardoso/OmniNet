using System;
using Omni.Core;
using UnityEngine;

namespace Omni.Internal.Samples
{
	public partial class RpcWithSourceGeneratos : MonoBehaviour
	{
		private void Awake()
		{
			NetworkCallbacks.OnClientConnected += OnClientConnected;
		}

		private void OnClientConnected(bool isServer, NetworkPeer peer)
		{
			OmniLogger.PrintError(isServer);
		}
	}
}