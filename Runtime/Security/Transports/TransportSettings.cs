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

namespace Omni.Internal.Transport
{
	[Serializable]
	public class TransportSettings
	{
		public bool DontFragment { get; internal set; }
		public bool ExclusiveAddressUse { get; internal set; }
		public bool NoDelay { get; set; }
		public bool EnableBroadcast { get; internal set; }
		public bool MulticastLoopback { get; internal set; }
		public bool EnableLingerState { get; set; }
		public bool Blocking { get; internal set; }
		public bool DualMode { get; internal set; }
		public byte Ttl { get; set; }
		public int BackLog { get; set; }
		public string Host { get; set; }
		public uint MaxFps { get; set; }
		public ushort ServerPort { get; set; }
		public ushort ClientPort { get; set; }
		public int ReceiveBufferSize { get; set; }
		public int SendBufferSize { get; set; }
		public int SendTimeout { get; set; }
		public int ReceiveTimeout { get; set; }
		public int LingerStateTime { get; set; }
		public int MaxMessageSize { get; set; }
		public bool UseNativeSockets { get; set; }
		public bool BroadcastReceiveEnabled { get; set; }
		public int DisconnectTimeout { get; set; }
		public bool IPv6Enabled { get; set; }
		public bool NatPunchEnabled { get; set; }
		public int PacketPoolSize { get; set; }
		public int PingInterval { get; set; }
		public bool UseSafeMtu { get; set; }
		public int MaxConnections { get; set; }
	}
}