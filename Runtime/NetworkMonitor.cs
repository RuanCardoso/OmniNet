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

using Omni.Core;
using Omni.Internal.Interfaces;
using System;
using UnityEngine;

#pragma warning disable

namespace Omni.Internal
{
	public class NetworkMonitor : MonoBehaviour
	{
		private class NetworkInfor
		{
			private Func<double, double, string> Format { get; }
			private Func<double, double> PropertyCurrentValue { get; }
			private double PropertyTx { get; set; }
			private double PropertyLastValue { get; set; }
			private double PropertySinceLastCheck { get; set; }
			private double PropertyDeltaTime { get; set; }

			internal NetworkInfor(Func<double, double> propertyCurrentValue, Func<double, double, string> format)
			{
				PropertyCurrentValue = propertyCurrentValue;
				Format = format;
			}

			internal void Update()
			{
				PropertyDeltaTime += Time.deltaTime;
				PropertySinceLastCheck = PropertyCurrentValue(PropertyLastValue) - PropertyLastValue;

				if (PropertyDeltaTime >= 1)
				{
					PropertyLastValue = PropertyCurrentValue(PropertyLastValue);
					PropertyTx = PropertySinceLastCheck / PropertyDeltaTime;

					// Reinicia as variáveis para a próxima verificação
					PropertySinceLastCheck = 0;
					PropertyDeltaTime = 0;
				}
			}

			public override string ToString()
			{
				double propertyCurrentValue = PropertyCurrentValue(PropertyLastValue);
				return string.Format(Format(propertyCurrentValue, PropertyTx), propertyCurrentValue, PropertyTx);
			}
		}

		private static ITransport ServerTransport => OmniNetwork.Main.ServerTransport;
		private static ITransport ClientTransport => OmniNetwork.Main.ClientTransport;

		[SerializeField] private bool isEnabled = true;
		[SerializeField][Range(1, 50)] private int fontSize = 18;
		[SerializeField] private Vector2 minSizePercentage = new Vector2(0.28f, 0.25f);

		private NetworkInfor[] clientProperties = new NetworkInfor[]
		{
			new NetworkInfor((lastValue) => ClientTransport.TotalMessagesSent, (value, tx) => "Total Messages Sent: {0} [{1:0} per second]"),
			new NetworkInfor((lastValue) => ClientTransport.TotalMessagesReceived, (value, tx) => "Total Messages Received: {0} [{1:0} per second]"),
			new NetworkInfor((lastValue) => ClientTransport.TotalBytesSent, (value, tx) => "Total Bytes Sent: {0} [{1:0} per second]"),
			new NetworkInfor((lastValue) => ClientTransport.TotalBytesReceived, (value, tx) => "Total Bytes Received: {0} [{1:0} per second]"),
			new NetworkInfor((lastValue) => ClientTransport.PacketLossPercent, (value, tx) => "Packet Loss: {0}%"),
			//new NetworkInfor((lastValue) => 0, (value, tx) => $"Time: {OmniNet.NetworkCommunicator.DateTime.ToString("dd-MM-yyyy HH:mm:ss.fff")}"),
			//new NetworkInfor((lastValue) => OmniNet.NetworkCommunicator.ClientTicks, (value, tx) => "Client Ticks: {0}"),
			//new NetworkInfor((lastValue) => OmniNet.NetworkCommunicator.Ticks.GetAverage(), (value, tx) => "Ticks: {0}"),
			//new NetworkInfor((lastValue) => (Communicator.obsoleteServerTime - Communicator.obsoleteRemoteTime), (value, tx) => "Remote Time Accuracy(Editor Only): {0:0.####} seconds" + $" [{(value * 1000):0} ms]"),
			new NetworkInfor((lastValue) => 0, (value, tx) => "FPS {0}"),
			new NetworkInfor((lastValue) => OmniNetwork.Time.Latency, (value, tx) => "Latency: {0}"),
			//new NetworkInfor((lastValue) => OmniTime.Time, (value, tx) => "Time: {0}"),
		};

		private NetworkInfor[] serverProperties = new NetworkInfor[]
		{
			new NetworkInfor((lastValue) => ServerTransport.TotalMessagesSent, (value, tx) => "Total Messages Sent: {0} [{1:0} per second]"),
			new NetworkInfor((lastValue) => ServerTransport.TotalMessagesReceived, (value, tx) => "Total Messages Received: {0} [{1:0} per second]"),
			new NetworkInfor((lastValue) => ServerTransport.TotalBytesSent,(value, tx) => "Total Bytes Sent: {0} [{1:0} per second]"),
			new NetworkInfor((lastValue) => ServerTransport.TotalBytesReceived, (value, tx) => "Total Bytes Received: {0} [{1:0} per second]"),
			new NetworkInfor((lastValue) => ServerTransport.PacketLossPercent, (value, tx) => "Packet Loss: {0}%"),
			//new NetworkInfor((lastValue) => 0, (value, tx) => $"Time: {OmniNet.NetworkCommunicator.Time.Value.ToString("dd-MM-yyyy HH:mm:ss.fff")}"),
			//new NetworkInfor((lastValue) => OmniNet.NetworkCommunicator.ServerTicks, (value, tx) => "Server Ticks: {0}"),
		};

