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
using Omni.Internal.Interfaces;
using Omni.Internal.Transport;
using System;
using System.Net;
using System.Threading;
using UnityEngine;

namespace Omni.Core
{
	[RequireComponent(typeof(NetworkCommunicator))]
	[RequireComponent(typeof(NetworkMonitor))]
	[RequireComponent(typeof(NetworkTime))]
	[RequireComponent(typeof(NtpServer))]
	[DefaultExecutionOrder(-3000)]
	public partial class OmniNetwork : RealtimeTickBasedSystem
	{
		public static OmniNetwork Omni { get; private set; }
		public static NetworkCommunicator Communicator { get; private set; }
		public static NetworkTime Time { get; private set; }
		public static NtpServer Ntp { get; private set; }

		public GameLoopOption LoopMode => loopMode;
		public int IOPS { get => m_IOPS; set => m_IOPS = value; }
		public TransportOption TransportOption { get => transportOption; set => transportOption = value; }

		internal int ManagedThreadId { get; private set; }
		internal TransportSettings TransportSettings { get; private set; }
		internal ITransport ServerTransport { get; private set; }
		internal ITransport ClientTransport { get; private set; }

		private bool IsInitialized { get; set; }

		private void Awake()
		{
			Omni = this;
			Communicator = GetComponent<NetworkCommunicator>();
			Time = GetComponent<NetworkTime>();
			Ntp = GetComponent<NtpServer>();
			InitializeTransport();
		}

		public override void Start()
		{
			base.Start();
			ManagedThreadId = Thread.CurrentThread.ManagedThreadId;
			InitializeConnection();
		}

		private void Update()
		{
			if (Omni.LoopMode == GameLoopOption.RealTime)
			{
				Simulate();
			}
		}

		public override void OnUpdateTick(ITickData tick)
		{
			if (Omni.LoopMode == GameLoopOption.TickBased)
			{
				Simulate();
			}
		}

		private void Simulate()
		{
			if (TransportOption == TransportOption.TcpTransport)
			{
				for (int i = 0; i < IOPS; i++)
				{
					Receive();
				}
			}
			else
			{
				Receive();
			}
		}

		private void Receive()
		{
			if (ServerTransport != null && ServerTransport.IsInitialized)
			{
				ServerTransport.Receive();
			}

			if (ClientTransport != null)
			{
				if (ClientTransport.IsInitialized && ClientTransport.IsConnected)
				{
					ClientTransport.Receive();
				}
			}
		}

		internal void InitializeConnection()
		{
			if (ClientTransport != null && ClientTransport.IsInitialized)
			{
				if (ConnectAsync)
				{
					ClientTransport.ConnectAsync(new IPEndPoint(IPAddress.Loopback, TransportSettings.ServerPort));
				}
				else
				{
					ClientTransport.Connect(new IPEndPoint(IPAddress.Loopback, TransportSettings.ServerPort));
				}
			}
		}

		internal void InitializeTransport()
		{
			if (IsInitialized)
			{
				return;
			}

			IsInitialized = true;
			switch (TransportOption)
			{
				case TransportOption.TcpTransport:
					{
						ServerTransport = new OmniTcpTransport();
						ClientTransport = new OmniTcpTransport();
						OnTcpSettingsChanged();
					}
					break;
				case TransportOption.LiteNetTransport:
					{
						ServerTransport = new LiteNetLibTransport();
						ClientTransport = new LiteNetLibTransport();
						OnLiteNetSettingsChanged();
					}
					break;
				default:
					throw new NotImplementedException("Omni: Invalid transport!");
			}

			if (Communicator != null)
			{
				Communicator.ProcessEvents();
			}

			if (OnTransportSettings != null)
			{
				if (OnTransportSettings.GetPersistentEventCount() == 0)
				{
					OmniLogger.Print("The platform-specific transport configuration is disabled.");
				}
				else
				{
					for (int i = 0; i < OnTransportSettings.GetPersistentEventCount(); i++)
					{
						if (OnTransportSettings.GetPersistentMethodName(i) == "")
						{
							OmniLogger.Print("Obs: The platform-specific transport configuration is disabled?");
						}
					}
				}
			}

			OnTransportSettings?.Invoke(TransportSettings, Application.platform);
			if (NetworkHelper.IsAvailablePort(TransportSettings.ServerPort))
			{
				ServerTransport.InitializeTransport(true, new IPEndPoint(IPAddress.Any, TransportSettings.ServerPort), TransportSettings);
			}
			else
			{
				OmniLogger.Print("There is an active server instance running, but it seems uninitialized in this instance. The application will continue.");
			}

			if (NetworkHelper.IsAvailablePort(TransportSettings.ClientPort))
			{
				ClientTransport.InitializeTransport(false, new IPEndPoint(IPAddress.Any, TransportSettings.ClientPort), TransportSettings);
			}
			else
			{
				TransportSettings.ClientPort = (ushort)new System.Random().Next(TransportSettings.ClientPort, ushort.MaxValue);
				ClientTransport.InitializeTransport(false, new IPEndPoint(IPAddress.Any, TransportSettings.ClientPort), TransportSettings);
			}
		}

		private void OnApplicationQuit()
		{
			if (ServerTransport != null && ServerTransport.IsInitialized)
			{
				ServerTransport.Close();
			}

			if (ClientTransport != null && ClientTransport.IsInitialized)
			{
				ClientTransport.Close();
			}
		}
	}
}
