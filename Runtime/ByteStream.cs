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

#if UNITY_SERVER && !UNITY_EDITOR
using System.Threading;
#endif

using MessagePack;
using Newtonsoft.Json;
using System;
using UnityEngine;
using static Omni.Core.Enums;

namespace Omni.Core
{
    public sealed class ByteStream
    {
        internal bool isRawBytes;

        private int position;
        private int bytesWritten;
        private bool isAcked;
        private readonly byte[] buffer;
        private readonly bool isPoolObject;
        private DateTime lastWriteTime;

        internal bool IsAcked
        {
            get => isAcked;
            set => isAcked = value;
        }
        internal DateTime LastWriteTime => lastWriteTime;

        public int Position
        {
            get => position;
            set => position = value;
        }
        public int BytesWritten => bytesWritten;
        public int BytesRemaining => bytesWritten - position;
        public byte[] Buffer => buffer;

#if UNITY_SERVER && !UNITY_EDITOR
        static int allocated = 0;
#endif
        internal static ByteStreamPool bsPool;
        internal void SetLastWriteTime() => lastWriteTime = DateTime.UtcNow;
        public ByteStream(int size, bool isPoolObject = false)
        {
            buffer = new byte[size];
            this.isPoolObject = isPoolObject;
#if UNITY_SERVER && !UNITY_EDITOR
            Logger.Inline($"Allocated: {Interlocked.Increment(ref allocated)} ByteStream!\r\n");
#endif
        }

        public void Write()
        {
            isRawBytes = false;
            position = 0;
            bytesWritten = 0;
        }

        public void Write(byte value)
        {
            if (ThrowIfNotEnoughSpace(sizeof(byte)))
            {
                buffer[position++] = value;
                bytesWritten += sizeof(byte);
            }
        }

        internal void WritePayload(Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode)
        {
            // Packed to optimize bandwidth!
            byte payload = (byte)((byte)channel | (byte)target << 1 | (byte)subTarget << 3 | (byte)cacheMode << 4);
            Write(payload);
        }

        public unsafe void Write(bool value) => Write(*(byte*)&value);
        public unsafe void Write(bool v1, bool v2) => Write((byte)(*(byte*)&v1 | *(byte*)&v2 << 1));
        public unsafe void Write(bool v1, bool v2, bool v3) => Write((byte)(*(byte*)&v1 | *(byte*)&v2 << 1 | *(byte*)&v3 << 2));
        public unsafe void Write(bool v1, bool v2, bool v3, bool v4) => Write((byte)(*(byte*)&v1 | *(byte*)&v2 << 1 | *(byte*)&v3 << 2 | *(byte*)&v4 << 3));
        public unsafe void Write(bool v1, bool v2, bool v3, bool v4, bool v5) => Write((byte)(*(byte*)&v1 | *(byte*)&v2 << 1 | *(byte*)&v3 << 2 | *(byte*)&v4 << 3 | *(byte*)&v5 << 4));

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

        internal void WritePacket(MessageType value)
        {
            if (position != 0 || bytesWritten != 0)
            {
                Logger.PrintError($"The ByteStream is not empty -> Position: {position} | BytesWritten: {bytesWritten}");
            }
            else
            {
                Write((byte)value);
            }
        }

        public void Write(Vector3 vector)
        {
            Write(vector.x);
            Write(vector.y);
            Write(vector.z);
        }

        public void Write(Vector2 vector)
        {
            Write(vector.x);
            Write(vector.y);
        }

        public void Write(Quaternion quaternion)
        {
            Write(quaternion.x);
            Write(quaternion.y);
            Write(quaternion.z);
            Write(quaternion.w);
        }

        public void Write(Color color)
        {
            Write(color.r);
            Write(color.g);
            Write(color.b);
            Write(color.a);
        }

        public void Write(Color32 color)
        {
            Write(color.r);
            Write(color.g);
            Write(color.b);
            Write(color.a);
        }