		private void Update()
		{
			if (ClientTransport != null && ClientTransport.IsConnected)
			{
				foreach (var prop in clientProperties)
				{
					prop.Update();
				}
			}

			if (ServerTransport != null && ServerTransport.IsInitialized)
			{
				foreach (var prop in serverProperties)
				{
					prop.Update();
				}
			}
		}

#if UNITY_EDITOR || !UNITY_SERVER
		private float titleSpacing = 10f;
		private GUIStyle titleStyle;
		private GUIStyle labelStyle;
		private GUIStyle buttonStyle;
		private void OnGUI()
		{
			float screenWidth = Screen.width;
			float screenHeight = Screen.height;

			labelStyle ??= new GUIStyle()
			{
				normal = { textColor = Color.white },
				fontSize = fontSize,
			};

			titleStyle ??= new GUIStyle(GUI.skin.box)
			{
				normal = { textColor = Color.white },
				alignment = TextAnchor.UpperCenter,
				fontSize = fontSize,
			};

			buttonStyle ??= new GUIStyle(GUI.skin.button)
			{
				normal = { textColor = Color.white },
				alignment = TextAnchor.MiddleCenter,
				fontSize = 20,
			};

			titleStyle.fontSize = 20;
			labelStyle.fontSize = fontSize;

			if (isEnabled)
			{
				float xSize = 0;
				if (ServerTransport != null && ServerTransport.IsInitialized)
				{
					DrawNetworkInfor(serverProperties, "Server", 0, out xSize);
				}
				else
				{
					if (ClientTransport == null)
					{
						if (GUI.Button(new Rect((screenWidth / 2) - (150 / 2), (screenHeight / 2) - (50 / 2), 150, 50), "Start Server", buttonStyle))
						{
							OmniNetwork.Main.InitializeTransport();
						}
					}
				}

				if (ClientTransport != null && ClientTransport.IsConnected)
				{
					DrawNetworkInfor(clientProperties, "Client", xSize + 10f, out xSize);
				}
				else
				{
					if (GUI.Button(new Rect((screenWidth / 2) - (200 / 2), ((screenHeight / 2) - (50 / 2)) + 60, 200, 50), "Start Client & Server", buttonStyle))
					{
						OmniNetwork.Main.InitializeTransport();
						OmniNetwork.Main.InitializeConnection();
					}
				}
			}
		}
#endif
#if UNITY_EDITOR || !UNITY_SERVER
		private void DrawNetworkInfor(NetworkInfor[] props, string name, float xOffset, out float xSize)
		{
			xSize = 0;
			// Get screen dimensions
			float screenWidth = Screen.width;
			float screenHeight = Screen.height;

			// Calculate title size
			Vector2 titleSize = GUI.skin.label.CalcSize(new GUIContent(name));

			// Calculate minimum size based on screen percentage
			Vector2 minSize = new Vector2(screenWidth * minSizePercentage.x, screenHeight * minSizePercentage.y);

			// Calculate box size based on title size, extra spacing, and labels
			Vector2 boxSize = new Vector2(titleSize.x, titleSize.y + titleSpacing + (props.Length * fontSize)); // Assuming each label has a height of 20

			// Ensure that the box size is not less than the minimum size
			boxSize = Vector2.Max(boxSize, minSize);

			// Calculate the position for the box on the right side of the screen
			float boxPosX = screenWidth - boxSize.x - 10 - xOffset; // 10 is an additional spacing from the right edge
			float boxPosY = 10; // Adjust the value as needed

			xSize = boxSize.x;

			// Draw the box based on the calculated size
			GUI.Box(new Rect(boxPosX, boxPosY, boxSize.x, boxSize.y), name, titleStyle);

			// Calculate the position for the first label inside the box
			float labelPosY = boxPosY + titleSize.y + titleSpacing;

			foreach (var prop in props)
			{
				// Draw labels inside the box
				GUI.Label(new Rect(boxPosX + 20, labelPosY, boxSize.x - 40, fontSize), prop.ToString(), labelStyle); // Adjust the Rect as needed
				labelPosY += fontSize + 5; // Assuming each label has a height of 20
			}
		}
#endif
	}
}
