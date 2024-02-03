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

using System;
using System.Threading.Tasks;

namespace Omni.Core
{
	public static class AwaitableHelper
	{
		//internal static Task Internal_SendCustomMessageAsync(IDataWriter writer, DataDeliveryMode deliveryMode, byte channel)
		//{
		//	if (deliveryMode == DataDeliveryMode.Unsecured)
		//	{
		//		throw new NotSupportedException("Unsecured data delivery mode is not supported. Use a secured data delivery mode for this operation.");
		//	}

		//	Half a;
		//	TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
		//	OmniNetwork.Communicator.Internal_SendCustomMessage(writer, deliveryMode, channel);
		//	return tcs.Task;
		//}
	}
}
