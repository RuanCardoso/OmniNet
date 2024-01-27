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
using System.Collections;
using System.Net;
using UnityEngine;

namespace Omni.Core
{
	[DefaultExecutionOrder(-500)]
	public class NtpServer : RealtimeTickBasedSystem
	{
		#region Ntp
		// https://pt.wikipedia.org/wiki/Network_Time_Protocol
		public class NtpProtocol
		{
			// NTP significa Network Time Protocol ou Protocolo de Tempo para Redes.
			// É o padrão que permite a sincronização dos relógios dos dispositivos de uma rede como servidores, estações de trabalho, roteadores e outros equipamentos à partir de referências de tempo confiáveis.
			// Além do protocolo de comunicação em si, o NTP define uma série de algoritmos utilizados para consultar os servidores, calcular a diferença de tempo e estimar um erro, escolher as melhores referências e ajustar o relógio local.
			private readonly NtpTransport ntpServer = new();
			private readonly NtpTransport ntpClient = new();

			private double accuracy = 0.5d;

			public event Func<double> OnServerT; // server time
			public event Func<double> OnClientT; // client time

			public SimpleMovingAverage RttExAvg { get; private set; } = new SimpleMovingAverage(30);
			// O algoritmo de Exponential Moving Average (EMA) pode desempenhar um papel valioso em sistemas de sincronização de tempo, como o Protocolo de Tempo de Rede (NTP).
			// Ao aplicar o EMA aos dados temporais fornecidos pelos servidores NTP, é possível suavizar variações abruptas nos tempos e detectar tendências de deriva temporal de forma eficaz.
			// A sensibilidade do EMA aos dados mais recentes permite uma resposta ágil a mudanças nos tempos de sincronização, contribuindo para uma adaptação mais rápida e precisa dos relógios dos dispositivos na rede.
			// Essa abordagem suavizada não apenas melhora a estabilidade na sincronização, mitigando flutuações temporais, mas também oferece uma base sólida para detectar e corrigir desvios temporais, contribuindo assim para a manutenção da precisão do tempo em ambientes que dependem fortemente de sincronização temporal, como é o caso em sistemas NTP.
			public ExponentialMovingAverage OffsetExAvg { get; private set; }

			public void Awake(int serverPort = 1023, int clientPort = 1025, int windowSize = 3)
			{
				OffsetExAvg = new ExponentialMovingAverage(windowSize);
				if (NetworkHelper.IsAvailablePort(serverPort))
				{
					ntpServer.Bind(new IPEndPoint(IPAddress.Any, serverPort));
					OmniLogger.Print($"The NTP Server was started on port: {serverPort}");
				}
				else
				{
					OmniLogger.Print($"An Ntp server is already running on port {serverPort}, but it seems uninitialized in this instance. The application will continue.");
				}

				if (NetworkHelper.IsAvailablePort(clientPort))
				{
					ntpClient.Bind(new IPEndPoint(IPAddress.Any, clientPort));
				}
				else
				{
					clientPort = new System.Random().Next(clientPort, ushort.MaxValue);
					ntpClient.Bind(new IPEndPoint(IPAddress.Any, clientPort));
				}
			}

			public void Start(double accuracy)
			{
				this.accuracy = accuracy;
				ntpServer.OnDataReceived += NtpServer_OnDataReceived;
				ntpClient.OnDataReceived += NtpClient_OnDataReceived;
			}

			public void Update()
			{
				ntpServer.Receive();
				ntpClient.Receive();
			}

			// https://ntp.br/conteudo/ntp/#:~:text=NTP%20significa%20Network%20Time%20Protocol,de%20refer%C3%AAncias%20de%20tempo%20confi%C3%A1veis.
			// https://info.support.huawei.com/info-finder/encyclopedia/en/NTP.html
			private void NtpClient_OnDataReceived(byte[] data, int len, EndPoint endPoint)
			{
				if (OnClientT != null)
				{
					double b = OnClientT.Invoke(); // client time

					IDataReader reader = new DataReader(50);
					reader.Write(data, 0, len);

					double a = reader.ReadDouble();
					double x = reader.ReadDouble();
					double y = reader.ReadDouble();

					// rtt (atraso) = (b-a)-(y-x) = .
					RttExAvg.Add((b - a) - (y - x));
					// considerando-se que o tempo de ida é igual ao tempo de volta, pode-se calcular o deslocamento entre o servidor e o relógio local como:
					// offset = ((T2-T1) + (T3-T4))/2 = .
					OffsetExAvg.Add(((x - a) + (y - b)) * accuracy);
				}
			}

