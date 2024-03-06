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

using Newtonsoft.Json.Utilities;
using Omni.Core.Cryptography;
using Omni.Core.IMatchmaking;
using Omni.Core.Web;
using Omni.Internal;
using Omni.Internal.Interfaces;
using Omni.Internal.Transport;
using System;
using System.Net;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace Omni.Core
{
	[RequireComponent(typeof(NetworkCommunicator))]
	[RequireComponent(typeof(NetworkMonitor))]
	[RequireComponent(typeof(NetworkTime))]
	[RequireComponent(typeof(NtpServer))]
	[RequireComponent(typeof(PortForwarding))]
	[RequireComponent(typeof(Matchmaking))]
	[DefaultExecutionOrder(-3000)] // Priority
	public partial class OmniNetwork : RealtimeTickBasedSystem // Part1
	{
		const string SceneName = "Omni Server(Debug Mode)";

		public static OmniNetwork Main { get; private set; }
		public static NtpServer Ntp { get; private set; }
		public static NetworkTime Time { get; private set; }
		public static Matchmaking Matchmaking { get; private set; }
		public static NetworkCommunicator Communicator { get; private set; }

		public string Guid { get => guid; set => guid = value; }
		public NetworkDispatcher NetworkDispatcher { get; private set; }
		public GameLoopOption LoopMode => loopMode;
		public TransportOption TransportOption => transportOption;
		public int IOPS { get => m_IOPS; set => m_IOPS = value; }
		public bool HasServer => ServerTransport != null && ServerTransport.IsInitialized;
		public bool HasClient => ClientTransport != null && ClientTransport.IsInitialized && ClientTransport.IsConnected;

#if UNITY_SERVER && !UNITY_EDITOR
		public bool IsConnected => HasServer;
#else
		public bool IsConnected => HasClient;
#endif
		public TransportSettings TransportSettings { get; private set; }
		public Scene? EditorScene { get; private set; }

		internal int ManagedThreadId { get; private set; }
		internal ITransport ServerTransport { get; private set; }
		internal ITransport ClientTransport { get; private set; }

		#region Rsa && Aes
		internal string PublicKey { get; private set; }
		internal string PrivateKey { get; private set; }
		internal byte[] AesKey { get; private set; }
		#endregion

		private bool m_IsInitialized;

		private void Awake()
		{
			#region Instance
			Main = this;
			_ = NetworkHelper.AddResolver(null);
			AotHelper.EnsureDictionary<string, object>();
			DontDestroyOnLoad(gameObject);
			#endregion

			#region Components
			NetworkDispatcher = new NetworkDispatcher(false);
			Communicator = GetComponent<NetworkCommunicator>();
			Matchmaking = GetComponent<Matchmaking>();
			Time = GetComponent<NetworkTime>();
			Ntp = GetComponent<NtpServer>();
			#endregion

			#region Initialize
			ManagedThreadId = Thread.CurrentThread.ManagedThreadId;
			SimpleHttpProtocol.AddEventListener();
			InitializeTransport();
			#endregion

			// Let's generate a pair of RSA keys for exchanging aes keys between client and server.
			// Only the 'server' will generate the key pair.
			#region Rsa && Aes
			if (HasServer)
			{
				RsaCryptography.GetRsaKeys(out string privateKey, out string publicKey);
				if (privateKey.Length > 0 && publicKey.Length > 0)
				{
					PublicKey = StringCipher.Encrypt(publicKey, Guid);
					PrivateKey = privateKey;
				}
				else throw new Exception("Error generating RSA keys. Please verify that the generation process is working correctly.");
			}
#if !UNITY_SERVER || UNITY_EDITOR
			AesKey = AesCryptography.GenerateKey();
#endif
			#endregion
		}

		public override void Start()
		{
			base.Start();
			// The server build has no client!
#if !UNITY_SERVER || UNITY_EDITOR
			InitializeConnection();
#endif
			CheckGarbageCollectorSettings();
			CheckApiModeSettings();
		}

		private void Update()
		{
			if (Main.LoopMode == GameLoopOption.RealTime)
			{
				Simulate();
			}

			NetworkDispatcher.Process();
		}

		// Simulate the scene on FixedUpdate.
		private void FixedUpdate()
		{
			if (EditorScene.HasValue)
			{
				if (!EditorScene.Value.IsValid())
				{
					return;
				}
			}

			if (physicsMode == LocalPhysicsMode.Physics3D)
			{
				PhysicsScene? physicsScene = EditorScene?.GetPhysicsScene();
				physicsScene?.Simulate(UnityEngine.Time.fixedDeltaTime);
			}

			if (physicsMode == LocalPhysicsMode.Physics2D)
			{
				PhysicsScene2D? physicsScene = EditorScene?.GetPhysicsScene2D();
				physicsScene?.Simulate(UnityEngine.Time.fixedDeltaTime);
			}
		}

		public override void OnUpdateTick(ITickData tick)
		{
			if (Main.LoopMode == GameLoopOption.TickBased)
			{
				Simulate();
			}
		}

		private void Simulate()
		{
			for (int i = 0; i < IOPS; i++)
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

			NetProtocol protocol = TransportOption switch
			{
				TransportOption.LiteNetTransport => NetProtocol.Udp,
				TransportOption.TcpTransport => NetProtocol.Tcp,
				TransportOption.WebSocketTransport => NetProtocol.WebSocket,
				_ => throw new Exception("Invalid protocol!"),
			};

			if (Matchmaking != null)
			{
				Matchmaking.ProcessEvents();
			}

			if (Communicator != null)
			{
				Communicator.ProcessEvents();
			}

			if (m_OnTransportSettings != null) m_OnTransportSettings.OnTransportSettings(TransportSettings, Application.platform);
			else OmniLogger.Print("The platform-specific transport configuration is disabled.");
			// The Server does not start in WebGl as it is not supported in browsers, it must be run outside of a browser environment.
#if !UNITY_WEBGL || UNITY_EDITOR
			if (NetworkHelper.IsAvailablePort(TransportSettings.ServerPort, protocol))
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
#endif

			if (NetworkHelper.IsAvailablePort(TransportSettings.ClientPort, protocol))
			{
				ClientTransport.InitializeTransport(false, new IPEndPoint(IPAddress.Any, TransportSettings.ClientPort), TransportSettings);
			}
			else
			{
				TransportSettings.ClientPort = (ushort)new System.Random().Next(TransportSettings.ClientPort, ushort.MaxValue);
				ClientTransport.InitializeTransport(false, new IPEndPoint(IPAddress.Any, TransportSettings.ClientPort), TransportSettings);
			}

			if (enableWebServer)
			{
				NetworkHttpServer.WebServer = new NetworkHttpCommunicator();
				NetworkHttpServer.WebServer.Initialize();
			}
		}

		private void CreateServerSimulation()
		{
			if (HasServer)
			{
				if (Application.isPlaying)
				{
					EditorScene = SceneManager.CreateScene(SceneName, new CreateSceneParameters((UnityEngine.SceneManagement.LocalPhysicsMode)physicsMode));
				}
			}
		}

		private void CheckGarbageCollectorSettings()
		{
			if (!GarbageCollector.isIncremental)
			{
				OmniLogger.Print("Consider enabling \"Incremental Garbage Collection\" for improved performance. This option can maximize performance by efficiently managing memory usage during runtime.");
			}
		}

		private void CheckApiModeSettings()
		{
#if !NETSTANDARD2_1
            OmniLogger.Print("Consider changing the API Mode to \".NET Standard 2.1\" for improved performance and compatibility. .NET Standard 2.1 offers enhanced features, performance optimizations, and broader library support, resulting in better performance and increased functionality for your application.");
#endif
#if !ENABLE_IL2CPP && !UNITY_EDITOR
            OmniLogger.Print("Consider changing the API Mode to \"IL2CPP\" for optimal performance. IL2CPP provides enhanced performance and security by converting your code into highly optimized C++ during the build process.");
#endif
		}

		public void OnApplicationQuit()
		{
			SimpleHttpProtocol.Close();
			if (ServerTransport != null && ServerTransport.IsInitialized)
			{
				ServerTransport.Close();
			}

			if (ClientTransport != null && ClientTransport.IsInitialized)
			{
				ClientTransport.Close();
			}

			if (enableWebServer)
			{
				NetworkHttpServer.WebServer.Close();
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

	public enum NetProtocol
	{
		Tcp,
		Udp,
		WebSocket
	}
}