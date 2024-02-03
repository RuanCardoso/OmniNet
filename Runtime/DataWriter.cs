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
using System.Runtime.InteropServices;
using System.Text;

namespace Omni.Core
{
	public class DataWriter : IDataWriter
	{
		public byte[] Buffer { get; }
		public int Position { get; private set; }
		public int BytesWritten { get; private set; }
		public Encoding Encoding { get; private set; }

		public DataWriter(int size)
		{
			Buffer = new byte[size];
			Encoding = Encoding.UTF8;
		}

		public void Write(byte[] buffer, int offset, int count)
		{
			int available = count - offset;
			for (int i = 0; i < available; i++)
			{
				Write(buffer[offset + i]);
			}
		}

		public void Write(Span<byte> value)
		{
			for (int i = 0; i < value.Length; i++)
			{
				Write(value[i]);
			}
		}

		public void Write(byte value)
		{
			if (ThrowIfNotEnoughSpace(1))
			{
				Buffer[Position++] = value;
				BytesWritten += 1;
			}
		}

		public void Write(short value)
		{
			Write((byte)value);
			Write((byte)(value >> 8));
		}

		public void Write(int value)
		{
			Write((byte)value);
			Write((byte)(value >> 8));
			Write((byte)(value >> 16));
			Write((byte)(value >> 24));
		}

		public void Write(long value)
		{
			Write((byte)value);
			Write((byte)(value >> 8));
			Write((byte)(value >> 16));
			Write((byte)(value >> 24));
			Write((byte)(value >> 32));
			Write((byte)(value >> 40));
			Write((byte)(value >> 48));
			Write((byte)(value >> 56));
		}

		public unsafe void Write(double value)
		{
			ulong TmpValue = *(ulong*)&value;
			Write((byte)TmpValue);
			Write((byte)(TmpValue >> 8));
			Write((byte)(TmpValue >> 16));
			Write((byte)(TmpValue >> 24));
			Write((byte)(TmpValue >> 32));
			Write((byte)(TmpValue >> 40));
			Write((byte)(TmpValue >> 48));
			Write((byte)(TmpValue >> 56));
		}

		public unsafe void Write(float value)
		{
			uint TmpValue = *(uint*)&value;
			Write((byte)TmpValue);
			Write((byte)(TmpValue >> 8));
			Write((byte)(TmpValue >> 16));
			Write((byte)(TmpValue >> 24));
		}

		public void Write(decimal value)
		{
			int[] bits = decimal.GetBits(value);
			for (int i = 0; i < bits.Length; i++)
			{
				Write(bits[i]);
			}
		}

		public void Write(sbyte value)
		{
			Write((byte)value);
		}

		public void Write(ushort value)
		{
			Write((byte)value);
			Write((byte)(value >> 8));
		}

		public void Write(uint value)
		{
			Write((byte)value);
			Write((byte)(value >> 8));
			Write((byte)(value >> 16));
			Write((byte)(value >> 24));
		}

		public void Write(ulong value)
		{
			Write((byte)value);
			Write((byte)(value >> 8));
			Write((byte)(value >> 16));
			Write((byte)(value >> 24));
			Write((byte)(value >> 32));
			Write((byte)(value >> 40));
			Write((byte)(value >> 48));
			Write((byte)(value >> 56));
		}

		/// <summary>
		/// Writes a string to the data stream.
		/// </summary>
		/// <param name="value">The string to write.</param>
		public void Write(string value)
		{
			int length = Encoding.GetByteCount(value);
			byte[] encoded = Encoding.GetBytes(value);
			Write7BitEncodedInt(length);
			Write(encoded, 0, length);
		}

		/// <summary>
		/// Writes a string to the data stream without incurring memory allocations.
		/// </summary>
		/// <remarks>
		/// This method efficiently utilizes the stack memory using STACKALLOC and is designed to handle small strings.
		/// To minimize memory overhead, it is recommended to avoid using this method for excessively large strings.
		/// </remarks>
		/// <param name="value">The string to be written to the data stream.</param>
		/// <exception cref="StackOverflowException">Thrown when the string is too large to be written to the STACK.</exception>
		public void WriteWithoutAllocation(string value)
		{
			int length = Encoding.GetByteCount(value);
			Span<byte> encoded = stackalloc byte[length];
			int encodedBytes = Encoding.GetBytes(value, encoded);
			if (encodedBytes != length)
				throw new Exception("Error: The string could not be written to the data stream.");
			Write7BitEncodedInt(length);
			Write(encoded);
		}

