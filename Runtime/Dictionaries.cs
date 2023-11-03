using System;
using System.Collections.Generic;
using static Omni.Core.Enums;

namespace Omni.Core
{
    public class Dictionaries
    {
        internal static readonly Dictionary<(byte, byte), Action<ByteStream, ushort, ushort, bool, RemoteStats>> RPCMethods = new(); // [RPC ID, INSTANCE ID]
        internal static readonly Dictionary<(ushort, ushort, bool, byte, ObjectType), OmniIdentity> identities = new();
        internal static readonly Dictionary<int, Action<ReadOnlyMemory<byte>, ushort, bool, RemoteStats>> handlers = new();
        internal static readonly Dictionary<(byte remoteId, ushort identityId, byte instanceId, ushort playerId, byte sceneId, ObjectType objectType), OmniCache> remoteCache = new();
        internal static readonly Dictionary<(byte remoteId, byte instanceId, ushort playerId), OmniCache> globalRemoteCache = new();
        internal static readonly Dictionary<(ushort identityId, byte instanceId, ushort playerId, byte sceneId, ObjectType objectType), OmniCache> serializeCache = new();
        internal static readonly Dictionary<(byte varId, ushort identityId, byte instanceId, ushort playerId, byte sceneId, ObjectType objectType), OmniCache> syncCache = new();
        internal static readonly Dictionary<(byte id, ushort playerId), OmniCache> globalCache = new();
        internal static readonly Dictionary<(byte id, ushort identityId, byte instanceId, ushort playerId, byte sceneId, ObjectType objectType), OmniCache> localCache = new();
    }
}