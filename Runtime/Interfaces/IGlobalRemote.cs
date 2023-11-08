namespace Omni.Core
{
    public interface IGlobalRemote
    {
        [Remote(0)]
        void RemoteEg(DataIOHandler IOHandler, ushort fromId, ushort toId, bool isServer, RemoteStats stats);
    }
}