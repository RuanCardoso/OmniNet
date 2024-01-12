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
using System.Data;
using System.Linq;
using System.Text;
using static Omni.Core.Enums;

namespace Omni.Core
{
    public static class OmniExtensions
    {
        /// <summary>
        /// Removes all key-value pairs from the dictionary that satisfy the specified predicate.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <param name="dict">The dictionary to remove items from.</param>
        /// <param name="predicate">A function to test each key for a condition.</param>
        internal static void RemoveAll<TKey, TValue>(this IDictionary<TKey, TValue> dict, Func<TKey, bool> predicate)
        {
            var items = dict.Keys.Where(predicate).ToList();
            foreach (var item in items)
            {
                dict.Remove(item);
            }
        }

        /// <summary>
        /// Converts a ulong value to a specified size unit.
        /// </summary>
        /// <param name="value">The ulong value to convert.</param>
        /// <param name="unit">The size unit to convert to.</param>
        /// <returns>A string representation of the converted value with the specified size unit.</returns>
        internal static string ToSizeUnit(this ulong value, SizeUnits unit) => (value / (double)Math.Pow(1024, (ulong)unit)).ToString("0.00");

        /// <summary>
        /// Checks if the specified index is within the bounds of the array.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="array">The array to check.</param>
        /// <param name="index">The index to check.</param>
        /// <returns><c>true</c> if the index is within the bounds of the array; otherwise, <c>false</c>.</returns>
        internal static bool IsInBounds<T>(this T[] array, int index) => (index >= 0) && (index < array.Length);

        /// <summary>
        /// Reads the value from the specified <see cref="DataIOHandler"/> and sets it to the <see cref="SyncRef{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="value">The <see cref="SyncRef{T}"/> to set the value to.</param>
        /// <param name="IOHandler">The <see cref="DataIOHandler"/> used to deserialize the value.</param>
        internal static void Read<T>(this SyncRef<T> value, DataIOHandler IOHandler) where T : class
        {
            ISyncBaseValue<T> ISyncBaseValue = value;
            ISyncBaseValue.Intern_Set(IOHandler.DeserializeWithMsgPack<T>());
        }

        /// <summary>
        /// Reads the serialized data for a <see cref="SyncRefCustom{T}"/> object and deserializes it using the provided <see cref="DataIOHandler"/>.
        /// </summary>
        /// <typeparam name="T">The type of the object that implements <see cref="ISyncCustom"/>.</typeparam>
        /// <param name="value">The <see cref="SyncRefCustom{T}"/> object to read and deserialize.</param>
        /// <param name="IOHandler">The <see cref="DataIOHandler"/> used for deserialization.</param>
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

        /// <summary>
        /// Reads the value of a <see cref="SyncValueCustom{T}"/> object from a <see cref="DataIOHandler"/> using custom deserialization.
        /// </summary>
        /// <typeparam name="T">The type of the value being read.</typeparam>
        /// <param name="value">The <see cref="SyncValueCustom{T}"/> object to read the value from.</param>
        /// <param name="IOHandler">The <see cref="DataIOHandler"/> used for deserialization.</param>
        /// <remarks>
        /// This method reads the value of a <see cref="SyncValueCustom{T}"/> object from a <see cref="DataIOHandler"/> using custom deserialization.
        /// It first checks if the value implements the <see cref="ISyncCustom"/> interface. If it does, it calls the <see cref="ISyncCustom.Deserialize"/> method
        /// on the value to perform the deserialization. The deserialized value is then set on the <see cref="SyncValueCustom{T}"/> object using the
        /// <see cref="ISyncBaseValue{T}.Intern_Set"/> method. If the value does not implement the <see cref="ISyncCustom"/> interface, an error message
        /// is logged using the <see cref="OmniLogger.PrintError"/> method.
        /// </remarks>
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

