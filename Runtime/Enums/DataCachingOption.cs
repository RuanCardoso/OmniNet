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
	/// Determines whether the data will be cached on the server or not.
	/// </summary>
	public enum DataCachingOption : byte
	{
		/// <summary>
		/// The data will not be cached on the server.
		/// </summary>
		None = 0,
		/// <summary>
		/// The data will be cached on the server with append mode, the data will be added to the cache.<br/>
		/// Warning: This mode can cause memory overflow if the data is not removed from the cache.<br/>
		/// Warning: This mode has a high performance cost, use it only if necessary and if the high memory usage is acceptable.<br/>
		/// </summary>
		Append = 1,
		/// <summary>
		/// The data will be cached on the server with overwrite mode, the data will be overwritten in the cache if it already exists.<br/>
		/// High performance, recommended for most cases.<br/>
		/// </summary>
		Overwrite = 2,
	}
}