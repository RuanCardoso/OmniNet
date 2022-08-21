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

namespace Neutron.Core
{
    public sealed class ByteStream
    {
        internal bool isRawBytes;
        private readonly byte[] buffer;
        private int position;
        private int bytesWritten;
        private DateTime lastWriteTime;

        public byte[] Buffer => buffer;
        public int BytesWritten => bytesWritten;
        public int BytesAvailable => bytesWritten - position;
        public DateTime LastWriteTime => lastWriteTime;
        public int Position { get => position; set => position = value; }

        public ByteStream(int size) => buffer = new byte[size];
        public void Write(byte value)
        {
            ThrowIfNotEnoughSpace(sizeof(byte));
            buffer[position++] = value;
            bytesWritten += sizeof(byte);
        }

        internal void WritePacket(MessageType value)
        {
            if (position != 0 || bytesWritten != 0)
                throw new Exception($"The ByteStream is not empty. Position: {position}, BytesWritten: {bytesWritten}");
            Write((byte)value);
        }

        public void Write(int value)
        {
            Write((byte)value);
            Write((byte)(value >> 8));
            Write((byte)(value >> 16));
            Write((byte)(value >> 24));
        }

        public void Write(uint value)
        {
            Write((byte)value);
            Write((byte)(value >> 8));
            Write((byte)(value >> 16));
            Write((byte)(value >> 24));
        }

        public void Write(short value)
        {
            Write((byte)value);
            Write((byte)(value >> 8));
        }

        public void Write(ushort value)
        {
            Write((byte)value);
            Write((byte)(value >> 8));
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

        public void Write(byte[] value, int offset, int size)
        {
            int available = size - offset;
            for (int i = 0; i < available; i++)
                Write(value[offset + i]);
        }

        public void Write(ByteStream value)
        {
            Write(value.buffer, 0, value.bytesWritten);
        }

        public void SetLastWriteTime() => lastWriteTime = DateTime.UtcNow;

        public void EndWrite()
        {
            isRawBytes = false;
            position = 0;
            bytesWritten = 0;
        }

        public byte ReadByte()
        {
            ThrowIfNotEnoughData(sizeof(byte));
            return buffer[position++];
        }

        internal MessageType ReadPacket()
        {
            return (MessageType)ReadByte();
        }

        public int ReadInt()
        {
            int value = ReadByte();
            value |= ReadByte() << 8;
            value |= ReadByte() << 16;
            value |= ReadByte() << 24;
            return value;
        }

        public uint ReadUInt()
        {
            uint value = ReadByte();
            value |= (uint)ReadByte() << 8;
            value |= (uint)ReadByte() << 16;
            value |= (uint)ReadByte() << 24;
            return value;
        }

        public short ReadShort()
        {
            short value = ReadByte();
            value |= (short)(ReadByte() << 8);
            return value;
        }

        public ushort ReadUShort()
        {
            ushort value = ReadByte();
            value |= (ushort)(ReadByte() << 8);
            return value;
        }

        public long ReadLong()
        {
            long value = ReadByte();
            value |= (long)ReadByte() << 8;
            value |= (long)ReadByte() << 16;
            value |= (long)ReadByte() << 24;
            value |= (long)ReadByte() << 32;
            value |= (long)ReadByte() << 40;
            value |= (long)ReadByte() << 48;
            value |= (long)ReadByte() << 56;
            return value;
        }

        public void Read(byte[] value, int offset, int size)
        {
            int available = size - offset;
            for (int i = 0; i < available; i++)
                value[offset + i] = ReadByte();
        }

        private void ThrowIfNotEnoughSpace(int size)
        {
            if (position + size > buffer.Length)
                throw new System.Exception($"Byte Stream: Not enough space to write!");
        }

        private void ThrowIfNotEnoughData(int size)
        {
            if (position + size > bytesWritten)
                throw new System.Exception($"Byte Stream: Not enough data to read!");
        }

        static ByteStreamPool byteStreams = new();
        public static ByteStream Get() => byteStreams.Get();
        public void Release() => byteStreams.Release(this);
    }
}