        /// <summary>
        /// Reads the serialized value from the specified <see cref="DataIOHandler"/> and sets it to the <see cref="ISyncBaseValue{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="value">The <see cref="ISyncBaseValue{T}"/> to set the deserialized value to.</param>
        /// <param name="IOHandler">The <see cref="DataIOHandler"/> used for reading the serialized value.</param>
        /// <remarks>
        /// This method is used for deserializing different types of values from the <see cref="DataIOHandler"/>.
        /// It supports deserialization of <see cref="int"/>, <see cref="bool"/>, <see cref="float"/>, and <see cref="byte"/> types.
        /// If the type is not supported, an error message will be logged.
        /// </remarks>
        internal static void Read<T>(this ISyncBaseValue<T> value, DataIOHandler IOHandler) where T : unmanaged
        {
            var converter = SyncValue<T>.Converter; // for high performance, converter is used to avoid boxing!
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
                        value.Intern_Set(converter.GetByte(IOHandler.InternalReadByte()));
                    }
                    break;
                default:
                    {
                        OmniLogger.PrintError("Error: Unsupported TypeCode for deserialization -> ISyncBaseValue<T>");
                    }
                    break;
            }
        }

        /// <summary>
        /// Deserializes a message of type <typeparamref name="T"/> from a byte array using MessagePack.
        /// </summary>
        /// <typeparam name="T">The type of the message to deserialize.</typeparam>
        /// <param name="message">The byte array containing the serialized message.</param>
        /// <param name="options">The options to use for deserialization (optional).</param>
        /// <returns>The deserialized message of type <typeparamref name="T"/>.</returns>
        public static T GetMessage<T>(this ReadOnlyMemory<byte> message, MessagePackSerializerOptions options = null) => MessagePackSerializer.Deserialize<T>(message, options);
        /// <summary>
        /// Sends a message using the specified message stream.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message to send.</param>
        /// <param name="messageStream">The message stream to send the message on.</param>
        /// <param name="fromServer">Indicates whether the message is sent from the server.</param>
        /// <param name="deliveryMode">The delivery mode for the message.</param>
        /// <param name="target">The target for the message.</param>
        /// <param name="processingOption">The processing option for the message.</param>
        /// <param name="cachingOption">The caching option for the message.</param>
        /// <param name="options">The options for the message serializer.</param>
        /// <returns>void</returns>
        public static void SendMessage<T>(this T message, MessageStream messageStream, bool fromServer, DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured, DataTarget target = DataTarget.Self, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None, MessagePackSerializerOptions options = null) where T : IMessage => SendMessage(message, messageStream, OmniHelper.GetPlayerId(fromServer), fromServer, deliveryMode, target, processingOption, cachingOption, options);
        /// <summary>
        /// Sends a message using the specified message stream to a specific player.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message to send.</param>
        /// <param name="messageStream">The message stream to use for serialization.</param>
        /// <param name="playerId">The ID of the player to send the message to.</param>
        /// <param name="fromServer">Indicates whether the message is sent from the server.</param>
        /// <param name="deliveryMode">The delivery mode for the message.</param>
        /// <param name="target">The target of the message.</param>
        /// <param name="processingOption">The processing option for the message.</param>
        /// <param name="cachingOption">The caching option for the message.</param>
        /// <param name="options">The serializer options for message serialization.</param>
        /// <exception cref="ArgumentNullException">Thrown if the message or messageStream is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the message serialization fails.</exception>
        public static void SendMessage<T>(this T message, MessageStream messageStream, ushort playerId, bool fromServer, DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured, DataTarget target = DataTarget.Self, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None, MessagePackSerializerOptions options = null) where T : IMessage
        {
            MessagePackSerializer.Serialize(messageStream, message, options);
            DataIOHandler _IOHandler_ = messageStream.GetIOHandler();
            OmniNetwork.GlobalMessage(_IOHandler_, message.Id, playerId, fromServer, deliveryMode, target, processingOption, cachingOption);
        }

        /// <summary>
        /// Sends a message using the specified message stream and delivery options.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message to send.</param>
        /// <param name="messageStream">The message stream to send the message on.</param>
        /// <param name="this">The OmniObject instance that is sending the message.</param>
        /// <param name="deliveryMode">The delivery mode for the message (default: DataDeliveryMode.Unsecured).</param>
        /// <param name="target">The target for the message (default: DataTarget.Self).</param>
        /// <param name="processingOption">The processing option for the message (default: DataProcessingOption.DoNotProcessOnServer).</param>
        /// <param name="cachingOption">The caching option for the message (default: DataCachingOption.None).</param>
        /// <param name="options">The options for the MessagePack serializer (default: null).</param>
        /// <exception cref="System.ArgumentNullException">Thrown if the message or messageStream is null.</exception>
        /// <remarks>
        /// This extension method allows sending a message using the specified message stream and delivery options.
        /// The message will be sent from the OmniObject instance that is invoking the method.
        /// </remarks>
        public static void SendMessage<T>(this T message, MessageStream messageStream, OmniObject @this, DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured, DataTarget target = DataTarget.Self, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None, MessagePackSerializerOptions options = null) where T : IMessage => SendMessage(message, messageStream, @this, @this.identity.playerId, deliveryMode, target, processingOption, cachingOption, options);
        /// <summary>
        /// Sends a message using the specified message stream and network parameters.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message to send.</param>
        /// <param name="messageStream">The message stream to use for sending.</param>
        /// <param name="this">The OmniObject instance.</param>
        /// <param name="playerId">The ID of the player to send the message to.</param>
        /// <param name="deliveryMode">The delivery mode for the message (default: DataDeliveryMode.Unsecured).</param>
        /// <param name="target">The target for the message (default: DataTarget.Self).</param>
        /// <param name="processingOption">The processing option for the message (default: DataProcessingOption.DoNotProcessOnServer).</param>
        /// <param name="cachingOption">The caching option for the message (default: DataCachingOption.None).</param>
        /// <param name="options">The MessagePack serializer options (default: null).</param>
        /// <exception cref="ArgumentNullException">Thrown if the message or messageStream is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the message serialization fails.</exception>
        public static void SendMessage<T>(this T message, MessageStream messageStream, OmniObject @this, ushort playerId, DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured, DataTarget target = DataTarget.Self, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None, MessagePackSerializerOptions options = null) where T : IMessage
        {
            MessagePackSerializer.Serialize(messageStream, message, options);
            DataIOHandler _IOHandler_ = messageStream.GetIOHandler();
            @this.Intern_Message(_IOHandler_, message.Id, playerId, deliveryMode, target, processingOption, cachingOption);
        }

        /// <summary>
        /// Maps the first result of a query to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to map the result to.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="transaction">The database transaction (optional).</param>
        /// <param name="timeout">The command timeout (optional).</param>
        /// <returns>The mapped result of type T.</returns>
        public static T MapFirstResultTo<T>(this Query query, IDbTransaction transaction = null, int? timeout = null)
        {
            var toJsonObject = query.First<object>(transaction, timeout);
            var fromJsonObject = JsonConvert.SerializeObject(toJsonObject);
            return JsonConvert.DeserializeObject<T>(fromJsonObject);
        }

        /// <summary>
        /// Maps all the results of a database query to a collection of objects of type T.
        /// </summary>
        /// <typeparam name="T">The type of objects to map the results to.</typeparam>
        /// <param name="query">The database query.</param>
        /// <param name="transaction">The database transaction (optional).</param>
        /// <param name="timeout">The timeout for the query (optional).</param>
        /// <returns>A collection of objects of type T.</returns>
        public static IEnumerable<T> MapAllResultsTo<T>(this Query query, IDbTransaction transaction = null, int? timeout = null)
        {
            var toJsonObject = query.Get<object>(transaction, timeout);
            var fromJsonObject = JsonConvert.SerializeObject(toJsonObject);
            return JsonConvert.DeserializeObject<IEnumerable<T>>(fromJsonObject);
        }

        /// <summary>
        /// Maps the paginated results of a query to a specified type.
        /// </summary>
        /// <typeparam name="T">The type to map the results to.</typeparam>
        /// <param name="query">The query to paginate.</param>
        /// <param name="page">The page number to retrieve.</param>
        /// <param name="perPage">The number of results per page.</param>
        /// <param name="transaction">The database transaction to use.</param>
        /// <param name="timeout">The timeout for the query.</param>
        /// <returns>An enumerable collection of mapped results.</returns>
        public static IEnumerable<T> MapPageResultsTo<T>(this Query query, int page, int perPage = 25, IDbTransaction transaction = null, int? timeout = null)
        {
            var toJsonObject = query.Paginate<object>(page, perPage, transaction, timeout);
            var fromJsonObject = JsonConvert.SerializeObject(toJsonObject.List);
            return JsonConvert.DeserializeObject<IEnumerable<T>>(fromJsonObject);
        }

        /// <summary>
        /// Processes a query in chunks, invoking a function for each chunk of elements.
        /// </summary>
        /// <typeparam name="T">The type of elements in the query.</typeparam>
        /// <param name="query">The query to process.</param>
        /// <param name="chunkSize">The size of each chunk.</param>
        /// <param name="func">The function to invoke for each chunk of elements. The function should return true to continue processing, or false to stop processing.</param>
        /// <param name="transaction">The database transaction to use.</param>
        /// <param name="timeout">The timeout for the query.</param>
        public static void ProcessInChunks<T>(this Query query, int chunkSize, Func<IEnumerable<T>, int, bool> func, IDbTransaction transaction = null, int? timeout = null)
        {
            query.Chunk<T>(chunkSize, func, transaction, timeout);
        }

        /// <summary>
        /// Processes a query in chunks, invoking the specified action for each chunk of items.
        /// </summary>
        /// <typeparam name="T">The type of items in the query.</typeparam>
        /// <param name="query">The query to process.</param>
        /// <param name="chunkSize">The size of each chunk.</param>
        /// <param name="action">The action to invoke for each chunk of items.</param>
        /// <param name="transaction">The database transaction to use (optional).</param>
        /// <param name="timeout">The timeout for the query (optional).</param>
        public static void ProcessInChunks<T>(this Query query, int chunkSize, Action<IEnumerable<T>, int> action, IDbTransaction transaction = null, int? timeout = null)
        {
            query.Chunk<T>(chunkSize, action, transaction, timeout);
        }

        /// <summary>
        /// Maps a Query object to a Row object.
        /// </summary>
        /// <param name="query">The Query object to map.</param>
        /// <returns>The mapped Row object.</returns>
        public static Row MapToRow(this Query query)
        {
            return MapFirstResultTo<Row>(query);
        }

        /// <summary>
        /// Maps the query results to a collection of rows.
        /// </summary>
        /// <param name="query">The query to map.</param>
        /// <returns>A collection of rows.</returns>
        public static IEnumerable<Row> MapToRows(this Query query)
        {
            return MapAllResultsTo<Row>(query);
        }

        /// <summary>
        /// Decodes a Base64 encoded string into its original UTF-8 representation.
        /// </summary>
        /// <param name="value">The Base64 encoded string to decode.</param>
        /// <returns>The original UTF-8 string.</returns>
        public static string FromBase64(this string value, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            return encoding.GetString(Convert.FromBase64String(value));
        }
    }
}