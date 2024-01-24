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
	public class DataWriter : IDataWriter
	{
		public byte[] Buffer { get; }
		public int Position { get; private set; }
		public int BytesWritten { get; private set; }

		public DataWriter(int size)
		{
			Buffer = new byte[size];
		}

		public void Write(byte[] buffer, int offset, int count)
		{
			int available = count - offset;
			for (int i = 0; i < available; i++)
			{
				Write(buffer[offset + i]);
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

		public void Write7BitEncodedInt(int value)
		{
			// Write out an int 7 bits at a time.  The high bit of the byte,
			// when on, tells reader to continue reading more bytes.
			uint v = (uint)value;   // support negative numbers
			while (v >= 0x80)
			{
				Write((byte)(v | 0x80));
				v >>= 7;
			}
			Write((byte)v);
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

		public void Recycle()
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
