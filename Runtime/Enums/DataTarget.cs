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
	/// Enum that represents the data target, used to send data to a specific target.
	/// </summary>
	public enum DataTarget : byte
	{
		/// <summary>
		/// Only the server will receive the data.
		/// </summary>
		Server = 0,
		/// <summary>
		/// All clients will receive the data.
		/// </summary>
		Broadcast = 1,
		/// <summary>
		/// All clients will receive the data, except the sender.
		/// </summary>
		BroadcastExcludingSelf = 2,
		/// <summary>
		/// Only the sender will receive the data.
		/// </summary>
		Self = 3,
	}
}