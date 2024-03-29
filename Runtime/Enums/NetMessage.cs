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

namespace Omni.Internal
{
	internal enum NetMessage : byte
	{
		Ping,
		Test,
		Message, // for client
		InternalMessage, // internal use
		RequestId,
		Rpc,
		SerializeView,
		LargeBlockOfBytes,
		InternalLargeBlockOfBytes,
        NetVar,
	}
}