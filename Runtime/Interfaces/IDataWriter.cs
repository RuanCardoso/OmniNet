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

namespace Omni.Core
{
	public interface IDataWriter
	{
		byte[] Buffer { get; }
		int Position { get; }
		int BytesWritten { get; }
		void Recycle();
		void Write(byte[] buffer, int offset, int count);
		void Write(byte value);
		void Write(short value);
		void Write(int value);
		void Write(long value);
		void Write(double value);
		void Write(float value);
		void Write(decimal value);
		void Write(sbyte value);
		void Write(ushort value);
		void Write(uint value);
		void Write(ulong value);
		void Write7BitEncodedInt(int value);
		void Marshalling_Write<T>(T structure) where T : struct;
	}
}