		/// <summary>
		/// Serializes the given object using a custom serialization method provided by the ISyncCustom interface.
		/// </summary>
		/// <typeparam name="T">The type of the object to serialize.</typeparam>
		/// <param name="ISyncCustom">The ISyncCustom object that provides the custom serialization method.</param>
		public void SerializeWithCustom<T>(ISyncCustom ISyncCustom) where T : class => ISyncCustom.Serialize(this);
		/// Serializes the given data object to JSON format using Json.NET and writes it to the output stream.
		/// </summary>
		/// <typeparam name="T">The type of the data object to serialize.</typeparam>
		/// <param name="data">The data object to serialize.</param>
		/// <param name="options">Optional settings to use during serialization.</param>
		public void SerializeWithJsonNet<T>(T data, JsonSerializerSettings options = null) => Write(JsonConvert.SerializeObject(data, options));
		/// <summary>
		/// Serializes the given data using MessagePack format and writes it to the stream.
		/// </summary>
		/// <typeparam name="T">The type of the data to be serialized.</typeparam>
		/// <param name="data">The data to be serialized.</param>
		/// <param name="options">The options to use for serialization. (Optional)</param>
		public void SerializeWithMsgPack<T>(T data, MessagePackSerializerOptions options = null)
		{
			byte[] _data_ = MessagePackSerializer.Serialize(data, options);
			int length = _data_.Length;
			Write7BitEncodedInt(length);
			Write(_data_, 0, length);
		}

		/// <summary>
		/// Writes a boolean value to the data stream.
		/// </summary>
		/// <param name="value">The boolean value to write.</param>
		public unsafe void Write(bool value) => Write(*(byte*)&value);
		/// <summary>
		/// Writes two boolean values to the underlying stream as a single byte.
		/// </summary>
		/// <param name="v1">The first boolean value to write.</param>
		/// <param name="v2">The second boolean value to write.</param>
		public unsafe void Write(bool v1, bool v2) => Write((byte)(*(byte*)&v1 | *(byte*)&v2 << 1));
		/// <summary>
		/// Writes three boolean values to the stream as a single byte.
		/// </summary>
		/// <param name="v1">The first boolean value to write.</param>
		/// <param name="v2">The second boolean value to write.</param>
		/// <param name="v3">The third boolean value to write.</param>
		public unsafe void Write(bool v1, bool v2, bool v3) => Write((byte)(*(byte*)&v1 | *(byte*)&v2 << 1 | *(byte*)&v3 << 2));
		/// <summary>
		/// Writes four boolean values into a byte and writes it to the stream.
		/// </summary>
		/// <param name="v1">The first boolean value.</param>
		/// <param name="v2">The second boolean value.</param>
		/// <param name="v3">The third boolean value.</param>
		/// <param name="v4">The fourth boolean value.</param>
		public unsafe void Write(bool v1, bool v2, bool v3, bool v4) => Write((byte)(*(byte*)&v1 | *(byte*)&v2 << 1 | *(byte*)&v3 << 2 | *(byte*)&v4 << 3));
		/// <summary>
		/// Writes five boolean values to the underlying buffer as a single byte.
		/// </summary>
		/// <param name="v1">The first boolean value to write.</param>
		/// <param name="v2">The second boolean value to write.</param>
		/// <param name="v3">The third boolean value to write.</param>
		/// <param name="v4">The fourth boolean value to write.</param>
		/// <param name="v5">The fifth boolean value to write.</param>
		public unsafe void Write(bool v1, bool v2, bool v3, bool v4, bool v5) => Write((byte)(*(byte*)&v1 | *(byte*)&v2 << 1 | *(byte*)&v3 << 2 | *(byte*)&v4 << 3 | *(byte*)&v5 << 4));
		/// <summary>
		/// Writes a byte composed of 6 boolean values.
		/// </summary>
		/// <param name="v1">The first boolean value.</param>
		/// <param name="v2">The second boolean value.</param>
		/// <param name="v3">The third boolean value.</param>
		/// <param name="v4">The fourth boolean value.</param>
		/// <param name="v5">The fifth boolean value.</param>
		/// <param name="v6">The sixth boolean value.</param>
		public unsafe void Write(bool v1, bool v2, bool v3, bool v4, bool v5, bool v6) => Write((byte)(*(byte*)&v1 | *(byte*)&v2 << 1 | *(byte*)&v3 << 2 | *(byte*)&v4 << 3 | *(byte*)&v5 << 4 | *(byte*)&v6 << 5));
		/// <summary>
		/// Writes a byte composed of 7 boolean values.
		/// </summary>
		/// <param name="v1">The first boolean value.</param>
		/// <param name="v2">The second boolean value.</param>
		/// <param name="v3">The third boolean value.</param>
		/// <param name="v4">The fourth boolean value.</param>
		/// <param name="v5">The fifth boolean value.</param>
		/// <param name="v6">The sixth boolean value.</param>
		/// <param name="v7">The seventh boolean value.</param>
		public unsafe void Write(bool v1, bool v2, bool v3, bool v4, bool v5, bool v6, bool v7) => Write((byte)(*(byte*)&v1 | *(byte*)&v2 << 1 | *(byte*)&v3 << 2 | *(byte*)&v4 << 3 | *(byte*)&v5 << 4 | *(byte*)&v6 << 5 | *(byte*)&v7 << 6));
		/// <summary>
		/// Writes a byte value composed of 8 boolean values.
		/// </summary>
		/// <param name="v1">The first boolean value.</param>
		/// <param name="v2">The second boolean value.</param>
		/// <param name="v3">The third boolean value.</param>
		/// <param name="v4">The fourth boolean value.</param>
		/// <param name="v5">The fifth boolean value.</param>
		/// <param name="v6">The sixth boolean value.</param>
		/// <param name="v7">The seventh boolean value.</param>
		/// <param name="v8">The eighth boolean value.</param>
		public unsafe void Write(bool v1, bool v2, bool v3, bool v4, bool v5, bool v6, bool v7, bool v8) => Write((byte)(*(byte*)&v1 | *(byte*)&v2 << 1 | *(byte*)&v3 << 2 | *(byte*)&v4 << 3 | *(byte*)&v5 << 4 | *(byte*)&v6 << 5 | *(byte*)&v7 << 6 | *(byte*)&v8 << 7));

		// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/IO/BinaryWriter.cs
		public void Write7BitEncodedInt(int value)
		{
			uint uValue = (uint)value;

			// Write out an int 7 bits at a time. The high bit of the byte,
			// when on, tells reader to continue reading more bytes.
			//
			// Using the constants 0x7F and ~0x7F below offers smaller
			// codegen than using the constant 0x80.

			while (uValue > 0x7Fu)
			{
				Write((byte)(uValue | ~0x7Fu));
				uValue >>= 7;
			}

			Write((byte)uValue);
		}

		// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/IO/BinaryWriter.cs
		public void Write7BitEncodedInt64(long value)
		{
			ulong uValue = (ulong)value;

			// Write out an int 7 bits at a time. The high bit of the byte,
			// when on, tells reader to continue reading more bytes.
			//
			// Using the constants 0x7F and ~0x7F below offers smaller
			// codegen than using the constant 0x80.

			while (uValue > 0x7Fu)
			{
				Write((byte)((uint)uValue | ~0x7Fu));
				uValue >>= 7;
			}

			Write((byte)uValue);
		}

		public unsafe void Marshalling_Write<T>(T structure) where T : struct
		{
			byte[] byteArray = new byte[Marshal.SizeOf(structure)];
			fixed (byte* byteArrayPtr = byteArray)
			{
				Marshal.StructureToPtr(structure, (IntPtr)byteArrayPtr, true);
			}
			Write7BitEncodedInt(byteArray.Length);
			Write(byteArray, 0, byteArray.Length);
		}

		public void Clear()
		{
			Position = 0;
			BytesWritten = 0;
		}

		private bool ThrowIfNotEnoughSpace(int size)
		{
			if (Position + size > Buffer.Length)
			{
				OmniLogger.PrintError($"Insufficient space to write {size} bytes. Current position: {Position}, requested position: {Position + size}");
				return false;
			}

			return true;
		}
	}
}
