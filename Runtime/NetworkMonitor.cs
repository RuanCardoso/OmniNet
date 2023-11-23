using System.Collections;
using UnityEngine;
using static Omni.Core.Enums;

namespace Omni.Core
{
    public class NetworkMonitor : MonoBehaviour
    {
        [SerializeField] private SizeUnits sizeUnit = SizeUnits.Byte;
        [SerializeField][Range(1, 50)] private int fontSize = 12;

        private ulong bytesSent;
        private ulong bytesReceived;
        private ulong packetsSent;
        private ulong packetsReceived;
        private ulong packetsDuplicated;
        private ulong packetsOutOfOrder;
        private ulong packetsRetransmitted;

        private GUIStyle titleStyle;
        private GUIStyle labelStyle;

        internal static ulong BytesSent { get; set; }
        internal static ulong BytesReceived { get; set; }
        internal static ulong PacketsSent { get; set; }
        internal static ulong PacketsReceived { get; set; }
        internal static ulong PacketsDuplicated { get; set; }
        internal static ulong PacketsOutOfOrder { get; set; }
        internal static ulong PacketsRetransmitted { get; set; }
        internal static ulong PacketsLost { get; set; }

        private void Start()
        {
            StartCoroutine(Flush());
        }

#if UNITY_EDITOR || !UNITY_SERVER
        private void OnGUI()
        {
            titleStyle ??= new GUIStyle(GUI.skin.textField)
            {
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter,
                fontSize = fontSize,
            };

            labelStyle ??= new GUIStyle()
            {
                normal = { textColor = Color.white },
                fontSize = fontSize,
            };

            titleStyle.fontSize = fontSize;
            labelStyle.fontSize = fontSize;

            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.BeginVertical(GUI.skin.box);
#if UNITY_EDITOR
            GUILayout.Label("Network Statistics (Server & Client)", titleStyle);
#elif !UNITY_SERVER
            GUILayout.Label("Network Statistics (Client)", titleStyle);
#endif
            GUILayout.Label($"Bytes Sent: {bytesSent.ToSizeUnit(sizeUnit)} {GetSizeUnit(sizeUnit)}", labelStyle);
            GUILayout.Label($"Bytes Received: {bytesReceived.ToSizeUnit(sizeUnit)} {GetSizeUnit(sizeUnit)}", labelStyle);
            GUILayout.Label($"Packets Sent: {packetsSent} p/s", labelStyle);
            GUILayout.Label($"Packets Received: {packetsReceived} p/s", labelStyle);
            GUILayout.Label($"Packets Duplicated: {packetsDuplicated} p/s", labelStyle);
            GUILayout.Label($"Packets Out of Order: {packetsOutOfOrder} p/s", labelStyle);
            GUILayout.Label($"Packets Retransmitted: {packetsRetransmitted} p/s", labelStyle);

            GUILayout.Label($"Packets Lost: {PacketsLost}%", labelStyle);
            GUILayout.Label($"Ping: {OmniTime.Ping} ms", labelStyle);
            GUILayout.Label($"Latency: {OmniTime.Latency} ms", labelStyle);
            GUILayout.Label($"Server Time: {OmniTime.Time}", labelStyle);
            GUILayout.Label($"Client Time: {OmniTime.LocalTime}", labelStyle);
            GUILayout.Label($"FPS: {OmniNetwork.Framerate}", labelStyle);

#if UNITY_EDITOR
            GUILayout.Space(10);
            GUILayout.Label("Within the editor, the network statistics for both the server and client are consolidated.\r\nTo view the statistics for the server and client individually, it is recommended to build the project.", labelStyle);
#endif
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
#endif

        WaitForSeconds _ = new(1);
        private IEnumerator Flush()
        {
            while (Application.isPlaying)
            {
                bytesSent = BytesSent;
                bytesReceived = BytesReceived;
                packetsSent = PacketsSent;
                packetsReceived = PacketsReceived;
                packetsDuplicated = PacketsDuplicated;
                packetsOutOfOrder = PacketsOutOfOrder;
                packetsRetransmitted = PacketsRetransmitted;

                BytesSent = 0;
                BytesReceived = 0;
                PacketsSent = 0;
                PacketsReceived = 0;
                PacketsDuplicated = 0;
                PacketsOutOfOrder = 0;
                PacketsRetransmitted = 0;

                yield return _;
            }
        }

        private string GetSizeUnit(SizeUnits sizeUnit)
        {
            return sizeUnit switch
            {
                SizeUnits.Byte => "b/s",
                SizeUnits.KB => "kb/s",
                SizeUnits.MB => "mb/s",
                SizeUnits.GB => "gb/s",
                SizeUnits.TB => "tb/s",
                _ => "b/s",
            };
        }
    }
}
