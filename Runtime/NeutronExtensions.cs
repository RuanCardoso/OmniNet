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
using System;
using System.Collections.Generic;
using System.Linq;
using static Neutron.Core.Enums;

namespace Neutron.Core
{
    public static class NeutronExtensions
    {
        internal static void RemoveAll<TKey, TValue>(this IDictionary<TKey, TValue> dict, Func<TKey, bool> predicate)
        {
            var items = dict.Keys.Where(predicate).ToList();
            foreach (var item in items) dict.Remove(item);
        }

        internal static string ToSizeUnit(this long value, SizeUnits unit) => (value / (double)Math.Pow(1024, (long)unit)).ToString("0.00");
        internal static bool IsInBounds<T>(this T[] array, int index) => (index >= 0) && (index < array.Length);
        internal static void Read<T>(this SyncRef<T> value, ByteStream message) where T : class => value.Intern_Set(message.Deserialize<T>());
        internal static void Read<T>(this SyncValue<T> value, ByteStream message) where T : unmanaged
        {
            var converter = SyncValue<T>.Converter;
            switch (value.typeCode)
            {
                case TypeCode.Int32:
                    value.Intern_Set(converter.GetInt(message.ReadInt()));
                    break;
                case TypeCode.Boolean:
                    value.Intern_Set(converter.GetBool(message.ReadBool()));
                    break;
                case TypeCode.Single:
                    value.Intern_Set(converter.GetFloat(message.ReadFloat()));
                    break;
                case TypeCode.Byte:
                    value.Intern_Set(converter.GetByte(message.ReadByte()));
                    break;
            }
        }

        internal static void Read<T>(this SyncCustom<T> value, ByteStream message) where T : class, ISyncCustom
        {
            ISyncCustom ISerialize = value.Get() as ISyncCustom;
            if (ISerialize != null) ISerialize.Deserialize(message);
            else Logger.PrintError("SyncCustom -> Deserialize fail!");
        }

        public static T GetMessage<T>(this ReadOnlyMemory<byte> message, MessagePackSerializerOptions options = null) => MessagePackSerializer.Deserialize<T>(message, options);
        public static void SendMessage<T>(this T message, MessageStream messageStream, bool fromServer, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None, MessagePackSerializerOptions options = null) where T : IMessage => SendMessage(message, messageStream, NeutronHelper.GetPlayerId(fromServer), fromServer, channel, target, subTarget, cacheMode, options);
        public static void SendMessage<T>(this T message, MessageStream messageStream, ushort playerId, bool fromServer, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None, MessagePackSerializerOptions options = null) where T : IMessage
        {
            MessagePackSerializer.Serialize(messageStream, message, options);
            ByteStream msgStream = messageStream.GetStream();
            NeutronNetwork.GlobalMessage(msgStream, message.Id, playerId, fromServer, channel, target, subTarget, cacheMode);
        }

        public static void SendMessage<T>(this T message, MessageStream messageStream, NeutronObject @this, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None, MessagePackSerializerOptions options = null) where T : IMessage => SendMessage(message, messageStream, @this, @this.identity.playerId, channel, target, subTarget, cacheMode, options);
        public static void SendMessage<T>(this T message, MessageStream messageStream, NeutronObject @this, ushort playerId, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None, MessagePackSerializerOptions options = null) where T : IMessage
        {
            MessagePackSerializer.Serialize(messageStream, message, options);
            ByteStream msgStream = messageStream.GetStream();
            @this.Intern_Message(msgStream, message.Id, playerId, channel, target, subTarget, cacheMode);
        }
    }
}