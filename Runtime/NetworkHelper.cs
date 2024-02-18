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

using MessagePack;
using MessagePack.Resolvers;
using MessagePack.Unity;
using MessagePack.Unity.Extension;
using Omni.Core;
using Omni.Internal.Transport;
using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using WebSocketSharp.Server;

namespace Omni.Internal
{
	public static class NetworkHelper
	{
		internal static bool IsAvailablePort(int port, NetProtocol protocol)
		{
			// Sockets are not supported in browsers, we will always return false.
#if UNITY_WEBGL && !UNITY_EDITOR
			return false;
#endif
			WebSocketServer sck3 = null;
			try
			{
				if (protocol == NetProtocol.WebSocket)
				{
					TransportSettings transportSettings = OmniNetwork.Main.TransportSettings;
					string host = OmniNetwork.Main.TransportSettings.UseHttpsOnly ? $"wss://{transportSettings.Host}:{port}" : $"ws://{transportSettings.Host}:{port}";
					sck3 = new WebSocketServer(host);
					sck3.Start();
					sck3.Stop();
					return true;
				}
			}
			catch (Exception)
			{
				sck3?.Stop();
				return false;
			}

			Socket sck1 = null, sck2 = null;
			try
			{
				if (protocol == NetProtocol.Tcp)
				{
					sck1 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
					sck1.Bind(new IPEndPoint(IPAddress.Any, port));
					sck1.Close();
				}

				if (protocol == NetProtocol.Udp)
				{
					sck2 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
					sck2.Bind(new IPEndPoint(IPAddress.Any, port));
					sck2.Close();
				}

				return true;
			}
			catch (Exception)
			{
				sck1?.Close();
				sck2?.Close();
				return false;
			}
		}

		internal static void ThrowAnErrorIfConcurrent()
		{
			if (Thread.CurrentThread.ManagedThreadId != OmniNetwork.Main.ManagedThreadId)
			{
				OmniLogger.LogError("Omni: Unity does not support operations outside the main thread.");
				throw new AccessViolationException("Omni: Unity does not support operations outside the main thread.");
			}
		}

		internal static unsafe int GetInt32FromGenericEnum<T>(T Enum) where T : unmanaged, IComparable, IConvertible, IFormattable
		{
			if (Unsafe.SizeOf<T>() != Unsafe.SizeOf<int>())
			{
				OmniLogger.PrintError($"Error: Cannot retrieve Int32 value from the generic enum. The size of type {typeof(T)} is not compatible with Int32 size.");
				return 0;
			}

			return *(int*)(&Enum);
		}

		public static int GetUniqueNetworkIdentityId()
		{
			GameObject uniqueGameObject = new GameObject("");
			int uniqueId = uniqueGameObject.GetInstanceID();
			MonoBehaviour.Destroy(uniqueGameObject);
			return uniqueId;
		}

		public static IFormatterResolver Formatter { get; private set; }
		public static MessagePackSerializerOptions AddResolver(IFormatterResolver IFormatterResolver)
		{
			IFormatterResolver ??= CompositeResolver.Create(/*GeneratedMessagePackResolver.Instance, */UnityBlitWithPrimitiveArrayResolver.Instance, UnityResolver.Instance, StandardResolver.Instance);
			Formatter = CompositeResolver.Create(IFormatterResolver, Formatter);
			return MessagePackSerializer.DefaultOptions = MessagePackSerializerOptions.Standard.WithResolver(Formatter);
		}
	}
}