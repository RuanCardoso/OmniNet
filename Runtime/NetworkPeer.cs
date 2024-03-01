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
using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using static Omni.Core.OmniNetwork;
using Key = MessagePack.KeyAttribute;

namespace Omni.Core
{
	[MessagePackObject]
	[JsonObject(MemberSerialization.OptIn)]
	public partial class NetworkPeer
	{
		#region Rsa && Aes
		[IgnoreMember]
		[JsonIgnore]
		internal byte[] AesKey { get; set; }
		#endregion

		[IgnoreMember]
		[JsonIgnore]
		internal Block DataBlock { get; } = new Block();
		internal class Block
		{
			internal IDataReader Reader { get; private set; }
			internal int Length { get; private set; }

			internal void Initialize(int length)
			{
				if (Reader == null)
				{
					Length = length;
					Reader = new DataReader(length);
					// We are dealing with fragmented data, we cannot reset the position, the data must be written from the last position.
					Reader.ResetPositionAfterWriting = false;
				}
			}

			internal bool IsCompleted()
			{
				return Reader != null && Reader.BytesWritten == Length;
			}

			internal void Finish()
			{
				Reader = null;
				Length = 0;
			}
		}

		[@Key(0)]
		[JsonProperty("Id")]
		public int Id { get; internal set; }
		[@Key(1)]
		[JsonProperty("DbId")]
		public int DatabaseId { get; set; }
		[@Key(2)]
		[JsonProperty("Name")]
		public string Name { get; set; }
		[@Key(3)]
		[JsonProperty("Props")]
		public Dictionary<string, object> Properties { get; } = new();
		[IgnoreMember]
		[JsonIgnore]
		public EndPoint EndPoint { get; private set; }
		[@Key(4)]
		[JsonProperty("Channel")]
		public int Channel { get; internal set; }

		internal NetworkPeer(int id, EndPoint endPoint)
		{
			Id = id;
			DatabaseId = -1;
			EndPoint = endPoint;
		}

		public NetworkPeer()
		{
		}

		public T Get<T>(string key) => (T)Convert.ChangeType(Properties[key], typeof(T));
		public T FastGet<T>(string key)
		{
			object value = Properties[key];
			return Unsafe.As<object, T>(ref value);
		}

		public bool TryGet<T>(string key, out T prop)
		{
			prop = default;
			try
			{
				if (Properties.TryGetValue(key, out object value))
				{
					prop = (T)Convert.ChangeType(value, typeof(T));
					return true;
				}
				return false;
			}
			catch
			{
				return false;
			}
		}

		public bool TryFastGet<T>(string key, out T prop)
		{
			prop = default;
			try
			{
				if (Properties.TryGetValue(key, out object value))
				{
					prop = Unsafe.As<object, T>(ref value);
					return true;
				}
				return false;
			}
			catch
			{
				return false;
			}
		}

		public void Add(string key, object value) => Properties.Add(key, value);
		public bool TryAdd(string key, object value) => Properties.TryAdd(key, value);

		public void Disconnect()
		{
			Communicator.DisconnectPeer(Id);
		}

		// Client Side -> WebSocket(WebGl)
		internal void SetEndPoint(EndPoint endPoint)
		{
			EndPoint = endPoint;
		}

		public byte[] SerializePeerWithMsgPack(MessagePackSerializerOptions options = null)
		{
			return MessagePackSerializer.Serialize(this, options);
		}

		public string SerializePeerWithJsonNet(JsonSerializerSettings jsonSerializerSettings = null)
		{
			return JsonConvert.SerializeObject(this, Formatting.Indented, jsonSerializerSettings);
		}

		public byte[] SerializePropertiesWithMsgPack(MessagePackSerializerOptions options = null)
		{
			return MessagePackSerializer.Serialize(Properties, options);
		}

		public string SerializePropertiesWithJsonNet(JsonSerializerSettings jsonSerializerSettings = null)
		{
			return JsonConvert.SerializeObject(Properties, Formatting.Indented, jsonSerializerSettings);
		}

		public void CopyFrom(NetworkPeer from)
		{
			Id = from.Id;
			DatabaseId = from.DatabaseId;
			Name = from.Name;
			foreach ((string key, object value) in from.Properties)
			{
				Properties.Add(key, value);
			}
		}
	}
}