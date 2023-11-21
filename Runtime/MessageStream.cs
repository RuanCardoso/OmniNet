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
using System.Text;
using static Omni.Core.PlatformSettings;

namespace Omni.Core
{
    public class MessageStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override long Length => IOHandler.BytesWritten;
        public override long Position
        {
            get => IOHandler.Position;
            set => IOHandler.Position = (int)value;
        }

        public DataIOHandler GetIOHandler() => IOHandler;
        private readonly DataIOHandler IOHandler;
        public MessageStream(Encoding encoding = null)
        {
            try
            {
                IOHandler = new DataIOHandler(ServerSettings.maxPacketSize, encoding: encoding);
            }
            catch
            {
                OmniLogger.PrintError("Cannot initialize in the global scope. Initialize within the Start or Awake method.");
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            IOHandler.Write(buffer, offset, count);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            IOHandler.Read(buffer, offset, count);
            return IOHandler.Position;
        }

        public override void Flush() => IOHandler.Write();
        public override long Seek(long offset, SeekOrigin origin) => throw new System.NotImplementedException("This operation is not supported. Please modify the Position property instead.");
        public override void SetLength(long value) => throw new System.NotImplementedException("This operation is not supported because an Omni buffer is not resizable.");
    }
}