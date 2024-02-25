using Omni.Core;
using UnityEngine;
using static Omni.Core.OmniNetwork;

namespace Omni.Internal.Samples
{
	public partial class RpcWithSourceGeneratos : NetworkBehaviour
	{
		public override void OnNetworkStart()
		{

		}

		public override void OnNetworkEventsRegister()
		{
			NetworkCallbacks.OnChannelPlayerJoined += NetworkCallbacks_OnChannelPlayerJoined;
			NetworkCallbacks.OnChannelPlayerLeft += NetworkCallbacks_OnChannelPlayerLeft;
		}

		private void NetworkCallbacks_OnChannelPlayerJoined(bool arg1, NetworkPeer arg2, int arg3)
		{
			OmniLogger.PrintError("Entrou no: " + arg3 + " : " + arg1);
		}

		private void NetworkCallbacks_OnChannelPlayerLeft(bool arg1, NetworkPeer arg2, int arg3)
		{
			OmniLogger.PrintError("Left no: " + arg3 + " : " + arg1);
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.V) && IsServer)
			{
				int channel = 1;
				Matchmaking.JoinChannel(channel, 1);
				OmniLogger.PrintError("Enviado");
			}

			if (Input.GetKeyDown(KeyCode.G) && IsServer)
			{
				int channel = 1;
				Matchmaking.LeaveChannel(channel, 1);
				OmniLogger.PrintError("Enviado");
			}
		}
	}
}