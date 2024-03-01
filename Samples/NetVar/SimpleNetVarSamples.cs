using Omni.Core;
using UnityEngine;

namespace Omni.Internal.Samples
{
	public partial class SimpleNetVarSamples : NetworkBehaviour // PARTIAL!!!! NetworkBehaviour!!!!
	{
		// NetVar only work with fields!

		// Simple Types
		// correct naming conventions for NetVar
		// private/public float 'm_Health' -> Ok
		// private/public float 'health' -> Ok

		// incorrect naming conventions for NetVar
		// private/public 'M_health' -> Error
		// private/public 'M_Health' -> Error
		// private/public 'any other letter'_health -> Error
		// private/public 'Health' -> Error

		// There are no naming convention rules for delegates.

		[SerializeField]
		private bool IsServerAuthority = false;

		[NetVar]
		[SerializeField]
		private float m_Health;

		[NetVar]
		[SerializeField]
		private int m_Mana;

		[NetVar]
		[SerializeField]
		private decimal m_Coins;

		[NetVar]
		[SerializeField]
		private long m_Diamonds;

		protected override void OnNetworkStart()
		{
		}

		[Button]
		private void Test()
		{
			// Use the generated property to sync!
			if (IsServerAuthority)
			{
				if (IsServer)
				{
					Health += 1;
					Mana += 1;
					Coins += 1;
					Diamonds += 1;
				}
			}
			else
			{
				if (IsClient)
				{
					Health += 1;
					Mana += 1;
					Coins += 1;
					Diamonds += 1;
				}
			}
		}
	}
}
