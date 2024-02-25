using Omni.Core;
using UnityEngine;

namespace Omni.Internal.Samples
{
	public partial class ComplexNetVarSamples : NetworkBehaviour // PARTIAL!!!! NetworkBehaviour!!!!
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

		[SerializeField]
		private bool IsServerAuthority = false;

		public override void OnNetworkStart()
		{
		}
	}
}