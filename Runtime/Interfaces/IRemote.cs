namespace Omni.Core
{
    public interface IRemote
    {
        [Remote(0)]
        void RemoteEg(DataIOHandler IOHandler, ushort fromId, ushort toId, RemoteStats stats);
    }
}