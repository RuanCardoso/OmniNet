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

namespace Neutron.Tests
{
    [MessagePackObject]
    public class PlayerTests : IMessage
    {
        [IgnoreMember] public byte Id => 0;
        //*********************************
        [Key(0)]
        public string name;
        [Key(1)]
        public int idade;
    }

    [MessagePackObject]
    public class ChatMsg
    {
        [IgnoreMember] public byte Id => 1;
        //*********************************
        [Key(0)]
        public string name;
        [Key(1)]
        public int msg;
    }
}