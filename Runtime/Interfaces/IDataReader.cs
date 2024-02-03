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
using System.Text;

namespace Omni.Core
{
	public interface IDataReader
	{
		byte[] Buffer { get; }
		int Position { get; }
		int BytesWritten { get; }
		Encoding Encoding { get; }
		void Recycle();
		void Write(byte[] buffer, int offset, int count);
		void Read(byte[] buffer, int offset, int count);
		int Read(Span<byte> value);
		T ReadCustomMessage<T>() where T : unmanaged, IComparable, IConvertible, IFormattable;
		byte ReadByte();
		short ReadShort();
		int ReadInt();
		long ReadLong();
		double ReadDouble();
		float ReadFloat();
		decimal ReadDecimal();
		sbyte ReadSByte();
		ushort ReadUShort();
		string ReadString();
		string ReadStringWithoutAllocation();
		void DeserializeWithCustom<T>(ISyncCustom ISyncCustom) where T : class;
		T DeserializeWithJsonNet<T>(JsonSerializerSettings options);
		T DeserializeWithMsgPack<T>(MessagePackSerializerOptions options);
		uint ReadUInt();
		ulong ReadULong();
		bool ReadBool();
		void ReadBool(out bool v1, out bool v2);
		void ReadBool(out bool v1, out bool v2, out bool v3);
		void ReadBool(out bool v1, out bool v2, out bool v3, out bool v4);
		void ReadBool(out bool v1, out bool v2, out bool v3, out bool v4, out bool v5);
		void ReadBool(out bool v1, out bool v2, out bool v3, out bool v4, out bool v5, out bool v6);
		void ReadBool(out bool v1, out bool v2, out bool v3, out bool v4, out bool v5, out bool v6, out bool v7);
		void ReadBool(out bool v1, out bool v2, out bool v3, out bool v4, out bool v5, out bool v6, out bool v7, out bool v8);
		int Read7BitEncodedInt();
		long Read7BitEncodedInt64();
		T Marshalling_ReadStructure<T>() where T : struct;

	}
}