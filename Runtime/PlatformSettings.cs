using Omni;
using System;
using UnityEngine;

namespace Omni.Core
{
    [DefaultExecutionOrder(-200)]
    public class PlatformSettings : MonoBehaviour
    {
        internal static PlatformSettings _self;
        internal static DedicatedServerSettings ServerSettings => _self.GetDedicatedServerSettings();
        internal static Settings ClientSettings => _self.GetPlatformSettings();

        private void Awake()
        {
            _self = this;
        }

        private void Start() { }

        [Serializable]
        public class DedicatedServerSettings
        {
            public ushort port = 5055;
            [Range(32, ushort.MaxValue)] public ushort bSPoolSize = 128; // Byte Stream Pool Size, quantidade de items no Pool.
            [Range(255, ushort.MaxValue)] public ushort windowSize = 255; // Define o tamanho da janela de recepção do protocolo.
            [Range(1, 1500)] public ushort maxPacketSize = 128;
            public double ackTimeout = 0.3d; // seconds, Provavelmente quando um Ack deve ser dado como não reconhecido. SOCK_SEND será executado!
            public int ackSweep = 15; // ms, Varredura por reconhecimentos(Ack's)...
            public bool dontDestroy = false;
            public bool loadNextScene = false;
            public bool hasCommands = false;
        }

        [Serializable]
        public class Settings
        {
            [Serializable]
            public class Hostname
            {
                [SerializeField] public string name;
                [SerializeField] public string host;
            }

            public Hostname[] hosts = {
               new Hostname() { host = "127.0.0.1", name = "localhost" },
               new Hostname() { host = "0.0.0.0", name = "WSL" },
               new Hostname() { host = "0.0.0.0", name = "Cloud Server" },
            };

            [Range(30, 32640)] public int maxFramerate = 60;
            [Range(1, 2040)] public int recvMultiplier = 1; // Quantas operações de recebimento/leitura de dados por quadro...
            [Range(128, ushort.MaxValue)] public int recvBufferSize = 8192;
            [Range(128, ushort.MaxValue)] public int sendBufferSize = 8192;
        }

        [SerializeField] private DedicatedServerSettings dedicatedServer;
        [SerializeField][HorizontalLine(height: 1)] private Settings editor;
        [SerializeField][HorizontalLine(height: 1)] private Settings windows;
        [SerializeField][HorizontalLine(height: 1)] private Settings linux;
        [SerializeField][HorizontalLine(height: 1)] private Settings osx;
        [SerializeField][HorizontalLine(height: 1)] private Settings ios;
        [SerializeField][HorizontalLine(height: 1)] private Settings android;
        [SerializeField][HorizontalLine(height: 1)] private Settings others;

        private Settings GetPlatformSettings()
        {
#if UNITY_EDITOR
            return editor;
#elif UNITY_STANDALONE_WIN
            return windows;
#elif UNITY_STANDALONE_LINUX
            return linux;
#elif UNITY_STANDALONE_OSX
            return osx;
#elif UNITY_IOS
            return ios;
#elif UNITY_ANDROID
            return android;
#else
            return others;
#endif
        }

        private DedicatedServerSettings GetDedicatedServerSettings() => dedicatedServer;
    }
}