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

using Omni.Internal;
using Omni.Internal.Transport;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace Omni.Core
{
	[RequireComponent(typeof(NetworkCommunicator))]
	[RequireComponent(typeof(NetworkMonitor))]
	[RequireComponent(typeof(NetworkTime))]
	public partial class OmniNetwork : RealtimeTickBasedSystem
	{
		[SerializeField]
		[InfoBox("Defines how the Editor simulation will work.")]
		private LocalPhysicsMode physicsMode = LocalPhysicsMode.Physics3D;
		[SerializeField]
		[InfoBox("Defines how network read operations will be executed, e.g., 30 Fps/Ticks x Iops.")]
		private GameLoopOption loopMode = GameLoopOption.RealTime;
		[SerializeField]
		private bool ConnectAsync = false;

		[SerializeField]
		[InfoBox("In the event of network freezes, consider adjusting the IOPS or Rec/Send buffer size, or reduce the frequency of data transmissions.\r\n")]
		private TransportOption transportOption = TransportOption.TcpTransport;

		[InfoBox("Please consider increasing (IOPS) in the event that read operations are experiencing a discernible lag relative to the sending rate.\r\n")]
		[SerializeField][EnableIf("transportOption", TransportOption.TcpTransport)][MinValue(1)] private int m_IOPS = 1;

		[SerializeField]
		private UnityEvent<TransportSettings, RuntimePlatform> OnTransportSettings;

		#region LiteNetLib
		[SerializeField]
		[ShowIf(nameof(TransportOption), TransportOption.LiteNetTransport)]
		[OnValueChanged(nameof(OnLiteNetSettingsChanged))]
		[InfoBox("Experimental: Use direct socket calls to drastically increase speed and reduce GC pressure. Only for Windows/Linux(Server & Client)")]
		private bool useNativeSockets = false;

		[SerializeField]
		[ShowIf(nameof(TransportOption), TransportOption.LiteNetTransport)]
		[OnValueChanged(nameof(OnLiteNetSettingsChanged))]
		private bool enableBroadcast = false;

		[SerializeField]
		[Label("IPv6 Enabled")]
		[ShowIf(nameof(TransportOption), TransportOption.LiteNetTransport)]
		[OnValueChanged(nameof(OnLiteNetSettingsChanged))]
		private bool IPv6Enabled = true;

		[SerializeField]
		[ShowIf(nameof(TransportOption), TransportOption.LiteNetTransport)]
		[OnValueChanged(nameof(OnLiteNetSettingsChanged))]
		private bool NatPunchEnabled = false;

		[SerializeField]
		[ShowIf(nameof(TransportOption), TransportOption.LiteNetTransport)]
		[OnValueChanged(nameof(OnLiteNetSettingsChanged))]
		private bool UseSafeMtu = false;

		[SerializeField]
		[ShowIf(nameof(TransportOption), TransportOption.LiteNetTransport)]
		[OnValueChanged(nameof(OnLiteNetSettingsChanged))]
		private int DisconnectTimeout = 5000;

		[SerializeField]
		[ShowIf(nameof(TransportOption), TransportOption.LiteNetTransport)]
		[OnValueChanged(nameof(OnLiteNetSettingsChanged))]
		private int PacketPoolSize = 1000;

		[SerializeField]
		[ShowIf(nameof(TransportOption), TransportOption.LiteNetTransport)]
		[OnValueChanged(nameof(OnLiteNetSettingsChanged))]
		private int PingInterval = 1000;

		private void OnLiteNetSettingsChanged()
		{
			TransportSettings = new();
			TransportSettings.UseNativeSockets = useNativeSockets;
			TransportSettings.EnableBroadcast = enableBroadcast;
			TransportSettings.IPv6Enabled = IPv6Enabled;
			TransportSettings.NatPunchEnabled = NatPunchEnabled;
			TransportSettings.UseSafeMtu = UseSafeMtu;
			TransportSettings.DisconnectTimeout = DisconnectTimeout;
			TransportSettings.PacketPoolSize = PacketPoolSize;
			TransportSettings.PingInterval = PingInterval;
			SetAll(TransportSettings);
		}
		#endregion

		#region Global
		[SerializeField]
		[OnValueChanged(nameof(GetAll))]
		private string Host = "127.0.0.1";
		[SerializeField]
		[OnValueChanged(nameof(GetAll))]
		private ushort ServerPort = 7777;
		[SerializeField]
		[HideIf(nameof(TransportOption), TransportOption.WebSocketTransport)]
		[OnValueChanged(nameof(GetAll))]
		private ushort ClientPort = 7778;
		[SerializeField]
		[OnValueChanged(nameof(GetAll))]
		private uint MaxFps = 60;
		[SerializeField]
		[Range(1, 1500)]
		[OnValueChanged(nameof(GetAll))]
		private ushort MaxMessageSize = 256;
		[SerializeField]
		[Range(1, 1500)]
		[OnValueChanged(nameof(GetAll))]
		private ushort MaxConnections = 300;
		[SerializeField]
		[OnValueChanged(nameof(GetAll))]
		private byte Ttl = 62;

		private void GetAll()
		{
			switch (TransportOption)
			{
				case TransportOption.TcpTransport:
					OnTcpSettingsChanged();
					break;
				case TransportOption.LiteNetTransport:
					OnLiteNetSettingsChanged();
					break;
				case TransportOption.WebSocketTransport:
					break;
				default:
					throw new NotImplementedException("Transport Settings: Invalid transport!");
			}
		}

		private void SetAll(TransportSettings transportSettings)
		{
			transportSettings.Host = Host;
			transportSettings.ServerPort = ServerPort;
			transportSettings.ClientPort = ClientPort;
			transportSettings.MaxFps = MaxFps;
			transportSettings.MaxMessageSize = MaxMessageSize;
			transportSettings.MaxConnections = MaxConnections;
			transportSettings.Ttl = Ttl;
		}
		#endregion

		#region Tcp Transport
		[SerializeField]
		[ShowIf(nameof(TransportOption), TransportOption.TcpTransport)]
		[OnValueChanged(nameof(OnTcpSettingsChanged))]
		private bool noDelay = true;

		[SerializeField]
		[ShowIf(nameof(TransportOption), TransportOption.TcpTransport)]
		[OnValueChanged(nameof(OnTcpSettingsChanged))]
		private bool lingerState = false;

		[SerializeField]
		[ShowIf(nameof(TransportOption), TransportOption.TcpTransport)]
		[EnableIf("lingerState")]
		[OnValueChanged(nameof(OnTcpSettingsChanged))]
		private int lingerStateTime = 0;

		[SerializeField]
		[ShowIf(nameof(TransportOption), TransportOption.TcpTransport)]
		[OnValueChanged(nameof(OnTcpSettingsChanged))]
		private int sendBufferSize = 8192;

		[SerializeField]
		[ShowIf(nameof(TransportOption), TransportOption.TcpTransport)]
		[OnValueChanged(nameof(OnTcpSettingsChanged))]
		private int receiveBufferSize = 8192;

		[SerializeField]
		[ShowIf(nameof(TransportOption), TransportOption.TcpTransport)]
		[OnValueChanged(nameof(OnTcpSettingsChanged))]
		private int sendTimeout = 500;

		[SerializeField]
		[ShowIf(nameof(TransportOption), TransportOption.TcpTransport)]
		[OnValueChanged(nameof(OnTcpSettingsChanged))]
		private int receiveTimeout = 0;

		private void OnTcpSettingsChanged()
		{
			TransportSettings = new();
			TransportSettings.NoDelay = noDelay;
			TransportSettings.EnableLingerState = lingerState;
			TransportSettings.LingerStateTime = lingerStateTime;
			TransportSettings.SendBufferSize = sendBufferSize;
			TransportSettings.ReceiveBufferSize = receiveBufferSize;
			TransportSettings.SendTimeout = sendTimeout;
			TransportSettings.ReceiveTimeout = receiveTimeout;
			SetAll(TransportSettings);
		}
		#endregion

		#region WebSocket
		private void OnWebSocketSettingsChanged()
		{
			TransportSettings = new();
			SetAll(TransportSettings);
		}
		#endregion
	}
}