        public void SerializeWithCustom<T>(ISyncCustom ISyncCustom) where T : class => ISyncCustom.Serialize(this);
        public void SerializeWithJsonNet<T>(T data, JsonSerializerSettings options = null) => Write(JsonConvert.SerializeObject(data, options));
        public void SerializeWithMsgPack<T>(T data, MessagePackSerializerOptions options = null)
        {
            byte[] _data_ = MessagePackSerializer.Serialize(data, options);
            int length = _data_.Length;
            Write7BitEncodedInt(length);
            Write(_data_, 0, length);
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

        public void Write(string value)
        {
            var encoding = OmniNetwork.Instance.Encoding;
            int length = encoding.GetByteCount(value);
            byte[] encoded = encoding.GetBytes(value);
            // write the length of string.
            Write7BitEncodedInt(length);
            // write the string.
            Write(encoded, 0, length);
        }

        public void Write(Span<byte> value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i]);
            }
        }

        public void Write(Memory<byte> value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                Write(value.Span[i]);
            }
        }

        public void Write(ReadOnlySpan<byte> value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i]);
            }
        }

        public void Write(ReadOnlyMemory<byte> value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                Write(value.Span[i]);
            }
        }

        public void Write(byte[] value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i]);
            }
        }

        public void Write(byte[] value, int offset, int size)
        {
            int available = size - offset;
            for (int i = 0; i < available; i++)
            {
                Write(value[offset + i]);
            }
        }

        public void Write(ByteStream value)
        {
            Write(value.buffer, 0, value.bytesWritten);
        }

        public void WriteRemainingBytes(ByteStream parameters)
        {
            Write(parameters, parameters.position, parameters.bytesWritten);
        }

        public void Write(ByteStream value, int offset, int size)
        {
            Write(value.buffer, offset, size);
        }

        public byte ReadByte() => ThrowIfNotEnoughData(sizeof(byte)) ? buffer[position++] : default;
        internal void ReadPayload(out Channel channel, out Target target, out SubTarget subTarget, out CacheMode cacheMode)
        {
            byte bPackedPayload = ReadByte();
            channel = (Channel)(bPackedPayload & 0x1);
            target = (Target)((bPackedPayload >> 1) & 0x3);
            subTarget = (SubTarget)((bPackedPayload >> 3) & 0x1);
            cacheMode = (CacheMode)((bPackedPayload >> 4) & 0x3);
        }

        internal MessageType ReadPacket()
        {
            return (MessageType)ReadByte();
        }

        public bool ReadBool() => ReadByte() == 1;
        public void ReadBool(out bool v1, out bool v2)
        {
            v1 = ReadBool();
            v2 = ReadBool();
        }

        public void ReadBool(out bool v1, out bool v2, out bool v3)
        {
            v1 = ReadBool();
            v2 = ReadBool();
            v3 = ReadBool();
        }

        public void ReadBool(out bool v1, out bool v2, out bool v3, out bool v4)
        {
            v1 = ReadBool();
            v2 = ReadBool();
            v3 = ReadBool();
            v4 = ReadBool();
        }

        public void ReadBool(out bool v1, out bool v2, out bool v3, out bool v4, out bool v5)
        {
            v1 = ReadBool();
            v2 = ReadBool();
            v3 = ReadBool();
            v4 = ReadBool();
            v5 = ReadBool();
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

        public Vector3 ReadVector3()
        {
            float x = ReadFloat();
            float y = ReadFloat();
            float z = ReadFloat();
            return new Vector3(x, y, z);
        }

        public Vector3 ReadVector2()
        {
            float x = ReadFloat();
            float y = ReadFloat();
            return new Vector2(x, y);
        }

        public Quaternion ReadQuaternion()
        {
            float x = ReadFloat();
            float y = ReadFloat();
            float z = ReadFloat();
            float w = ReadFloat();
            return new Quaternion(x, y, z, w);
        }

        public Color ReadColor()
        {
            float r = ReadFloat();
            float g = ReadFloat();
            float b = ReadFloat();
            float a = ReadFloat();
            return new Color(r, g, b, a);
        }

        public Color ReadColor32()
        {
            byte r = ReadByte();
            byte g = ReadByte();
            byte b = ReadByte();
            byte a = ReadByte();
            return new Color32(r, g, b, a);
        }

        public void DeserializeWithCustom<T>(ISyncCustom ISyncCustom) where T : class => ISyncCustom.Deserialize(this);
        public T DeserializeWithJsonNet<T>(JsonSerializerSettings options = null) => JsonConvert.DeserializeObject<T>(ReadString(), options);
        public T DeserializeWithMsgPack<T>(MessagePackSerializerOptions options = null)
        {
            int length = Read7BitEncodedInt();
            byte[] _data_ = new byte[length];
            Read(_data_, 0, length);
            return MessagePackSerializer.Deserialize<T>(_data_, options);
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
            value |= (uint)(ReadByte() << 8);
            value |= (uint)(ReadByte() << 16);
            value |= (uint)(ReadByte() << 24);
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

        public long ReadLong()
        {
            long value = ReadByte();
            value |= (long)(ReadByte() << 8);
            value |= (long)(ReadByte() << 16);
            value |= (long)(ReadByte() << 24);
            value |= (long)(ReadByte() << 32);
            value |= (long)(ReadByte() << 40);
            value |= (long)(ReadByte() << 48);
            value |= (long)(ReadByte() << 56);
            return value;
        }

        public string ReadString()
        {
            var encoding = OmniNetwork.Instance.Encoding;
            // Read the length of string.
            int length = Read7BitEncodedInt();
            // Initialize new Matrix with the specified length.
            byte[] encoded = new byte[length];
            Read(encoded, 0, length);
            // Create new string with the readed bytes.
            return new string(encoding.GetString(encoded));
        }

        public void Read(byte[] value, int offset, int size)
        {
            int available = size - offset;
            for (int i = 0; i < available; i++)
            {
                value[offset + i] = ReadByte();
            }
        }

        #region Slice Memory
        public ReadOnlyMemory<byte> ReadAsReadOnlyMemory()
        {
            ReadOnlyMemory<byte> _ = buffer;
            return _[position..bytesWritten];
        }

        public ReadOnlyMemory<byte> ReadAsReadOnlyMemory(int offset, int size)
        {
            ReadOnlyMemory<byte> _ = buffer;
            return _[offset..size];
        }

        public ReadOnlyMemory<byte> ReadAsReadOnlyMemory(int size)
        {
            ReadOnlyMemory<byte> _ = buffer;
            return _[position..size];
        }

        public ReadOnlySpan<byte> ReadAsReadOnlySpan()
        {
            ReadOnlySpan<byte> _ = buffer;
            return _[position..bytesWritten];
        }

        public ReadOnlySpan<byte> ReadAsReadOnlySpan(int offset, int size)
        {
            ReadOnlySpan<byte> _ = buffer;
            return _[offset..size];
        }

        public ReadOnlySpan<byte> ReadAsReadOnlySpan(int size)
        {
            ReadOnlySpan<byte> _ = buffer;
            return _[position..size];
        }

        public Memory<byte> ReadAsMemory()
        {
            Memory<byte> _ = buffer;
            return _[position..bytesWritten];
        }

        public Memory<byte> ReadAsMemory(int offset, int size)
        {
            Memory<byte> _ = buffer;
            return _[offset..size];
        }

        public Memory<byte> ReadAsMemory(int size)
        {
            Memory<byte> _ = buffer;
            return _[position..size];
        }

        public Span<byte> ReadAsSpan()
        {
            Span<byte> _ = buffer;
            return _[position..bytesWritten];
        }

        public Span<byte> ReadAsSpan(int offset, int size)
        {
            Span<byte> _ = buffer;
            return _[offset..size];
        }

        public Span<byte> ReadAsSpan(int size)
        {
            Span<byte> _ = buffer;
            return _[position..size];
        }
        #endregion

        private bool ThrowIfNotEnoughSpace(int size)
        {
            if (position + size > buffer.Length)
            {
                Logger.PrintError($"Byte Stream: Not enough space to write! you are writing {size} bytes -> pos: {position + size}");
                return false;
            }

            return true;
        }

        private bool ThrowIfNotEnoughData(int size)
        {
            if (position + size > bytesWritten)
            {
                Logger.PrintError($"Byte Stream: Not enough data to read!");
                return false;
            }

            return true;
        }

        private static void ThrowIfNotInitialized()
        {
            if (bsPool == null)
            {
                Logger.PrintError("Omni is not initialized?");
            }
        }

        public static ByteStream Get()
        {
            ThrowIfNotInitialized();
            ByteStream _get_ = bsPool.Get();
            _get_.isRelease = false;
            if (_get_.position != 0 || _get_.bytesWritten != 0 || !_get_.isPoolObject)
                Logger.PrintError($"The ByteStream is not empty -> Position: {_get_.position} | BytesWritten: {_get_.bytesWritten}. Maybe you are modifying a ByteStream that is being used by another thread? or are you using a ByteStream that has already been released?");
            return _get_;
        }

        internal static ByteStream Get(MessageType msgType)
        {
            ThrowIfNotInitialized();
            ByteStream _get_ = bsPool.Get();
            _get_.isRelease = false;
            if (_get_.position != 0 || _get_.bytesWritten != 0 || !_get_.isPoolObject)
                Logger.PrintError($"The ByteStream is not empty -> Position: {_get_.position} | BytesWritten: {_get_.bytesWritten}. Maybe you are modifying a ByteStream that is being used by another thread? or are you using a ByteStream that has already been released?");
            else _get_.WritePacket(msgType);
            return _get_;
        }

#pragma warning disable IDE0060
        internal static ByteStream Get(MessageType msgType, bool isEmpty)
#pragma warning restore IDE0060
        {
            ThrowIfNotInitialized();
            ByteStream _get_ = bsPool.Get();
            _get_.isRelease = false;
            if (_get_.position != 0 || _get_.bytesWritten != 0 || !_get_.isPoolObject)
                Logger.PrintError($"The ByteStream is not empty -> Position: {_get_.position} | BytesWritten: {_get_.bytesWritten}. Maybe you are modifying a ByteStream that is being used by another thread? or are you using a ByteStream that has already been released?");
            else
            {
                _get_.WritePacket(msgType);
                _get_.Write((byte)1);
            }
            return _get_;
        }

        internal bool isRelease = false;
        internal void Release()
        {
            if (isPoolObject)
            {
                if (isRelease)
                {
                    Logger.PrintError($"The ByteStream is already released!");
                }
                else
                {
                    isRelease = true;
                    bsPool.Release(this);
                }
            }
            else
            {
                Write();
            }
        }
    }
}