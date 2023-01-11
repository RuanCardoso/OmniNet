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

using MessagePack;
using Neutron.Core;
using UnityEngine;

namespace Neutron.Tests
{
    [MessagePackObject]
    public struct NetMove : IMessage
    {
        [IgnoreMember] public byte Id => 1;

        [Key(0)]
        public Vector3 pos;
        [Key(1)]
        public Quaternion rotation;
        [Key(2)]
        public double time;

        public NetMove(Vector3 pos, Quaternion rotation, double time)
        {
            this.pos = pos;
            this.rotation = rotation;
            this.time = time;
        }
    }
}