			// https://techhub.hpe.com/eginfolib/networking/docs/switches/5820x-5800/5998-7395r_nmm_cg/content/441755722.htm
			private void NtpServer_OnDataReceived(byte[] data, int len, EndPoint endPoint)
			{
				if (OnServerT != null)
				{
					double x = OnServerT.Invoke(); // server time

					IDataReader reader = new DataReader(50);
					reader.Write(data, 0, len);

					double a = reader.ReadDouble();

					IDataWriter writer = new DataWriter(50);
					writer.Write(a);
					writer.Write(x);
					double y = OnServerT.Invoke(); // server time
					writer.Write(y);
					ntpServer.Send(writer.Buffer, writer.BytesWritten, endPoint);
				}
			}

			// O Cliente lê seu relógio, que fornece o tempo a.
			// O Cliente envia a Mensagem 1 com a informação de tempo a para o servidor.
			// O Servidor recebe a Mensagem 1 e nesse instante lê seu relógio, que fornece o instante x.O Servidor mantém a e x em variáveis.
			// O Servidor após algum tempo lê novamente seu relógio, que fornece o instante y.
			// O Servidor envia a Mensagem 2 com a, x e y para o cliente.
			// O Cliente recebe a Mensagem 2 e nesse instante lê seu relógio, que fornece o instante b.
			public void Query(IPEndPoint endPoint)
			{
				if (OnClientT != null)
				{
					IDataWriter writer = new DataWriter(50);
					writer.Write(OnClientT.Invoke());
					ntpClient.Send(writer.Buffer, writer.BytesWritten, endPoint);
				}
			}

			public void Close()
			{
				ntpServer.Close();
				ntpClient.Close();
			}
		}
		#endregion

		private readonly NtpProtocol ntpProtocol = new NtpProtocol();
		private IPEndPoint queryPeer;
		private long ticks;

		[SerializeField]
		private ushort serverPort = 1023;
		[SerializeField]
		private ushort clientPort = 1025;
		[InfoBox("Adjust precision settings to fit your reality and gaming style; maybe the default setting is not recommended. Evaluate your use case carefully.")]
		[Header("Accuracy Settings")]
		[SerializeField]
		[MinValue(1)]
		private int sampleWindow = 3;
		[SerializeField]
		[Range(0f, 1f)]
		private double accuracy = 0.5d;
		[SerializeField]
		[Range(1f, 3600f)]
		[Label("Query Interval(Sec)")] private float queryInterval = 5;

		internal double SynchronizedTime
		{
			get
			{
#pragma warning disable IDE0046
				if (OmniNetwork.Omni.LoopMode == GameLoopOption.TickBased)
				{
					return (long)(ticks + ntpProtocol.OffsetExAvg.GetAverage());
				}
				else
				{
					return Math.Round(Time.timeAsDouble + ntpProtocol.OffsetExAvg.GetAverage(), 3);
				}
#pragma warning restore IDE0046
			}
		}

		internal double Latency => ntpProtocol.RttExAvg.GetAverage();

		private void Awake()
		{
			queryPeer = new IPEndPoint(IPAddress.Parse(OmniNetwork.Omni.TransportSettings.Host), serverPort);
			ntpProtocol.OnServerT += () => OmniNetwork.Omni.LoopMode == GameLoopOption.TickBased ? ticks : Time.timeAsDouble;
			ntpProtocol.OnClientT += () => OmniNetwork.Omni.LoopMode == GameLoopOption.TickBased ? ticks : Time.timeAsDouble;
			ntpProtocol.Awake(serverPort, clientPort, sampleWindow);
		}

		public override void Start()
		{
			base.Start();
			ntpProtocol.Start(accuracy);
			StartCoroutine(Query());
		}

		private void Update()
		{
			if (OmniNetwork.Omni.LoopMode == GameLoopOption.RealTime)
			{
				ntpProtocol.Update();
			}
		}

		public override void OnUpdateTick(ITickData tick)
		{
			if (OmniNetwork.Omni.LoopMode == GameLoopOption.TickBased)
			{
				ntpProtocol.Update();
			}
			ticks++;
		}

		private IEnumerator Query()
		{
			// The purpose of these calls before the while loop may be to ensure that the system clock is initially synchronized before entering the continuous query cycle. 
			// This can be helpful to prevent situations where the system clock is not immediately synchronized when the application starts.
			// 
			// Furthermore, the introduction of these initial pauses may serve as a startup measure to allow the system time to stabilize before initiating the repetitive querying of the NTP server. 
			// This can be particularly useful if there are other startup or configuration operations that need to occur before the system is fully ready to synchronize the clock continuously.
			ntpProtocol.Query(queryPeer);
			yield return new WaitForSeconds(1f);
			ntpProtocol.Query(queryPeer);
			yield return new WaitForSeconds(1f);
			ntpProtocol.Query(queryPeer);
			yield return new WaitForSeconds(1f);
			// Continuous clock synchronization......
			while (true)
			{
				ntpProtocol.Query(queryPeer);
				yield return new WaitForSeconds(queryInterval);
			}
		}

		private void OnApplicationQuit()
		{
			ntpProtocol.Close();
		}
	}
}
