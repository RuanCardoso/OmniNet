using System;
using UnityEngine.SceneManagement;
using static Omni.Core.Enums;

namespace Omni.Core
{
    public class ServerEventHandler
    {
        public static event Action<OmniPlayer> OnPlayerConnected;
        public static event Action<OmniPlayer> OnPlayerDisconnected;
        public static event Action<OmniPlayer> OnPlayerPingReceived;

        internal static void FirePlayerConnected(OmniPlayer player) => OnPlayerConnected?.Invoke(player);
        internal static void FirePlayerDisconnected(OmniPlayer player) => OnPlayerDisconnected?.Invoke(player);
        internal static void FirePlayerPingReceived(OmniPlayer player) => OnPlayerPingReceived?.Invoke(player);
    }

    public class ClientEventHandler
    {
        public static event Action OnConnected;
        public static event Action OnDisconnected;
        public static event Action OnPingReceived;

        internal static void FireConnected() => OnConnected?.Invoke();
        internal static void FireDisconnected() => OnDisconnected?.Invoke();
        internal static void FirePingReceived() => OnPingReceived?.Invoke();
    }

    public class GlobalEventHandler
    {
        public static event Action<Scene, LoadSceneMode, PlatformOp> OnSceneLoaded;
        public static event Action<Scene, PlatformOp> OnSceneUnloaded;
        public static event Action<DataIOHandler, ushort, bool> OnMessageReceived;

        internal static void FireSceneLoaded(Scene scene, LoadSceneMode loadSceneMode, PlatformOp platformOp) => OnSceneLoaded?.Invoke(scene, loadSceneMode, platformOp);
        internal static void FireSceneUnloaded(Scene scene, PlatformOp platformOp) => OnSceneUnloaded?.Invoke(scene, platformOp);
        internal static void FireMessageReceived(DataIOHandler IOHandler, ushort fromId, bool fromServer) => OnMessageReceived?.Invoke(IOHandler, fromId, fromServer);
    }
}