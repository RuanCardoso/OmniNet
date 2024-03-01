using Omni.Core;

namespace Omni.Internal.Samples
{
	public partial class RpcWithSourceGeneratos : NetworkBehaviour
	{
		protected override void OnNetworkStart()
		{
			
		}

		private void Update()
		{
			if (IsServer)
			{
				SerializeView(1, DataDeliveryMode.Unreliable);
			}
		}

		protected override void OnSerializeView(IDataWriter writer)
		{
			writer.Write(100);
		}

		protected override void OnDeserializeView(IDataReader reader, NetworkPeer peer)
		{
			OmniLogger.PrintError(reader.ReadInt());
		}
	}
}