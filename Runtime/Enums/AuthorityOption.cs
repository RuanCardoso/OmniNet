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

namespace Omni.Core
{
	/// <summary>
	/// Represents the authority mode for network objects.
	/// </summary>
	public enum AuthorityMode
	{
		/// <summary>
		/// When is mine, the object will be controlled by the local player.
		/// </summary>
		Mine,
		/// <summary>
		/// When is server, the object will be controlled by the server.
		/// </summary>
		Server,
		/// <summary>
		/// When is client, the object will be controlled by the remote player.<br/>
		/// All players has authority over the object.
		/// </summary>
		Client,
		/// <summary>
		/// When is custom, the object will be controlled by the your authority implementation.
		/// </summary>
		Custom
	}
}
