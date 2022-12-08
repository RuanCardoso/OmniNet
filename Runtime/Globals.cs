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
using System.Buffers;
using System.Linq;
using System.Net;
using MessagePack;
using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Neutron.Core
{
    public interface ISerializable
    {
        int Id { get; }
    }

    internal enum MessageType : byte
    {
        None = 0,
        StressTest = 1,
        Acknowledgement = 2,
        Zone = 3,
        GlobalMessage = 4,
        RemotePlayer = 5,
        RemoteInstantiated = 6,
        RemoteScene = 7,
        RemoteStatic = 8,
        Ping = 9,
        Connect = 254,
        Disconnect = 255,
    }

    public enum Channel : byte
    {
        Unreliable = 0,
        Reliable = 1,
    }

    public enum Target : byte
    {
        Server = 0,
        All = 1,
        Others = 2,
        Me = 3,
    }

    public enum SubTarget : byte
    {
        None,
        Server
    }

    internal enum ObjectType : byte
    {
        Player = 0,
        Instantiated = 1,
        Scene = 2,
        Static = 3,
    }

    internal enum SizeUnits
    {
        Byte, KB, MB, GB, TB, PB, EB, ZB, YB
    }

    internal enum LocalPhysicsMode
    {
        Physics2D = 0x1,
        Physics3D = 0x2
    }

    internal enum EncodingType : int
    {
        UTF8,
        UTF7,
        UTF32,
        ASCII,
        Unicode,
    }

    internal static class Helper
    {
        internal static int GetFreePort()
        {
            System.Net.Sockets.UdpClient udpClient = new(new IPEndPoint(IPAddress.Any, 0));
            IPEndPoint endPoint = (IPEndPoint)udpClient.Client.LocalEndPoint;
            int port = endPoint.Port;
            udpClient.Close();
            return port;
        }

        internal static int GetAvailableId<T>(T[] array, Func<T, int> predicate, int maxRange, int minRange = 0)
        {
            var ids = array.Select(predicate);
#pragma warning disable IDE0046
            if (maxRange == ids.Count())
                return maxRange;
#pragma warning restore IDE0046
            return Enumerable.Range(minRange, maxRange).Except(ids).ToArray()[0];
        }

#if UNITY_EDITOR
        internal static List<string> GetDefines(out BuildTargetGroup targetGroup)
        {
            BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
            targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
#if UNITY_SERVER
            var symbols = PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Server).Split(';').ToList();
#else
            var symbols = PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(targetGroup)).Split(';').ToList();
#endif
            return symbols;
        }

        internal static void SetDefines(params NeutronDefine[] defines)
        {
            List<string> definedSymbols = GetDefines(out var targetGroup);
            for (int i = 0; i < defines.Length; i++)
            {
                NeutronDefine define = defines[i];
                if (define.enabled)
                {
                    if (!definedSymbols.Contains(define.define)) definedSymbols.Add(define.define);
                    else { /* the symbol has already been defined */ }
                }
                else
                {
                    if (definedSymbols.Contains(define.define)) definedSymbols.Remove(define.define);
                    else { /* the symbol has already been removed */ }
                }
            }

            string symbols = string.Join(';', definedSymbols);
#if UNITY_SERVER
            PlayerSettings.SetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Server, symbols);
#else
            PlayerSettings.SetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(targetGroup), symbols);
#endif
        }
#endif
    }

    public static class Extensions
    {
        internal static string ToSize(this long value, SizeUnits unit) => (value / (double)Math.Pow(1024, (long)unit)).ToString("0.00");
        public static bool InBounds<T>(this T[] array, int index) => (index >= 0) && (index < array.Length);
        public static ByteStream Pack<T>(this T value, MessagePackSerializerOptions options = null) where T : ISerializable
        {
            byte[] data = MessagePackSerializer.Serialize<T>(value, options);
            ByteStream byteStream = ByteStream.Get();
            byteStream.WritePacket(MessageType.GlobalMessage);
            byteStream.Write(value.Id);
            byteStream.Write(data, 0, data.Length);
            return byteStream;
        }

        public static ByteStream PackTwo<T>(this T value, MessagePackSerializerOptions options = null) where T : ISerializable
        {
            IBufferWriter<byte> bufferWriter = new ArrayBufferWriter<byte>(1000);
            MessagePackSerializer.Serialize<T>(bufferWriter, value, options);
            // ByteStream byteStream = ByteStream.Get();
            // byteStream.WritePacket(MessageType.GlobalMessage);
            // byteStream.Write(value.Id);
            //byteStream.Write(bufferWriter.GetSpan());
            return default;
        }

        public static T Unpack<T>(this ByteStream value, MessagePackSerializerOptions options = null)
        {
            ReadOnlyMemory<byte> data = value.Buffer;
            return MessagePackSerializer.Deserialize<T>(data[..value.BytesWritten], options);
        }

        //public static void Send(this ByteStream value, Channel channel = Channel.Unreliable, Target target = Target.Me, int playerId = 0) => NeutronNetwork.Send(value, channel, target, playerId);
    }

    [Serializable]
    internal class LocalSettings
    {
        [Serializable]
        public class Host
        {
            [SerializeField] internal string name;
            [SerializeField] internal string host;
            [SerializeField] internal int port;
        }

        [ReadOnly][AllowNesting] public string name = "No Plataform!";
#if UNITY_SERVER
        [HideInInspector]
#endif
        public Host[] hosts = {
            new Host() { host = "127.0.0.1", name = "localhost", port = 5055 } ,
            new Host() { host = "0.0.0.0", name = "WSL", port = 5055 } ,
            new Host() { host = "0.0.0.0", name = "Cloud Server", port = 5055 } ,
        };

        [HideInInspector] public bool enabled;
        [Range(30, byte.MaxValue * 128)] public int maxFramerate = 60;
#if NEUTRON_MULTI_THREADED
        [HideInInspector]
#endif
        [Range(1, byte.MaxValue * 8)] public int recvMultiplier = 1;
        [Range(0, 5f)] public double ackTimeout = 0.3f; // seconds
        [Range(1, 1000)] public int ackSweep = 15; // ms
        [Min(128)] public int recvBufferSize = 8192;
        [Min(128)] public int sendBufferSize = 8192;
        [Min(0)][HideInInspector] public int recvTimeout = 0;
        [Min(0)][HideInInspector] public int sendTimeout = 0;
    }
}