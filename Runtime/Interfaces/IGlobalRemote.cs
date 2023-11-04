namespace Omni.Core
{
    public interface IGlobalRemote
    {
        [Remote(0)]
        void RemoteEg(ByteStream byteStream, ushort fromId, ushort toId, bool isServer, RemoteStats stats);
    }
}