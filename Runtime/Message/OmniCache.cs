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
using static Omni.Core.Enums;

namespace Omni.Core
{
    internal class OmniCache
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
        internal readonly DataDeliveryMode deliveryMode;
        internal readonly ObjectType objectType;

        internal OmniCache(byte[] data, int length, ushort fromId, ushort toId, byte sceneId, ushort identityId, byte rpcId, byte instanceId, MessageType messageType, DataDeliveryMode deliveryMode, ObjectType objectType)
        {
            this.data = data;
            this.fromId = fromId;
            this.toId = toId;
            this.sceneId = sceneId;
            this.identityId = identityId;
            this.rpcId = rpcId;
            this.instanceId = instanceId;
            this.messageType = messageType;
            this.deliveryMode = deliveryMode;
            this.objectType = objectType;

            Buffer = this.data;
            Buffer = Buffer[..length];
        }

        internal void SetData(byte[] src, int length)
        {
            System.Buffer.BlockCopy(src, 0, data, 0, length);
            Buffer = Buffer[..length];
        }
    }
}