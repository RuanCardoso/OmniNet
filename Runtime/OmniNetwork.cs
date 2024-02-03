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
using UnityEngine.SceneManagement;

namespace Omni.Core
{
	[RequireComponent(typeof(NetworkCommunicator))]
	[RequireComponent(typeof(NetworkMonitor))]
	[RequireComponent(typeof(NetworkTime))]
	[RequireComponent(typeof(NtpServer))]
	[DefaultExecutionOrder(-3000)]
	public partial class OmniNetwork : RealtimeTickBasedSystem
	{
		const string SceneName = "Omni Server(Debug Mode)";

		public static OmniNetwork Omni { get; private set; }
		public static NetworkCommunicator Communicator { get; private set; }
		public static NetworkTime Time { get; private set; }
		public static NtpServer Ntp { get; private set; }

		public NetworkDispatcher NetworkDispatcher { get; private set; }
		public GameLoopOption LoopMode => loopMode;
		public int IOPS { get => m_IOPS; set => m_IOPS = value; }
		public TransportOption TransportOption => transportOption;
		public bool HasServer => ServerTransport.IsInitialized;
		public Scene? ServerScene { get; private set; }

#if UNITY_SERVER && !UNITY_EDITOR
		public bool IsConnected => HasServer;
#else
		public bool IsConnected => ClientTransport.IsConnected;
#endif

		internal int ManagedThreadId { get; private set; }
		internal TransportSettings TransportSettings { get; private set; }
		internal ITransport ServerTransport { get; private set; }
		internal ITransport ClientTransport { get; private set; }

		private bool m_IsInitialized;

		private void Awake()
		{
			#region Instance
			Omni = this;
			#endregion

			#region Components
			NetworkDispatcher = new NetworkDispatcher();
			Communicator = GetComponent<NetworkCommunicator>();
			Time = GetComponent<NetworkTime>();
			Ntp = GetComponent<NtpServer>();
			#endregion

			#region Initialize
			SimpleHttpProtocol.AddEventListener();
			InitializeTransport();
			#endregion
		}

		protected override void Start()
		{
			base.Start();
			ManagedThreadId = Thread.CurrentThread.ManagedThreadId;
#if !UNITY_SERVER || UNITY_EDITOR
			InitializeConnection();
#endif
		}

		private void Update()
		{
			if (Omni.LoopMode == GameLoopOption.RealTime)
			{
				Simulate();
			}

			NetworkDispatcher.Process();
		}

		// Simulate the scene on FixedUpdate.
		private void FixedUpdate()
		{
			if (physicsMode == LocalPhysicsMode.Physics3D)
			{
				PhysicsScene? physicsScene = ServerScene?.GetPhysicsScene();
				physicsScene?.Simulate(UnityEngine.Time.fixedDeltaTime);
			}

			if (physicsMode == LocalPhysicsMode.Physics2D)
			{
				PhysicsScene2D? physicsScene = ServerScene?.GetPhysicsScene2D();
				physicsScene?.Simulate(UnityEngine.Time.fixedDeltaTime);
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
				if (TransportOption != TransportOption.WebSocketTransport)
				{
					ServerTransport.Receive();
				}
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
					ClientTransport.ConnectAsync(new IPEndPoint(IPAddress.Parse(TransportSettings.Host), TransportSettings.ServerPort));
				}
				else
				{
					ClientTransport.Connect(new IPEndPoint(IPAddress.Parse(TransportSettings.Host), TransportSettings.ServerPort));
				}
			}
		}

		internal void InitializeTransport()
		{
			if (m_IsInitialized)
			{
				return;
			}

			m_IsInitialized = true;
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
				case TransportOption.WebSocketTransport:
					{
						ServerTransport = new WebTransport();
						ClientTransport = new WebTransport();
						OnWebSocketSettingsChanged();
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
#if !UNITY_SERVER || UNITY_EDITOR
				CreateServerSimulation();
#endif
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

		private void CreateServerSimulation()
		{
			if (HasServer)
			{
				if (Application.isPlaying)
				{
					ServerScene = SceneManager.CreateScene(SceneName, new CreateSceneParameters((UnityEngine.SceneManagement.LocalPhysicsMode)physicsMode));
				}
			}
		}

		protected override void OnApplicationQuit()
		{
			base.OnApplicationQuit();
			SimpleHttpProtocol.Close();
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

public enum LocalPhysicsMode
{
	//
	// Resumo:
	//     A local 2D physics Scene will be created and owned by the Scene.
	Physics2D = 1,
	//
	// Resumo:
	//     A local 3D physics Scene will be created and owned by the Scene.
	Physics3D = 2
}
