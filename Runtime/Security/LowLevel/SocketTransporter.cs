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
using static Omni.Core.Enums;

namespace Omni.Core
{
	internal class SocketTransporter
	{
		protected virtual void Connect()
		{
			throw new NotImplementedException("Connect method not implemented. Please override this method in your derived class.");
		}

		protected virtual void ConnectAsync()
		{
			throw new NotImplementedException("ConnectAsync method not implemented. Please override this method in your derived class.");
		}

		protected virtual void InitServer()
		{
			throw new NotImplementedException("InitServer method not implemented. Please override this method in your derived class.");
		}

		protected virtual void InitServerAsync()
		{
			throw new NotImplementedException("InitServerAsync method not implemented. Please override this method in your derived class.");
		}

		protected virtual void OnMessage(DataIOHandler IOHandler, DataDeliveryMode deliveryMode, DataTarget target, DataProcessingOption processingOption, DataCachingOption cachingOption, MessageType messageType, UdpEndPoint remoteEndPoint)
		{
			throw new NotImplementedException("OnMessage method not implemented. Please override this method in your derived class.");
		}

		protected virtual void Disconnect(UdpEndPoint endPoint, string msg = "")
		{
			throw new NotImplementedException($"Disconnect method not implemented. {msg}");
		}

		protected virtual UdpClient GetClient(UdpEndPoint remoteEndPoint)
		{
			throw new NotImplementedException("GetClient method not implemented. Please override this method in your derived class.");
		}
	}
}
