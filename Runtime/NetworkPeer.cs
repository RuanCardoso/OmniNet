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
using System.Collections.Generic;
using System.Net;
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

		internal NetworkPeer(int id, EndPoint endPoint)
		{
			Id = id;
			DatabaseId = -1;
			EndPoint = endPoint;
		}

		public NetworkPeer()
		{
		}

		// Client Side -> WebSocket(WebGl)
		internal void SetEndPoint(EndPoint endPoint)
		{
			EndPoint = endPoint;
		}

		public byte[] Serialize()
		{
			return MessagePackSerializer.Serialize(this, MessagePackSerializer.DefaultOptions.WithCompression(MessagePackCompression.Lz4Block));
		}

		public string SerializeAsJson()
		{
			return JsonConvert.SerializeObject(this, Formatting.Indented);
		}

		public byte[] SerializeProperties()
		{
			return MessagePackSerializer.Serialize(Properties, MessagePackSerializer.DefaultOptions.WithCompression(MessagePackCompression.Lz4Block));
		}

		public string SerializePropertiesAsJson()
		{
			return JsonConvert.SerializeObject(Properties, Formatting.Indented);
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