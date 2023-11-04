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

using Omni;
using System;
using System.Collections.Generic;
using System.Linq;
using static Omni.Core.Enums;

namespace Omni.Core
{
    public static class OmniExtensions
    {
        internal static void RemoveAll<TKey, TValue>(this IDictionary<TKey, TValue> dict, Func<TKey, bool> predicate)
        {
            var items = dict.Keys.Where(predicate).ToList();
            foreach (var item in items)
            {
                dict.Remove(item);
            }
        }

        internal static string ToSizeUnit(this long value, SizeUnits unit) => (value / (double)Math.Pow(1024, (long)unit)).ToString("0.00");
        internal static bool IsInBounds<T>(this T[] array, int index) => (index >= 0) && (index < array.Length);
        internal static void Read<T>(this SyncRef<T> value, ByteStream message) where T : class
        {
            ISyncBaseValue<T> ISyncBaseValue = value as ISyncBaseValue<T>;
            ISyncBaseValue.Intern_Set(message.DeserializeWithMsgPack<T>());
        }

        internal static void Read<T>(this SyncRefCustom<T> value, ByteStream message) where T : class, ISyncCustom
        {
            if (value.Get() is ISyncCustom ISerialize)
            {
                ISerialize.Deserialize(message);
            }
            else
            {
                OmniLogger.PrintError("Error: Failed to deserialize SyncCustom object. Make sure it implements ISyncCustom.");
            }
        }

        internal static void Read<T>(this SyncValueCustom<T> value, ByteStream message) where T : unmanaged, ISyncCustom
        {
            T get_value = value.Get();
            if (get_value is ISyncCustom ISerialize)
            {
                ISerialize.Deserialize(message);
                ((ISyncBaseValue<T>)value).Intern_Set((T)ISerialize);
            }
            else
            {
                OmniLogger.PrintError("Error: Failed to deserialize SyncCustom object. Make sure it implements ISyncCustom.");
            }
        }

        internal static void Read<T>(this ISyncBaseValue<T> value, ByteStream message) where T : unmanaged
        {
            var converter = SyncValue<T>.Converter;
            switch (value.TypeCode)
            {
                case TypeCode.Int32:
                    {
                        value.Intern_Set(converter.GetInt(message.ReadInt()));
                    }
                    break;
                case TypeCode.Boolean:
                    {
                        value.Intern_Set(converter.GetBool(message.ReadBool()));
                    }
                    break;
                case TypeCode.Single:
                    {
                        value.Intern_Set(converter.GetFloat(message.ReadFloat()));
                    }
                    break;
                case TypeCode.Byte:
                    {
                        value.Intern_Set(converter.GetByte(message.ReadByte()));
                    }
                    break;
                default:
                    {
                        OmniLogger.PrintError("Error: Unsupported TypeCode for deserialization -> ISyncBaseValue<T>");
                    }
                    break;
            }
        }

        public static T GetMessage<T>(this ReadOnlyMemory<byte> message, MessagePackSerializerOptions options = null) => MessagePackSerializer.Deserialize<T>(message, options);
        public static void SendMessage<T>(this T message, MessageStream messageStream, bool fromServer, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None, MessagePackSerializerOptions options = null) where T : IMessage => SendMessage(message, messageStream, OmniHelper.GetPlayerId(fromServer), fromServer, channel, target, subTarget, cacheMode, options);
        public static void SendMessage<T>(this T message, MessageStream messageStream, ushort playerId, bool fromServer, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None, MessagePackSerializerOptions options = null) where T : IMessage
        {
            MessagePackSerializer.Serialize(messageStream, message, options);
            ByteStream msgStream = messageStream.GetStream();
            OmniNetwork.GlobalMessage(msgStream, message.Id, playerId, fromServer, channel, target, subTarget, cacheMode);
        }

        public static void SendMessage<T>(this T message, MessageStream messageStream, OmniObject @this, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None, MessagePackSerializerOptions options = null) where T : IMessage => SendMessage(message, messageStream, @this, @this.identity.playerId, channel, target, subTarget, cacheMode, options);
        public static void SendMessage<T>(this T message, MessageStream messageStream, OmniObject @this, ushort playerId, Channel channel = Channel.Unreliable, Target target = Target.Me, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None, MessagePackSerializerOptions options = null) where T : IMessage
        {
            MessagePackSerializer.Serialize(messageStream, message, options);
            ByteStream msgStream = messageStream.GetStream();
            @this.Intern_Message(msgStream, message.Id, playerId, channel, target, subTarget, cacheMode);
        }
    }
}