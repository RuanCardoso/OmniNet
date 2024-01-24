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
using System.Runtime.InteropServices;

namespace Omni.Core
{
	public class DataReader : IDataReader
	{
		public byte[] Buffer { get; }
		public int Position { get; private set; }
		public int BytesWritten { get; private set; }

		public DataReader(int size)
		{
			Buffer = new byte[size];
		}

		public void Write(byte[] buffer, int offset, int count)
		{
			int available = count - offset;
			for (int i = 0; i < available; i++)
			{
				if (!(Position + count > Buffer.Length))
				{
					Buffer[Position++] = buffer[offset + i];
					BytesWritten += 1;
				}
				else
				{
					OmniLogger.PrintError($"DataReader: Insufficient space to write {count} bytes. Current position: {Position}, requested position: {Position + count}");
				}
			}
			Position = 0;
		}

		public void Read(byte[] buffer, int offset, int count)
		{
			int available = count - offset;
			for (int i = 0; i < available; i++)
			{
				buffer[offset + i] = ReadByte();
			}
		}

		public byte ReadByte()
		{
			if (ThrowIfNotEnoughData(sizeof(byte)))
			{
				return Buffer[Position++];
			}

			return 0;
		}

		public decimal ReadDecimal()
		{
			throw new System.NotImplementedException();
		}

		public unsafe double ReadDouble()
		{
			uint lo = (uint)(ReadByte() | ReadByte() << 8 |
			   ReadByte() << 16 | ReadByte() << 24);

			uint hi = (uint)(ReadByte() | ReadByte() << 8 |
			  ReadByte() << 16 | ReadByte() << 24);

			ulong tmpBuffer = ((ulong)hi) << 32 | lo;
			return *(double*)&tmpBuffer;
		}

		public unsafe float ReadFloat()
		{
			uint tmpBuffer = ReadByte();
			tmpBuffer |= (uint)(ReadByte() << 8);
			tmpBuffer |= (uint)(ReadByte() << 16);
			tmpBuffer |= (uint)(ReadByte() << 24);
			return *(float*)&tmpBuffer;
		}

		public int ReadInt()
		{
			int value = ReadByte();
			value |= ReadByte() << 8;
			value |= ReadByte() << 16;
			value |= ReadByte() << 24;
			return value;
		}

		public long ReadLong()
		{
			uint lo = (uint)(ReadByte() | ReadByte() << 8 |
							ReadByte() << 16 | ReadByte() << 24);
			uint hi = (uint)(ReadByte() | ReadByte() << 8 |
							 ReadByte() << 16 | ReadByte() << 24);
			return (long)((ulong)hi) << 32 | lo;
		}

		public sbyte ReadSByte()
		{
			return (sbyte)ReadByte();
		}

		public short ReadShort()
		{
			short value = ReadByte();
			value |= (short)(ReadByte() << 8);
			return value;
		}

		public uint ReadUInt()
		{
			uint value = ReadByte();
			value |= (uint)(ReadByte() << 8);
			value |= (uint)(ReadByte() << 16);
			value |= (uint)(ReadByte() << 24);
			return value;
		}

		public ulong ReadULong()
		{
			uint lo = (uint)(ReadByte() | ReadByte() << 8 |
							ReadByte() << 16 | ReadByte() << 24);
			uint hi = (uint)(ReadByte() | ReadByte() << 8 |
							 ReadByte() << 16 | ReadByte() << 24);
			return ((ulong)hi) << 32 | lo;
		}

		public ushort ReadUShort()
		{
			ushort value = ReadByte();
			value |= (ushort)(ReadByte() << 8);
			return value;
		}

		public int Read7BitEncodedInt()
		{
			// Read out an Int32 7 bits at a time.  The high bit
			// of the byte when on means to continue reading more bytes.
			int count = 0;
			int shift = 0;
			byte b;
			do
			{
				// Check for a corrupted stream.  Read a max of 5 bytes.
				// In a future version, add a DataFormatException.
				if (shift == 5 * 7)  // 5 bytes max per Int32, shift += 7
					throw new FormatException("Format_Bad7BitInt32");

				// ReadByte handles end of stream cases for us.
				b = ReadByte();
				count |= (b & 0x7F) << shift;
				shift += 7;
			} while ((b & 0x80) != 0);
			return count;
		}

		public unsafe T Marshalling_ReadStructure<T>() where T : struct
		{
			int length = Read7BitEncodedInt();
			byte[] data = new byte[length];
			Read(data, 0, length);
			fixed (byte* ptrToData = &data[0])
			{
				return (T)Marshal.PtrToStructure(new IntPtr(ptrToData), typeof(T));
			}
		}

		public void Recycle()
		{
			Position = 0;
			BytesWritten = 0;
		}

		private bool ThrowIfNotEnoughData(int size)
		{
			if (Position + size > BytesWritten)
			{
				OmniLogger.PrintError($"Not enough data to read. Requested: {size} bytes, available: {BytesWritten - Position} bytes, current position: {Position}");
				OmniLogger.PrintError("Possible Error: Double event not registered for the same IOHandler? Possible double data read.");
				return false;
			}

			return true;
		}
	}
}