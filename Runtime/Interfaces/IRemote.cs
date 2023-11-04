namespace Omni.Core
{
    public interface IRemote
    {
        [Remote(0)]
        void RemoteEg(ByteStream byteStream, ushort fromId, ushort toId, RemoteStats stats);
    }
}