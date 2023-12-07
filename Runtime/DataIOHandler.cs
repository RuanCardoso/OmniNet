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

using Humanizer.Bytes;
using MessagePack;
using Newtonsoft.Json;
using Omni.Core.Cryptography;
using System;
using System.Text;
using UnityEngine;
using static Omni.Core.Enums;

namespace Omni.Core
{
    /// <summary>
    /// Handles input/output operations for data, providing methods to write and read data from a buffer, and to serialize and deserialize data.<br/>
    /// Also provides methods to serialize and deserialize data using Json and custom serialization.<br/>
    /// Note: Prioritize bitwise serialization for better performance.<br/>
    /// </summary>
    public sealed class DataIOHandler
    {
        private readonly bool isPoolObject;
        private readonly byte[] buffer;
        private Encoding encoding;

        private int position;
        private int bytesWritten;
        private bool isAcked;
        private DateTime lastWriteTime;

        internal bool isRawBytes;

        /// <summary>
        /// Gets or sets a value indicating whether this instance is acknowledged.
        /// Secure layer only.<br/>
        /// </summary>
        internal bool IsAcked
        {
            get => isAcked;
            set => isAcked = value;
        }

        /// <summary>
        /// Used to re-send the packet if it is not acknowledged, or to send the packet again if it is not received.<br/>
        /// Secure layer only.<br/>
        /// </summary>
        internal DateTime LastWriteTime => lastWriteTime;

        /// <summary>
        /// Gets or sets the current position in the data stream.
        /// </summary>
        public int Position
        {
            get => position;
            set => position = value;
        }

        /// <summary>
        /// The number of bytes written to the stream.
        /// </summary>
        public int BytesWritten => bytesWritten;

        /// <summary>
        /// Gets the number of bytes remaining to be read.
        /// </summary>
        public int BytesRemaining => bytesWritten - position;

        /// <summary>
        /// Gets the buffer used by the DataIOHandler.
        /// </summary>
        public byte[] Buffer => buffer;

        /// <summary>
        /// A pool of DataIOHandler objects used to reduce memory allocation.<br/>
        /// Memory allocation is very expensive and can cause performance issues, GC spikes and memory fragmentation.<br/>
        /// </summary>
        internal static DataIOHandlerPool bsPool;
        internal void SetLastWriteTime() => lastWriteTime = DateTime.UtcNow;

        /// <summary>
        /// Handles input and output of data with a buffer of a specified size.
        /// </summary>
        /// <param name="size">The size of the buffer.</param>
        /// <param name="isPoolObject">Whether this object is a pooled object.</param>
        public DataIOHandler(int size, bool isPoolObject = false, Encoding encoding = null)
        {
            buffer = new byte[size]; // Never change the size of the buffer or buffer reference.
            this.encoding = encoding ?? Encoding.ASCII; // if encoding is null, use ASCII encoding.
            this.isPoolObject = isPoolObject;
        }

        /// <summary>
        /// Resets the DataIOHandler for reuse.
        /// Current position and the number of bytes written are reset to 0.
        /// </summary>
        public void Write()
        {
            isRawBytes = false;
            position = 0;
            bytesWritten = 0;
        }

        /// <summary>
        /// Writes a byte value to the buffer.
        /// </summary>
        /// <param name="value">The byte value to write.</param>
        public void Write(byte value)
        {
            if (ThrowIfNotEnoughSpace(sizeof(byte)))
            {
                buffer[position++] = value;
                bytesWritten += sizeof(byte);
            }
        }

        /// <summary>
        /// Writes the payload to the data stream, packing the delivery mode, target, processing option and caching option into a single byte using bit shifting.
        /// </summary>
        /// <param name="deliveryMode">The delivery mode of the data.</param>
        /// <param name="target">The target of the data.</param>
        /// <param name="processingOption">The processing option of the data.</param>
        /// <param name="cachingOption">The caching option of the data.</param>
        internal void WritePayload(DataDeliveryMode deliveryMode, DataTarget target, DataProcessingOption processingOption, DataCachingOption cachingOption)
        {
            //  0000 0000
            //         || -> Delivery Mode
            //      ||   -> Target
            //    |      -> Processing Option
            // ||       -> Caching Option
            // Represents the payload in a single byte, using bit shifting.
            // Packed because it is more efficient to bandwith and performance.
            byte payload = (byte)((byte)deliveryMode | (byte)target << 2 | (byte)processingOption << 4 | (byte)cachingOption << 5);
            Write(payload);
        }

