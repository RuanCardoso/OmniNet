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
	public interface IDataReader
	{
		byte[] Buffer { get; }
		int Position { get; }
		int BytesWritten { get; }
		void Recycle();
		void Write(byte[] buffer, int offset, int count);
		void Read(byte[] buffer, int offset, int count);
		byte ReadByte();
		short ReadShort();
		int ReadInt();
		long ReadLong();
		double ReadDouble();
		float ReadFloat();
		decimal ReadDecimal();
		sbyte ReadSByte();
		ushort ReadUShort();
		uint ReadUInt();
		ulong ReadULong();
		int Read7BitEncodedInt();
		T Marshalling_ReadStructure<T>() where T : struct;
	}
}