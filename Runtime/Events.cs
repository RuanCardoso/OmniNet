using System;
using UnityEngine.SceneManagement;

namespace Omni.Core
{
    public class ServerEventHandler : GlobalEventHandler
    {
        public static event Action<OmniPlayer> OnPlayerConnected;
        public static event Action<OmniPlayer> OnPlayerDisconnected;
        public static event Action<OmniPlayer> OnPlayerPingReceived;

        internal static void FirePlayerConnected(OmniPlayer player) => OnPlayerConnected?.Invoke(player);
        internal static void FirePlayerDisconnected(OmniPlayer player) => OnPlayerDisconnected?.Invoke(player);
        internal static void FirePlayerPingReceived(OmniPlayer player) => OnPlayerPingReceived?.Invoke(player);
    }

    public class ClientEventHandler : GlobalEventHandler
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
        public static event Action<Scene, LoadSceneMode> OnSceneLoaded;
        public static event Action<Scene> OnSceneUnloaded;

        internal static void FireSceneLoaded(Scene scene, LoadSceneMode loadSceneMode) => OnSceneLoaded?.Invoke(scene, loadSceneMode);
        internal static void FireSceneUnloaded(Scene scene) => OnSceneUnloaded?.Invoke(scene);
    }
}