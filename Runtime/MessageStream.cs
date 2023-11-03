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

using System.IO;
using static Omni.Core.PlatformSettings;

namespace Omni.Core
{
    public class MessageStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override long Length => msgStream.BytesWritten;
        public override long Position { get => msgStream.Position; set => msgStream.Position = (int)value; }

        private readonly ByteStream msgStream;
        public MessageStream() => msgStream = new(ServerSettings.maxPacketSize);

        public override void Write(byte[] buffer, int offset, int count) => msgStream.Write(buffer, offset, count);
        public override int Read(byte[] buffer, int offset, int count)
        {
            msgStream.Read(buffer, offset, count);
            return msgStream.Position;
        }

        public ByteStream GetStream() => msgStream;
        public override void Flush() => msgStream.Write();
        public override long Seek(long offset, SeekOrigin origin) => throw new System.NotImplementedException("This operation is not supported, but you can modify the Position property!");
        public override void SetLength(long value) => throw new System.NotImplementedException("This operation is not supported as a omni buffer is not resizable!");
    }
}