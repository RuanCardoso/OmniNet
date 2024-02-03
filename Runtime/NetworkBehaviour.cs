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

using UnityEngine;

namespace Omni.Core
{
	public class NetworkBehaviour : RealtimeTickBasedSystem
	{
		[SerializeField][Required("It cannot be null!")] private NetworkIdentity m_Identity;
		public NetworkIdentity Identity => m_Identity;
	}
}
