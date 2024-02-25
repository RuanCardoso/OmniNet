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
using System.Text;

namespace Omni.Core
{
	public interface IDataWriter
	{
		byte[] Buffer { get; }
		int Position { get; set; }
		int BytesWritten { get; set; }
		bool IsReleased { get; set; }
		Encoding Encoding { get; set; }
		void Clear();
		void Write(byte[] buffer, int offset, int count);
		void Write(Span<byte> value);
		void Write(ReadOnlySpan<byte> value);
		void Write(Stream value);
		void Write(char value);
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
		void Write(string value);
		void WriteWithoutAllocation(string value);
		void SerializeWithCustom<T>(ISyncCustom ISyncCustom) where T : class;
		void SerializeWithJsonNet<T>(T data, JsonSerializerSettings options = null);
		void SerializeWithMsgPack<T>(T data, MessagePackSerializerOptions options = null);
		void Write(bool value);
		void Write(bool v1, bool v2);
		void Write(bool v1, bool v2, bool v3);
		void Write(bool v1, bool v2, bool v3, bool v4);
		void Write(bool v1, bool v2, bool v3, bool v4, bool v5);
		void Write(bool v1, bool v2, bool v3, bool v4, bool v5, bool v6);
		void Write(bool v1, bool v2, bool v3, bool v4, bool v5, bool v6, bool v7);
		void Write(bool v1, bool v2, bool v3, bool v4, bool v5, bool v6, bool v7, bool v8);
		void Write7BitEncodedInt(int value);
		void Write7BitEncodedInt64(long value);
		void Marshalling_Write<T>(T structure) where T : struct;

	}
}