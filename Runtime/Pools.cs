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
	public class DataWriterPool
	{
		private int length;
		private readonly Stack<IDataWriter> pool = new();

		public DataWriterPool(int length = 128)
		{
			this.length = length;
			for (int i = 0; i < length; i++)
			{
				pool.Push(new DataWriter(length));
			}
		}

		public IDataWriter Get()
		{
			if (pool.Count == 0)
			{
				OmniLogger.Print("Pool: No DataIOHandler's are currently available. A temporary DataIOHandler will be created to handle this data.");
				return new DataWriter(length);
			}
			else
			{
				return pool.Pop();
			}
		}

		public void Release(IDataWriter writer)
		{
			writer.Clear();
			pool.Push(writer);
		}

		public int Count => pool.Count;
	}

	/// <summary>
	/// Pool of reusable DataReaders's for efficient memory usage and CPU performance.
	/// This class is not thread-safe.
	/// </summary>
	public class DataReaderPool
	{
		private readonly Stack<IDataReader> pool = new();

		public DataReaderPool(int length = 128)
		{
			for (int i = 0; i < length; i++)
			{
				pool.Push(new DataReader(length));
			}
		}

		public IDataReader Get()
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

		public void Release(IDataReader reader)
		{
			reader.Recycle();
			pool.Push(reader);
		}

		public int Count => pool.Count;
	}
}