        /// <summary>
        /// Write Tcp(protocol) payload.<br/>
        /// Part of Message Framing (https://blog.stephencleary.com/2009/04/message-framing.html)
        /// </summary>
        internal void WritePayload(byte[] buffer, int size)
        {
            Write();
            Write(buffer, 0, size);
            Position = 0;
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

        /// <summary>
        /// Writes a 32-bit integer in a compressed format using 7-bit encoding.
        /// </summary>
        /// <param name="value">The 32-bit integer to be written.</param>
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
                OmniLogger.PrintError($"The IOHandler is not empty -> Position: {position} | BytesWritten: {bytesWritten}");
            }
            else
            {
                Write((byte)value);
            }
        }

        /// <summary>
        /// Writes a Vector3 to the data stream.
        /// </summary>
        /// <param name="vector">The Vector3 to write.</param>
        public void Write(Vector3 vector)
        {
            Write(vector.x);
            Write(vector.y);
            Write(vector.z);
        }

        /// <summary>
        /// Writes a Vector2 to the data stream.
        /// </summary>
        /// <param name="vector">The Vector2 to write.</param>
        public void Write(Vector2 vector)
        {
            Write(vector.x);
            Write(vector.y);
        }

        /// <summary>
        /// Writes a Quaternion to the data stream.
        /// </summary>
        /// <param name="quaternion">The Quaternion to write.</param>
        public void Write(Quaternion quaternion)
        {
            Write(quaternion.x);
            Write(quaternion.y);
            Write(quaternion.z);
            Write(quaternion.w);
        }

        /// <summary>
        /// Writes a Color value to the data stream.
        /// </summary>
        /// <param name="color">The Color value to write.</param>
        public void Write(Color color)
        {
            Write(color.r);
            Write(color.g);
            Write(color.b);
            Write(color.a);
        }

