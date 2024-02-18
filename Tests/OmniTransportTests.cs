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

using NUnit.Framework;
using Omni.Core;
using Omni.Internal;
using System.Net;
using System.Net.Sockets;

public class OmniTransportTests
{
	[Test]
	public void CheckIfTransportersIsNotNullAndInitialized()
	{
		OmniNetwork omniNet = new OmniNetwork();
		omniNet.InitializeTransport();

		Assert.IsNotNull(omniNet.TransportSettings);
		Assert.IsNotNull(omniNet.ClientTransport);
		Assert.IsNotNull(omniNet.ServerTransport);

		Assert.AreNotEqual(omniNet.ClientTransport, omniNet.ServerTransport);

		Assert.IsTrue(omniNet.ClientTransport.IsInitialized);
		Assert.IsTrue(omniNet.ServerTransport.IsInitialized);
	}

	[Test]
	public void CheckIfIDataReaderIsValid()
	{
		int value = int.MaxValue;
		IDataReader dataReader = new DataReader(10);
		dataReader.Write(new byte[4] { (byte)value, (byte)(value >> 8), (byte)(value >> 16), (byte)(value >> 24) }, 0, 4);

		Assert.AreEqual(0, dataReader.Position);
		Assert.AreEqual(4, dataReader.BytesWritten);
		Assert.AreEqual(value, dataReader.ReadInt());
	}

	[Test]
	public void CheckIfIDataWriterIsValid()
	{
		ushort value = ushort.MaxValue;
		IDataWriter dataReader = new DataWriter(10);
		dataReader.Write(value);

		Assert.AreEqual(2, dataReader.Position);
		Assert.AreEqual(2, dataReader.BytesWritten);
	}

	[Test]
	public void CheckIfPortIsDisponible()
	{
		TcpListener tcpListener = new TcpListener(IPAddress.Any, 7778);
		tcpListener.Start();

		bool value = NetworkHelper.IsAvailablePort(1024, NetProtocol.Tcp);
		Assert.AreEqual(value, true);

		bool value2 = NetworkHelper.IsAvailablePort(1025, NetProtocol.Tcp);
		Assert.AreEqual(value2, true);

		//bool value3 = NetworkHelper.IsAvailablePort(123);
		//Assert.AreEqual(value3, false);

		bool value4 = NetworkHelper.IsAvailablePort(7778, NetProtocol.Tcp);
		Assert.AreEqual(value4, false);

		bool value4inverted = !NetworkHelper.IsAvailablePort(7778, NetProtocol.Tcp);
		Assert.AreEqual(value4inverted, true);

		tcpListener.Stop();

		bool value5 = NetworkHelper.IsAvailablePort(7778, NetProtocol.Tcp);
		Assert.AreEqual(value5, true);
	}
}
