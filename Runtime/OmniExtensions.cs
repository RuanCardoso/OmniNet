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
using Newtonsoft.Json;
using Omni.Execution;
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
        internal static void Read<T>(this SyncRef<T> value, DataIOHandler IOHandler) where T : class
        {
            ISyncBaseValue<T> ISyncBaseValue = value;
            ISyncBaseValue.Intern_Set(IOHandler.DeserializeWithMsgPack<T>());
        }

        internal static void Read<T>(this SyncRefCustom<T> value, DataIOHandler IOHandler) where T : class, ISyncCustom
        {
            if (value.Get() is ISyncCustom ISerialize)
            {
                ISerialize.Deserialize(IOHandler);
            }
            else
            {
                OmniLogger.PrintError("Error: Failed to deserialize SyncCustom object. Make sure it implements ISyncCustom.");
            }
        }

        internal static void Read<T>(this SyncValueCustom<T> value, DataIOHandler IOHandler) where T : unmanaged, ISyncCustom
        {
            T get_value = value.Get();
            if (get_value is ISyncCustom ISerialize)
            {
                ISerialize.Deserialize(IOHandler);
                ((ISyncBaseValue<T>)value).Intern_Set((T)ISerialize);
            }
            else
            {
                OmniLogger.PrintError("Error: Failed to deserialize SyncCustom object. Make sure it implements ISyncCustom.");
            }
        }

        internal static void Read<T>(this ISyncBaseValue<T> value, DataIOHandler IOHandler) where T : unmanaged
        {
            var converter = SyncValue<T>.Converter;
            switch (value.TypeCode)
            {
                case TypeCode.Int32:
                    {
                        value.Intern_Set(converter.GetInt(IOHandler.ReadInt()));
                    }
                    break;
                case TypeCode.Boolean:
                    {
                        value.Intern_Set(converter.GetBool(IOHandler.ReadBool()));
                    }
                    break;
                case TypeCode.Single:
                    {
                        value.Intern_Set(converter.GetFloat(IOHandler.ReadFloat()));
                    }
                    break;
                case TypeCode.Byte:
                    {
                        value.Intern_Set(converter.GetByte(IOHandler.ReadByte()));
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
        public static void SendMessage<T>(this T message, MessageStream messageStream, bool fromServer, DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured, DataTarget target = DataTarget.Self, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None, MessagePackSerializerOptions options = null) where T : IMessage => SendMessage(message, messageStream, OmniHelper.GetPlayerId(fromServer), fromServer, deliveryMode, target, processingOption, cachingOption, options);
        public static void SendMessage<T>(this T message, MessageStream messageStream, ushort playerId, bool fromServer, DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured, DataTarget target = DataTarget.Self, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None, MessagePackSerializerOptions options = null) where T : IMessage
        {
            MessagePackSerializer.Serialize(messageStream, message, options);
            DataIOHandler _IOHandler_ = messageStream.GetIOHandler();
            OmniNetwork.GlobalMessage(_IOHandler_, message.Id, playerId, fromServer, deliveryMode, target, processingOption, cachingOption);
        }

        public static void SendMessage<T>(this T message, MessageStream messageStream, OmniObject @this, DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured, DataTarget target = DataTarget.Self, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None, MessagePackSerializerOptions options = null) where T : IMessage => SendMessage(message, messageStream, @this, @this.identity.playerId, deliveryMode, target, processingOption, cachingOption, options);
        public static void SendMessage<T>(this T message, MessageStream messageStream, OmniObject @this, ushort playerId, DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured, DataTarget target = DataTarget.Self, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None, MessagePackSerializerOptions options = null) where T : IMessage
        {
            MessagePackSerializer.Serialize(messageStream, message, options);
            DataIOHandler _IOHandler_ = messageStream.GetIOHandler();
            @this.Intern_Message(_IOHandler_, message.Id, playerId, deliveryMode, target, processingOption, cachingOption);
        }

        public static T To<T>(this Query query)
        {
            var toJsonObject = query.First<object>();
            var fromJsonObject = JsonConvert.SerializeObject(toJsonObject);
            return JsonConvert.DeserializeObject<T>(fromJsonObject);
        }
    }
}