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

using System;
using static Neutron.Core.Enums;

namespace Neutron.Core
{
    internal class RemoteCache
    {
        internal ReadOnlyMemory<byte> Buffer { get; private set; }

        internal readonly byte[] data;
        internal readonly ushort fromId;
        internal readonly ushort toId;
        internal readonly byte sceneId;
        internal readonly ushort identityId;
        internal readonly byte rpcId;
        internal readonly byte instanceId;
        internal readonly MessageType messageType;
        internal readonly Channel channel;
        internal readonly ObjectType objectType;

        internal RemoteCache(byte[] data, int length, ushort fromId, ushort toId, byte sceneId, ushort identityId, byte rpcId, byte instanceId, MessageType messageType, Channel channel, ObjectType objectType)
        {
            this.data = data;
            this.fromId = fromId;
            this.toId = toId;
            this.sceneId = sceneId;
            this.identityId = identityId;
            this.rpcId = rpcId;
            this.instanceId = instanceId;
            this.messageType = messageType;
            this.channel = channel;
            this.objectType = objectType;

            Buffer = data;
            Buffer = Buffer[..length];
        }

        internal void SetData(byte[] src, int length)
        {
            System.Buffer.BlockCopy(src, 0, data, 0, length);
            Buffer = Buffer[..length];
        }
    }
}