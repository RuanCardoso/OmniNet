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

using MessagePack;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Omni.Core
{
	public class DataReader : IDataReader
	{
		public byte[] Buffer { get; }
		public int Position { get; set; }
		public int BytesWritten { get; set; }
		public bool ResetPositionAfterWriting { get; set; } = true;
		public bool IsReleased { get; set; }
		public Encoding Encoding { get; set; }

		public DataReader(int size)
		{
			Buffer = new byte[size];
			Encoding = Encoding.UTF8;
		}

		public void Write(byte[] buffer, int offset, int count)
		{
			int available = count - offset;
			for (int i = 0; i < available; i++)
			{
				if (ThrowIfNotEnoughSpace(1))
				{
					int index = offset + i;
					if (index > (buffer.Length - 1))
					{
						throw new Exception("Data Reader -> An attempt was made to obtain a boundary outside the ranges of the source array(buffer).");
					}
					Buffer[Position++] = buffer[index];
					BytesWritten += 1;
				}
				else break;
			}

			if (ResetPositionAfterWriting)
			{
				Position = 0;
			}
		}

		public void Write(Span<byte> value)
		{
			int count = value.Length;
			int available = count;
			for (int i = 0; i < available; i++)
			{
				if (ThrowIfNotEnoughSpace(1))
				{
					Buffer[Position++] = value[i];
					BytesWritten += 1;
				}
				else break;
			}

			if (ResetPositionAfterWriting)
			{
				Position = 0;
			}
		}

		public void Write(ReadOnlySpan<byte> value)
		{
			int count = value.Length;
			int available = count;
			for (int i = 0; i < available; i++)
			{
				if (ThrowIfNotEnoughSpace(1))
				{
					Buffer[Position++] = value[i];
					BytesWritten += 1;
				}
				else break;
			}

			if (ResetPositionAfterWriting)
			{
				Position = 0;
			}
		}

		public void Write(Stream value)
		{
			int count = (int)value.Length;
			while (Position < count)
			{
				int n = value.Read(Buffer, Position, count - Position);
				if (ThrowIfNotEnoughSpace(n))
				{
					Position += n;
					BytesWritten += n;
				}
				else break;
			}

			if (ResetPositionAfterWriting)
			{
				Position = 0;
			}
		}

		public void Read(byte[] buffer, int offset, int count)
		{
			int available = count - offset;
			for (int i = 0; i < available; i++)
			{
				buffer[offset + i] = ReadByte();
			}
		}

		public int Read(Span<byte> value)
		{
			for (int i = 0; i < value.Length; i++)
			{
				value[i] = ReadByte();
			}
			return value.Length;
		}

		public T ReadCustomMessage<T>() where T : unmanaged, IComparable, IConvertible, IFormattable
		{
			int tValue = Read7BitEncodedInt();
			return tValue.ReadCustomMessage<T>();
		}

		public char ReadChar()
		{
			char value = (char)ReadByte();
			value |= (char)(ReadByte() << 8);
			return value;
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
			int[] bits = new int[4];
			for (int i = 0; i < bits.Length; i++)
			{
				bits[i] = ReadInt();
			}
			return new decimal(bits);
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

		/// <summary>
		/// Reads a string from the data stream.
		/// </summary>
		/// <returns>The read string.</returns>
		public string ReadString()
		{
			int length = Read7BitEncodedInt();
			byte[] encoded = new byte[length];
			Read(encoded, 0, length);
			return Encoding.GetString(encoded);
		}

		/// <summary>
		/// Reads a string to the data stream without incurring memory allocations.
		/// </summary>
		/// <remarks>
		/// This method efficiently utilizes the stack memory using STACKALLOC and is designed to handle small strings.
		/// To minimize memory overhead, it is recommended to avoid using this method for excessively large strings.
		/// </remarks>
		public string ReadStringWithoutAllocation()
		{
			int length = Read7BitEncodedInt();
			Span<byte> encoded = stackalloc byte[length];
			Read(encoded);
			return Encoding.GetString(encoded);
		}

		/// <summary>
		/// Deserializes the data using a custom implementation of ISyncCustom.
		/// </summary>
		/// <typeparam name="T">The type of the custom implementation of ISyncCustom.</typeparam>
		/// <param name="ISyncCustom">The custom implementation of ISyncCustom.</param>
		public void DeserializeWithCustom<T>(ISyncCustom ISyncCustom) where T : class => ISyncCustom.Deserialize(this);
		/// <summary>
		/// Deserializes a JSON string into an object of type T using Json.Net.
		/// </summary>
		/// <typeparam name="T">The type of the object to deserialize.</typeparam>
		/// <param name="options">Optional settings to customize the deserialization process.</param>
		/// <returns>The deserialized object.</returns>
		public T DeserializeWithJsonNet<T>(JsonSerializerSettings options = null) => JsonConvert.DeserializeObject<T>(ReadString(), options);
		/// <summary>
		/// Deserializes a byte array using MessagePack and returns an object of type T.
		/// </summary>
		/// <typeparam name="T">The type of the object to deserialize.</typeparam>
		/// <param name="options">The MessagePackSerializerOptions to use for deserialization.</param>
		/// <returns>The deserialized object of type T.</returns>
		public T DeserializeWithMsgPack<T>(MessagePackSerializerOptions options = null)
		{
			int length = Read7BitEncodedInt();
			byte[] _data_ = new byte[length];
			Read(_data_, 0, length);
			return MessagePackSerializer.Deserialize<T>(_data_, options);
		}

		/// <summary>
		/// Reads a boolean value from the input stream.
		/// </summary>
		/// <returns>The boolean value read from the input stream.</returns>
		public bool ReadBool() => ReadByte() == 1;
		/// <summary>
		/// Reads two boolean values from the stream.
		/// </summary>
		/// <param name="v1">The first boolean value read from the stream.</param>
		/// <param name="v2">The second boolean value read from the stream.</param>
		public void ReadBool(out bool v1, out bool v2)
		{
			byte pByte = ReadByte();
			v1 = (pByte & 0b1) == 1;
			v2 = ((pByte >> 1) & 0b1) == 1;
		}

		/// <summary>
		/// Reads three boolean values from the input stream.
		/// </summary>
		/// <param name="v1">The first boolean value read from the input stream.</param>
		/// <param name="v2">The second boolean value read from the input stream.</param>
		/// <param name="v3">The third boolean value read from the input stream.</param>
		public void ReadBool(out bool v1, out bool v2, out bool v3)
		{
			byte pByte = ReadByte();
			v1 = (pByte & 0b1) == 1;
			v2 = ((pByte >> 1) & 0b1) == 1;
			v3 = ((pByte >> 2) & 0b1) == 1;
		}

		/// <summary>
		/// Reads four boolean values from the input stream.
		/// </summary>
		/// <param name="v1">The first boolean value read from the input stream.</param>
		/// <param name="v2">The second boolean value read from the input stream.</param>
		/// <param name="v3">The third boolean value read from the input stream.</param>
		/// <param name="v4">The fourth boolean value read from the input stream.</param>
		public void ReadBool(out bool v1, out bool v2, out bool v3, out bool v4)
		{
			byte pByte = ReadByte();
			v1 = (pByte & 0b1) == 1;
			v2 = ((pByte >> 1) & 0b1) == 1;
			v3 = ((pByte >> 2) & 0b1) == 1;
			v4 = ((pByte >> 3) & 0b1) == 1;
		}

		/// <summary>
		/// Reads five boolean values from the underlying stream.
		/// </summary>
		/// <param name="v1">The first boolean value.</param>
		/// <param name="v2">The second boolean value.</param>
		/// <param name="v3">The third boolean value.</param>
		/// <param name="v4">The fourth boolean value.</param>
		/// <param name="v5">The fifth boolean value.</param>
		public void ReadBool(out bool v1, out bool v2, out bool v3, out bool v4, out bool v5)
		{
			byte pByte = ReadByte();
			v1 = (pByte & 0b1) == 1;
			v2 = ((pByte >> 1) & 0b1) == 1;
			v3 = ((pByte >> 2) & 0b1) == 1;
			v4 = ((pByte >> 3) & 0b1) == 1;
			v5 = ((pByte >> 4) & 0b1) == 1;
		}

		/// <summary>
		/// Reads six boolean values from the input stream.
		/// </summary>
		/// <param name="v1">The first boolean value.</param>
		/// <param name="v2">The second boolean value.</param>
		/// <param name="v3">The third boolean value.</param>
		/// <param name="v4">The fourth boolean value.</param>
		/// <param name="v5">The fifth boolean value.</param>
		/// <param name="v6">The sixth boolean value.</param>
		public void ReadBool(out bool v1, out bool v2, out bool v3, out bool v4, out bool v5, out bool v6)
		{
			byte pByte = ReadByte();
			v1 = (pByte & 0b1) == 1;
			v2 = ((pByte >> 1) & 0b1) == 1;
			v3 = ((pByte >> 2) & 0b1) == 1;
			v4 = ((pByte >> 3) & 0b1) == 1;
			v5 = ((pByte >> 4) & 0b1) == 1;
			v6 = ((pByte >> 5) & 0b1) == 1;
		}

		/// <summary>
		/// Reads seven boolean values from the underlying stream.
		/// </summary>
		/// <param name="v1">The first boolean value.</param>
		/// <param name="v2">The second boolean value.</param>
		/// <param name="v3">The third boolean value.</param>
		/// <param name="v4">The fourth boolean value.</param>
		/// <param name="v5">The fifth boolean value.</param>
		/// <param name="v6">The sixth boolean value.</param>
		/// <param name="v7">The seventh boolean value.</param>
		public void ReadBool(out bool v1, out bool v2, out bool v3, out bool v4, out bool v5, out bool v6, out bool v7)
		{
			byte pByte = ReadByte();
			v1 = (pByte & 0b1) == 1;
			v2 = ((pByte >> 1) & 0b1) == 1;
			v3 = ((pByte >> 2) & 0b1) == 1;
			v4 = ((pByte >> 3) & 0b1) == 1;
			v5 = ((pByte >> 4) & 0b1) == 1;
			v6 = ((pByte >> 5) & 0b1) == 1;
			v7 = ((pByte >> 6) & 0b1) == 1;
		}

		/// <summary>
		/// Reads 8 boolean values from the stream.
		/// </summary>
		/// <param name="v1">The first boolean value.</param>
		/// <param name="v2">The second boolean value.</param>
		/// <param name="v3">The third boolean value.</param>
		/// <param name="v4">The fourth boolean value.</param>
		/// <param name="v5">The fifth boolean value.</param>
		/// <param name="v6">The sixth boolean value.</param>
		/// <param name="v7">The seventh boolean value.</param>
		/// <param name="v8">The eighth boolean value.</param>
		public void ReadBool(out bool v1, out bool v2, out bool v3, out bool v4, out bool v5, out bool v6, out bool v7, out bool v8)
		{
			byte pByte = ReadByte();
			v1 = (pByte & 0b1) == 1;
			v2 = ((pByte >> 1) & 0b1) == 1;
			v3 = ((pByte >> 2) & 0b1) == 1;
			v4 = ((pByte >> 3) & 0b1) == 1;
			v5 = ((pByte >> 4) & 0b1) == 1;
			v6 = ((pByte >> 5) & 0b1) == 1;
			v7 = ((pByte >> 6) & 0b1) == 1;
			v8 = ((pByte >> 7) & 0b1) == 1;
		}

		// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/IO/BinaryReader.cs
		public int Read7BitEncodedInt()
		{
			// Unlike writing, we can't delegate to the 64-bit read on
			// 64-bit platforms. The reason for this is that we want to
			// stop consuming bytes if we encounter an integer overflow.

			uint result = 0;
			byte byteReadJustNow;

			// Read the integer 7 bits at a time. The high bit
			// of the byte when on means to continue reading more bytes.
			//
			// There are two failure cases: we've read more than 5 bytes,
			// or the fifth byte is about to cause integer overflow.
			// This means that we can read the first 4 bytes without
			// worrying about integer overflow.

			const int MaxBytesWithoutOverflow = 4;
			for (int shift = 0; shift < MaxBytesWithoutOverflow * 7; shift += 7)
			{
				// ReadByte handles end of stream cases for us.
				byteReadJustNow = ReadByte();
				result |= (byteReadJustNow & 0x7Fu) << shift;

				if (byteReadJustNow <= 0x7Fu)
				{
					return (int)result; // early exit
				}
			}

			// Read the 5th byte. Since we already read 28 bits,
			// the value of this byte must fit within 4 bits (32 - 28),
			// and it must not have the high bit set.

			byteReadJustNow = ReadByte();
			if (byteReadJustNow > 0b_1111u)
			{
				throw new FormatException("SR.Format_Bad7BitInt");
			}

			result |= (uint)byteReadJustNow << (MaxBytesWithoutOverflow * 7);
			return (int)result;
		}

		// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/IO/BinaryReader.cs
		public long Read7BitEncodedInt64()
		{
			ulong result = 0;
			byte byteReadJustNow;

			// Read the integer 7 bits at a time. The high bit
			// of the byte when on means to continue reading more bytes.
			//
			// There are two failure cases: we've read more than 10 bytes,
			// or the tenth byte is about to cause integer overflow.
			// This means that we can read the first 9 bytes without
			// worrying about integer overflow.

			const int MaxBytesWithoutOverflow = 9;
			for (int shift = 0; shift < MaxBytesWithoutOverflow * 7; shift += 7)
			{
				// ReadByte handles end of stream cases for us.
				byteReadJustNow = ReadByte();
				result |= (byteReadJustNow & 0x7Ful) << shift;

				if (byteReadJustNow <= 0x7Fu)
				{
					return (long)result; // early exit
				}
			}

			// Read the 10th byte. Since we already read 63 bits,
			// the value of this byte must fit within 1 bit (64 - 63),
			// and it must not have the high bit set.

			byteReadJustNow = ReadByte();
			if (byteReadJustNow > 0b_1u)
			{
				throw new FormatException("SR.Format_Bad7BitInt");
			}

			result |= (ulong)byteReadJustNow << (MaxBytesWithoutOverflow * 7);
			return (long)result;
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

		public void Clear()
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

		private bool ThrowIfNotEnoughSpace(int size)
		{
			if (Position + size > Buffer.Length)
			{
				OmniLogger.PrintError($"DataReader: Insufficient space to write {size} bytes. Current position: {Position} | requested position: {Position + size} | buffer length: {Buffer.Length}");
				return false;
			}
			return true;
		}
	}
}