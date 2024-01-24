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

using System.Collections.Generic;

#pragma warning disable

namespace Omni.Core
{
	/// <summary>
	/// Pool of reusable DataWriter's for efficient memory usage and CPU performance.
	/// This class is not thread-safe.
	/// </summary>
	internal class DataWriterPool
	{
		private readonly Stack<DataWriter> pool = new();

		public DataWriterPool(int length = 128)
		{
			for (int i = 0; i < length; i++)
			{
				pool.Push(new DataWriter(length));
			}
		}

		public DataWriter Get()
		{
			if (pool.Count == 0)
			{
				OmniLogger.Print("Pool: No DataIOHandler's are currently available. A temporary DataIOHandler will be created to handle this data.");
				return new DataWriter(128);
			}
			else
			{
				return pool.Pop();
			}
		}

		public void Release(DataWriter IOHandler)
		{
			IOHandler.Recycle();
			pool.Push(IOHandler);
		}

		public int Count => pool.Count;
	}

	/// <summary>
	/// Pool of reusable DataReaders's for efficient memory usage and CPU performance.
	/// This class is not thread-safe.
	/// </summary>
	internal class DataReaderPool
	{
		private readonly Stack<DataReader> pool = new();

		public DataReaderPool(int length = 128)
		{
			for (int i = 0; i < length; i++)
			{
				pool.Push(new DataReader(length));
			}
		}

		public DataReader Get()
		{
			if (pool.Count == 0)
			{
				OmniLogger.Print("Pool: No DataIOHandler's are currently available. A temporary DataIOHandler will be created to handle this data.");
				return new DataReader(1500);
			}
			else
			{
				return pool.Pop();
			}
		}

		public void Release(DataReader IOHandler)
		{
			IOHandler.Recycle();
			pool.Push(IOHandler);
		}

		public int Count => pool.Count;
	}
}