        /// <summary>
        /// Writes the specified color to the stream.
        /// </summary>
        /// <param name="color">The color to write.</param>
        public void Write(Color32 color)
        {
            Write(color.r);
            Write(color.g);
            Write(color.b);
            Write(color.a);
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
        /// Writes a 32-bit integer value to the underlying stream.
        /// </summary>
        /// <param name="value">The 32-bit integer value to write.</param>
        public void Write(int value)
        {
            Write((byte)value);
            Write((byte)(value >> 8));
            Write((byte)(value >> 16));
            Write((byte)(value >> 24));
        }

        /// <summary>
        /// Writes a 32-bit unsigned integer to the underlying stream.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(uint value)
        {
            Write((byte)value);
            Write((byte)(value >> 8));
            Write((byte)(value >> 16));
            Write((byte)(value >> 24));
        }

        /// <summary>
        /// Writes a short value to the stream.
        /// </summary>
        /// <param name="value">The short value to write.</param>
        public void Write(short value)
        {
            Write((byte)value);
            Write((byte)(value >> 8));
        }

        /// <summary>
        /// Writes a ushort value to the stream.
        /// </summary>
        /// <param name="value">The ushort value to write.</param>
        public void Write(ushort value)
        {
            Write((byte)value);
            Write((byte)(value >> 8));
        }

        /// <summary>
        /// Writes a double-precision floating-point value to the underlying stream using little-endian encoding.
        /// </summary>
        /// <param name="value">The double-precision floating-point value to write.</param>
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

        /// <summary>
        /// Writes a float value to the data stream.
        /// </summary>
        /// <param name="value">The float value to write.</param>
        public unsafe void Write(float value)
        {
            uint TmpValue = *(uint*)&value;
            Write((byte)TmpValue);
            Write((byte)(TmpValue >> 8));
            Write((byte)(TmpValue >> 16));
            Write((byte)(TmpValue >> 24));
        }

        /// <summary>
        /// Writes a long value to the data stream.
        /// </summary>
        /// <param name="value">The long value to write.</param>
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

        /// <summary>
        /// Writes a string to the data stream.
        /// </summary>
        /// <param name="value">The string to write.</param>
        public void Write(string value)
        {
            int length = encoding.GetByteCount(value);
            byte[] encoded = encoding.GetBytes(value);
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
            int length = encoding.GetByteCount(value);
            Span<byte> encoded = stackalloc byte[length];
            int encodedBytes = encoding.GetBytes(value, encoded);
            if (encodedBytes != length)
                throw new Exception("Error: The string could not be written to the data stream.");
            Write7BitEncodedInt(length);
            Write(encoded);
        }

        /// <summary>
        /// Writes a span of bytes to the data stream.
        /// </summary>
        /// <param name="value">The span of bytes to write.</param>
        public void Write(Span<byte> value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i]);
            }
        }

        /// <summary>
        /// Writes a block of memory to the output stream.
        /// </summary>
        /// <param name="value">The block of memory to write to the output stream.</param>
        public void Write(Memory<byte> value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                Write(value.Span[i]);
            }
        }

        /// <summary>
        /// Writes a span of bytes to the output stream.
        /// </summary>
        /// <param name="value">The span of bytes to write.</param>
        public void Write(ReadOnlySpan<byte> value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i]);
            }
        }

        /// <summary>
        /// Writes the specified bytes to the stream.
        /// </summary>
        /// <param name="value">The bytes to write.</param>
        public void Write(ReadOnlyMemory<byte> value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                Write(value.Span[i]);
            }
        }

        /// <summary>
        /// Writes a byte array to the data stream.
        /// </summary>
        /// <param name="value">The byte array to write.</param>
        public void Write(byte[] value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i]);
            }
        }

        /// <summary>
        /// Writes a portion of a byte array to the underlying stream.
        /// </summary>
        /// <param name="value">The byte array containing the data to write.</param>
        /// <param name="offset">The zero-based byte offset in value at which to begin copying bytes to the stream.</param>
        /// <param name="size">The number of bytes to be written to the stream.</param>
        public void Write(byte[] value, int offset, int size)
        {
            int available = size - offset;
            for (int i = 0; i < available; i++)
            {
                Write(value[offset + i]);
            }
        }

        /// <summary>
        /// Writes the data from the IOHandler buffer to the output stream.
        /// </summary>
        /// <param name="IOHandler">The IOHandler containing the data to be written.</param>
        public void Write(DataIOHandler IOHandler)
        {
            Write(IOHandler.buffer, 0, IOHandler.bytesWritten);
        }

        /// <summary>
        /// Copies the remaining bytes from the given IOHandler to the current IOHandler starting from the current position of the given IOHandler.
        /// </summary>
        /// <param name="IOHandler">The IOHandler to copy the remaining bytes from.</param>
        public void WriteRemainingBytes(DataIOHandler IOHandler)
        {
            Write(IOHandler, IOHandler.position, IOHandler.bytesWritten);
        }

        /// <summary>
        /// Writes data from the specified <paramref name="IOHandler"/> to the current buffer starting at the specified <paramref name="offset"/> and continuing for the specified <paramref name="size"/>.
        /// </summary>
        /// <param name="IOHandler">The <see cref="DataIOHandler"/> to read data from.</param>
        /// <param name="offset">The zero-based byte offset in the current buffer at which to begin writing data.</param>
        /// <param name="size">The number of bytes to write.</param>
        public void Write(DataIOHandler IOHandler, int offset, int size)
        {
            Write(IOHandler.buffer, offset, size);
        }


        /// <summary>
        /// Writes a 128-bit array of bytes to the output stream.
        /// </summary>
        /// <param name="d128Bits">The 128-bit array of bytes to write.</param>
        internal void Write128Bits(byte[] d128Bits)
        {
            if (d128Bits.Length != 16)
            {
                OmniLogger.PrintError("The 128-bit array of bytes must have 16 bytes.");
                return;
            }

            for (int i = 0; i < d128Bits.Length; i++)
            {
                Write(d128Bits[i]);
            }
        }

        /// <summary>
        /// Reads a byte from the buffer.
        /// </summary>
        /// <returns>The byte read from the buffer.</returns>
        public byte ReadByte() => ThrowIfNotEnoughData(sizeof(byte)) ? buffer[position++] : default;

        /// <summary>
        /// Reads the payload and extracts the delivery mode, target, processing option and caching option.
        /// </summary>
        /// <param name="deliveryMode">The delivery mode of the payload.</param>
        /// <param name="target">The target of the payload.</param>
        /// <param name="processingOption">The processing option of the payload.</param>
        /// <param name="cachingOption">The caching option of the payload.</param>
        internal void ReadPayload(out DataDeliveryMode deliveryMode, out DataTarget target, out DataProcessingOption processingOption, out DataCachingOption cachingOption)
        {
            byte payload = ReadByte();
            deliveryMode = (DataDeliveryMode)(payload & 0b11);
            target = (DataTarget)((payload >> 2) & 0b11);
            processingOption = (DataProcessingOption)((payload >> 4) & 0b1);
            cachingOption = (DataCachingOption)((payload >> 5) & 0b11);
        }

        internal MessageType ReadPacket()
        {
            return (MessageType)ReadByte();
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

        /// <summary>
        /// Reads a 32-bit integer in a compressed format using 7 bits per digit.
        /// </summary>
        /// <returns>The integer value.</returns>
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

        /// <summary>
        /// Reads a Vector3 from the data stream.
        /// </summary>
        /// <returns>The Vector3 read from the data stream.</returns>
        public Vector3 ReadVector3()
        {
            float x = ReadFloat();
            float y = ReadFloat();
            float z = ReadFloat();
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Reads a Vector2 from the data stream.
        /// </summary>
        /// <returns>The Vector3 read from the data stream.</returns>
        public Vector3 ReadVector2()
        {
            float x = ReadFloat();
            float y = ReadFloat();
            return new Vector2(x, y);
        }

        /// <summary>
        /// Reads a Quaternion from the data stream.
        /// </summary>
        /// <returns>The Vector3 read from the data stream.</returns>
        public Quaternion ReadQuaternion()
        {
            float x = ReadFloat();
            float y = ReadFloat();
            float z = ReadFloat();
            float w = ReadFloat();
            return new Quaternion(x, y, z, w);
        }

        /// <summary>
        /// Reads a Color value from the data stream.
        /// </summary>
        /// <returns>The Color value read from the data stream.</returns>
        public Color ReadColor()
        {
            float r = ReadFloat();
            float g = ReadFloat();
            float b = ReadFloat();
            float a = ReadFloat();
            return new Color(r, g, b, a);
        }

        // <summary>
        /// Reads a Color value from the data stream.
        /// </summary>
        /// <returns>The Color value read from the data stream.</returns>
        public Color ReadColor32()
        {
            byte r = ReadByte();
            byte g = ReadByte();
            byte b = ReadByte();
            byte a = ReadByte();
            return new Color32(r, g, b, a);
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
        /// Reads an integer value from the data stream.
        /// </summary>
        /// <returns>The integer value read from the data stream.</returns>
        public int ReadInt()
        {
            int value = ReadByte();
            value |= ReadByte() << 8;
            value |= ReadByte() << 16;
            value |= ReadByte() << 24;
            return value;
        }

        /// <summary>
        /// Reads an unsigned integer from the data stream.
        /// </summary>
        /// <returns>The unsigned integer read from the data stream.</returns>
        public uint ReadUInt()
        {
            uint value = ReadByte();
            value |= (uint)(ReadByte() << 8);
            value |= (uint)(ReadByte() << 16);
            value |= (uint)(ReadByte() << 24);
            return value;
        }

        /// <summary>
        /// Reads a short value from the input stream.
        /// </summary>
        /// <returns>The short value read from the input stream.</returns>
        public short ReadShort()
        {
            short value = ReadByte();
            value |= (short)(ReadByte() << 8);
            return value;
        }

        /// <summary>
        /// Reads an unsigned short value from the data stream.
        /// </summary>
        /// <returns>The unsigned short value read from the data stream.</returns>
        public ushort ReadUShort()
        {
            ushort value = ReadByte();
            value |= (ushort)(ReadByte() << 8);
            return value;
        }

        /// <summary>
        /// Reads a double-precision floating-point number from the current stream.
        /// </summary>
        /// <returns>The double-precision floating-point number.</returns>
        public unsafe double ReadDouble()
        {
            uint lo = (uint)(ReadByte() | ReadByte() << 8 |
               ReadByte() << 16 | ReadByte() << 24);

            uint hi = (uint)(ReadByte() | ReadByte() << 8 |
               ReadByte() << 16 | ReadByte() << 24);

            ulong tmpBuffer = ((ulong)hi) << 32 | lo;
            return *(double*)&tmpBuffer;
        }

        /// <summary>
        /// Reads a float value from the input stream.
        /// </summary>
        /// <returns>The float value read from the input stream.</returns>
        public unsafe float ReadFloat()
        {
            uint tmpBuffer = ReadByte();
            tmpBuffer |= (uint)(ReadByte() << 8);
            tmpBuffer |= (uint)(ReadByte() << 16);
            tmpBuffer |= (uint)(ReadByte() << 24);
            return *(float*)&tmpBuffer;
        }

        /// <summary>
        /// Reads a long value from the input stream.
        /// </summary>
        /// <returns>The long value read from the input stream.</returns>
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

        /// <summary>
        /// Reads a string from the data stream.
        /// </summary>
        /// <returns>The read string.</returns>
        public string ReadString()
        {
            int length = Read7BitEncodedInt();
            byte[] encoded = new byte[length];
            Read(encoded, 0, length);
            return encoding.GetString(encoded);
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
            return encoding.GetString(encoded);
        }

        /// <summary>
        /// Reads a specified number of bytes from the input stream and writes them into an array at a specified offset.
        /// </summary>
        /// <param name="value">The array to write the bytes into.</param>
        /// <param name="offset">The offset in the array at which to begin writing.</param>
        /// <param name="size">The number of bytes to read.</param>
        public void Read(byte[] value, int offset, int size)
        {
            int available = size - offset;
            for (int i = 0; i < available; i++)
            {
                value[offset + i] = ReadByte();
            }
        }


        /// <summary>
        /// Reads a specified number of bytes from the input stream and writes them into an Span.
        /// </summary>
        /// <param name="value">The span to write the bytes into.</param>
        public void Read(Span<byte> value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                value[i] = ReadByte();
            }
        }

        #region Slice Memory
        /// <summary>
        /// Reads the buffer as a read-only memory.
        /// </summary>
        /// <returns>A read-only memory containing the buffer data.</returns>
        public ReadOnlyMemory<byte> ReadAsReadOnlyMemory()
        {
            ReadOnlyMemory<byte> _ = buffer;
            return _[position..bytesWritten];
        }

        /// <summary>
        /// Reads a portion of the buffer as a read-only memory.
        /// </summary>
        /// <param name="offset">The zero-based byte offset in the buffer at which to begin reading.</param>
        /// <param name="size">The number of bytes to read.</param>
        /// <returns>A read-only memory containing the specified portion of the buffer.</returns>
        public ReadOnlyMemory<byte> ReadAsReadOnlyMemory(int offset, int size)
        {
            ReadOnlyMemory<byte> _ = buffer;
            return _[offset..size];
        }

        /// <summary>
        /// Reads a portion of the buffer as a read-only memory of bytes.
        /// </summary>
        /// <param name="size">The size of the memory to read.</param>
        /// <returns>A read-only memory of bytes.</returns>
        public ReadOnlyMemory<byte> ReadAsReadOnlyMemory(int size)
        {
            ReadOnlyMemory<byte> _ = buffer;
            return _[position..size];
        }

        /// <summary>
        /// Reads the buffer as a read-only span of bytes.
        /// </summary>
        /// <returns>A read-only span of bytes.</returns>
        public ReadOnlySpan<byte> ReadAsReadOnlySpan()
        {
            ReadOnlySpan<byte> _ = buffer;
            return _[position..bytesWritten];
        }

        /// <summary>
        /// Reads a portion of the buffer as a read-only span of bytes.
        /// </summary>
        /// <param name="offset">The zero-based byte offset into the buffer at which to begin reading.</param>
        /// <param name="size">The number of bytes to read.</param>
        /// <returns>A read-only span of bytes.</returns>
        public ReadOnlySpan<byte> ReadAsReadOnlySpan(int offset, int size)
        {
            ReadOnlySpan<byte> _ = buffer;
            return _[offset..size];
        }

        /// <summary>
        /// Reads a portion of the buffer as a read-only span of bytes.
        /// </summary>
        /// <param name="size">The size of the span to read.</param>
        /// <returns>A read-only span of bytes.</returns>
        public ReadOnlySpan<byte> ReadAsReadOnlySpan(int size)
        {
            ReadOnlySpan<byte> _ = buffer;
            return _[position..size];
        }

        /// <summary>
        /// Reads the buffer as a <see cref="Memory{T}"/> of bytes.
        /// </summary>
        /// <returns>A <see cref="Memory{T}"/> of bytes.</returns>
        public Memory<byte> ReadAsMemory()
        {
            Memory<byte> _ = buffer;
            return _[position..bytesWritten];
        }

        /// <summary>
        /// Reads a portion of the buffer as a <see cref="Memory{T}"/> of bytes.
        /// </summary>
        /// <param name="offset">The zero-based byte offset into the buffer at which to begin reading.</param>
        /// <param name="size">The number of bytes to read.</param>
        /// <returns>A <see cref="Memory{T}"/> of bytes containing data read from the buffer.</returns>
        public Memory<byte> ReadAsMemory(int offset, int size)
        {
            Memory<byte> _ = buffer;
            return _[offset..size];
        }

        /// <summary>
        /// Reads a block of bytes from the current position in the buffer and returns it as a <see cref="Memory{T}"/>.
        /// </summary>
        /// <param name="size">The number of bytes to read.</param>
        /// <returns>A <see cref="Memory{T}"/> containing the read bytes.</returns>
        public Memory<byte> ReadAsMemory(int size)
        {
            Memory<byte> _ = buffer;
            return _[position..size];
        }

        /// <summary>
        /// Reads the buffer as a <see cref="Span{T}"/>.
        /// </summary>
        /// <returns>A <see cref="Span{T}"/> containing the data in the buffer.</returns>
        public Span<byte> ReadAsSpan()
        {
            Span<byte> _ = buffer;
            return _[position..bytesWritten];
        }

        /// <summary>
        /// Reads a span of bytes from the buffer starting at the specified offset and with the specified size.
        /// </summary>
        /// <param name="offset">The zero-based byte offset in the buffer at which to begin reading.</param>
        /// <param name="size">The number of bytes to read.</param>
        /// <returns>A span of bytes from the buffer.</returns>
        public Span<byte> ReadAsSpan(int offset, int size)
        {
            Span<byte> _ = buffer;
            return _[offset..size];
        }

        /// <summary>
        /// Reads a span of bytes from the buffer starting from the current position and of the specified size.
        /// </summary>
        /// <param name="size">The size of the span to read.</param>
        /// <returns>A span of bytes.</returns>
        public Span<byte> ReadAsSpan(int size)
        {
            Span<byte> _ = buffer;
            return _[position..size];
        }
        #endregion

        /// <summary>
        /// Reads 128 bits of data from the input stream.
        /// </summary>
        /// <returns>An array of bytes representing the 128 bits of data.</returns>
        internal byte[] Read128Bits()
        {
            byte[] d128Bits = new byte[16];
            for (int i = 0; i < d128Bits.Length; i++)
            {
                d128Bits[i] = ReadByte();
            }
            return d128Bits;
        }

        /// <summary>
        /// Encrypts the buffer using the specified key and returns the initialization vector (IV).
        /// </summary>
        /// <param name="key">The encryption key.</param>
        /// <returns>The initialization vector (IV).</returns>
        internal byte[] EncryptBuffer(byte[] key, int offset)
        {
            byte[] tmpBuffer = AESEncryption.Encrypt(buffer, offset, bytesWritten - offset, key, out byte[] IV);
            position = bytesWritten = 0;
            Write(tmpBuffer);
            position = 0;
            return IV;
        }

        /// <summary>
        /// Decrypts the buffer using the specified key and initialization vector (IV).
        /// </summary>
        /// <param name="key">The key used for decryption.</param>
        /// <param name="IV">The initialization vector (IV) used for decryption.</param>
        /// <param name="offset">The offset in the buffer where the decryption should start.</param>
        internal void DecryptBuffer(byte[] key, byte[] IV, int offset)
        {
            byte[] tmpBuffer = AESEncryption.Decrypt(buffer, offset, bytesWritten - offset, key, IV);
            position = bytesWritten = 0;
            Write(tmpBuffer);
            position = 0;
        }

        private bool ThrowIfNotEnoughSpace(int size)
        {
            if (position + size > buffer.Length)
            {
                OmniLogger.PrintError($"IOHandler: Insufficient space to write {size} bytes. Current position: {position}, requested position: {position + size}");
                return false;
            }

            return true;
        }

        private bool ThrowIfNotEnoughData(int size)
        {
            if (position + size > bytesWritten)
            {
                OmniLogger.PrintError($"IOHandler: Not enough data to read. Requested: {size} bytes, available: {bytesWritten - position} bytes, current position: {position}");
                OmniLogger.PrintError("Possible Error: Double event not registered for the same IOHandler? Possible double data read.");
                return false;
            }

            return true;
        }

        private static void ThrowIfNotInitialized()
        {
            if (bsPool == null)
            {
                OmniLogger.PrintError("Error: Omni has not been initialized. Please ensure initialization before using it.");
            }
        }

        /// <summary>
        /// Provides a way to obtain a <see cref="DataIOHandler"/> object from the pool.
        /// This method is not thread-safe.
        /// </summary>
        public static DataIOHandler Get(Encoding encoding = null)
        {
            ThrowIfNotInitialized();
            DataIOHandler _get_ = bsPool.Get();
            _get_.isRelease = false;
            if (_get_.position != 0 || _get_.bytesWritten != 0 || !_get_.isPoolObject)
                OmniLogger.PrintError($"The IOHandler is not empty -> Position: {_get_.position} | BytesWritten: {_get_.bytesWritten}. Maybe you are modifying a IOHandler that is being used by another thread? or are you using a IOHandler that has already been released?");
            else
            {
                _get_.SetEncoding(encoding);
            }
            return _get_;
        }

        /// <summary>
        /// Provides a way to obtain a <see cref="DataIOHandler"/> object from the pool.
        /// This method is not thread-safe.
        /// </summary>
        internal static DataIOHandler Get(MessageType msgType, Encoding encoding = null)
        {
            ThrowIfNotInitialized();
            DataIOHandler _get_ = bsPool.Get();
            _get_.isRelease = false;
            if (_get_.position != 0 || _get_.bytesWritten != 0 || !_get_.isPoolObject)
                OmniLogger.PrintError($"The IOHandler is not empty -> Position: {_get_.position} | BytesWritten: {_get_.bytesWritten}. Maybe you are modifying a IOHandler that is being used by another thread? or are you using a IOHandler that has already been released?");
            else
            {
                _get_.SetEncoding(encoding);
                _get_.WritePacket(msgType);
            }
            return _get_;
        }

#pragma warning disable IDE0060
        /// This method is not thread-safe.
        internal static DataIOHandler Get(MessageType msgType, bool empty, Encoding encoding = null)
#pragma warning restore IDE0060
        {
            ThrowIfNotInitialized();
            DataIOHandler _get_ = bsPool.Get();
            _get_.isRelease = false;
            if (_get_.position != 0 || _get_.bytesWritten != 0 || !_get_.isPoolObject)
                OmniLogger.PrintError($"The IOHandler is not empty -> Position: {_get_.position} | BytesWritten: {_get_.bytesWritten}. Maybe you are modifying a IOHandler that is being used by another thread? or are you using a IOHandler that has already been released?");
            else
            {
                _get_.SetEncoding(encoding);
                _get_.WritePacket(msgType);
                _get_.Write((byte)0x1);
            }
            return _get_;
        }

        /// <summary>
        /// Sets the encoding used for data input/output.
        /// If the encoding is null, ASCII encoding will be used.
        /// </summary>
        /// <param name="encoding">The encoding to be set.</param>
        private void SetEncoding(Encoding encoding)
        {
            this.encoding = encoding ?? Encoding.ASCII;
        }

        internal bool isRelease = false;
        internal void Release()
        {
            if (isPoolObject)
            {
                if (isRelease)
                {
                    OmniLogger.PrintError("Error: The IOHandler has already been released and cannot be released again.");
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