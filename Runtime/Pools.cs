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
			IDataWriter writer = Internal_Get();
			if (writer.Position > 0 || writer.BytesWritten > 0)
			{
				throw new Exception("Error: Data writer is not in the expected state. Make sure to consume or reset the writer before calling Get().");
			}
			return writer;
		}

		private IDataWriter Internal_Get()
		{
			if (pool.Count == 0)
			{
				OmniLogger.Print("Pool: No DataWriter are currently available. A temporary DataWriter will be created to handle this data.");
				return new DataWriter(length);
			}
			else
			{
				return pool.Pop();
			}
		}

		public void Release(IDataWriter writer)
		{
			if (writer.Position == 0 || writer.BytesWritten == 0)
			{
				throw new Exception("Error: Data reader cannot be released as the pool has already been released.");
			}

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
			IDataReader reader = Internal_Get();
			if (reader.Position > 0 || reader.BytesWritten > 0)
			{
				throw new Exception("Error: Data reader is not in the expected state. Make sure to consume or reset the reader before calling Get().");
			}
			return reader;
		}

		private IDataReader Internal_Get()
		{
			if (pool.Count == 0)
			{
				OmniLogger.Print("Pool: No DataReader are currently available. A temporary DataReader will be created to handle this data.");
				return new DataReader(1500);
			}
			else
			{
				return pool.Pop();
			}
		}

		public void Release(IDataReader reader)
		{
			if (reader.Position == 0 || reader.BytesWritten == 0)
			{
				throw new Exception("Error: Data reader cannot be released as the pool has already been released.");
			}

			reader.Clear();
			pool.Push(reader);
		}

		public int Count => pool.Count;
	}
}