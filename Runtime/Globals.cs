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
        iRPCPlayer = 5,
        iRPCInstantiated = 6,
        iRPCScene = 7,
        Instantiate = 8,
        Connect = 254,
        Disconnect = 255,
    }

    public enum Channel : byte
    {
        Unreliable = 0,
        Reliable = 1,
        ReliableAndOrderly = 2,
    }

    public enum Target : byte
    {
        Server = 0,
        All = 1,
        Others = 2,
        Me = 3,
    }

    internal enum ObjectType : byte
    {
        Player = 0,
        Instantiated = 1,
        Scene = 2,
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

        internal static int GetAvailableId<T>(T[] array, Func<T, int> predicate, int maxRange)
        {
            var ids = array.Select(predicate);
            if (maxRange == ids.Count())
                return maxRange;
            return Enumerable.Range(0, maxRange).Except(ids).ToArray()[0];
        }

#if UNITY_EDITOR
        public static void SetDefine(bool remove = false, string except = "", params string[] defines)
        {
            BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            var definedSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup).Split(';').ToList();

            if (!string.IsNullOrEmpty(except))
            {
                var _except = except.Split(';').ToList();
                definedSymbols.RemoveAll(x => _except.Contains(x));
            }

            for (int i = 0; i < defines.Length; i++)
            {
                string def = defines[i];
                if (remove) definedSymbols.Remove(def);
                else if (!definedSymbols.Contains(def))
                {
                    if (def.ToUpper().Contains("_REMOVED")) definedSymbols.Remove(def.Replace("_REMOVED", "").Replace("_removed", ""));
                    else definedSymbols.Add(def);
                }
            }

            string symbols = string.Join(";", definedSymbols.ToArray());
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

        public static void Send(this ByteStream value, Channel channel = Channel.Unreliable, Target target = Target.Me, int playerId = 0) => NeutronNetwork.Send(value, channel, target, playerId);
    }
}