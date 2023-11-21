using System;
using System.Collections.Generic;
using static Omni.Core.Enums;

namespace Omni.Core
{
    /// <summary>
    /// For high performance, hash tables are used to store data.
    /// </summary>
    public class Dictionaries
    {
        internal static readonly Dictionary<(byte, byte), Action<DataIOHandler, ushort, ushort, bool, RemoteStats>> RPCMethods = new(); // [RPC ID, INSTANCE ID]
        internal static readonly Dictionary<(ushort, ushort, bool, byte, ObjectType), OmniIdentity> Identities = new();
        internal static readonly Dictionary<int, Action<ReadOnlyMemory<byte>, ushort, bool, RemoteStats>> Handlers = new();
        internal static readonly Dictionary<(byte remoteId, ushort identityId, byte instanceId, ushort playerId, byte sceneId, ObjectType objectType), OmniCache> RemoteDataCache = new();
        internal static readonly Dictionary<(byte remoteId, byte instanceId, ushort playerId), OmniCache> RemoteGlobalDataCache = new();
        internal static readonly Dictionary<(ushort identityId, byte instanceId, ushort playerId, byte sceneId, ObjectType objectType), OmniCache> SerializeDataCache = new();
        internal static readonly Dictionary<(byte varId, ushort identityId, byte instanceId, ushort playerId, byte sceneId, ObjectType objectType), OmniCache> SyncDataCache = new();
        internal static readonly Dictionary<(byte id, ushort playerId), OmniCache> GlobalDataCache = new();
        internal static readonly Dictionary<(byte id, ushort identityId, byte instanceId, ushort playerId, byte sceneId, ObjectType objectType), OmniCache> LocalDataCache = new();

        internal static void ClearDataCache(ushort playerId)
        {
            GlobalDataCache.RemoveAll(x => x.playerId == playerId);
            RemoteGlobalDataCache.RemoveAll(x => x.playerId == playerId);
            LocalDataCache.RemoveAll(x => x.playerId == playerId);
            RemoteDataCache.RemoveAll(x => x.playerId == playerId);
            SerializeDataCache.RemoveAll(x => x.playerId == playerId);
            SyncDataCache.RemoveAll(x => x.playerId == playerId);
        }
    }
}