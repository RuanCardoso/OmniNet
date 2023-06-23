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

using NaughtyAttributes;
using System;
using UnityEngine;

namespace Neutron.Core
{
    [Serializable]
    internal class LocalSettings
    {
        [Serializable]
        public class Host
        {
            [SerializeField] internal string name;
            [SerializeField] internal string Ip;
        }

        [InfoBox("Please ensure that the active platform in 'Build Settings' matches the platform displayed by Atom. If they don't match, click on 'Request Script Compilation'.", EInfoBoxType.Warning)]
        [ReadOnly][AllowNesting][Label("")][Space(3)] public string name = "No Plataform!";

        public Host[] hosts = {
            new Host() { Ip = "127.0.0.1", name = "localhost" },
            new Host() { Ip = "0.0.0.0", name = "WSL" },
            new Host() { Ip = "0.0.0.0", name = "Cloud Server" },
        };

        [HideInInspector] public bool enabled;
        [Header("Others")]
        [Range(30, byte.MaxValue * 128)] public int maxFramerate = 60;
        [Range(1, byte.MaxValue * 8)] public int recvMultiplier = 1;
        [Header("Timers")]
        [Range(0, 5f)] public double ackTimeout = 0.3f; // seconds
        [Range(1, 1000)] public int ackSweep = 15; // ms
        [Header("Socket")]
        [Range(128, ushort.MaxValue)] public int recvBufferSize = 8192;
        [Range(128, ushort.MaxValue)] public int sendBufferSize = 8192;
        [Min(0)][HideInInspector] public int recvTimeout = 0;
        [Min(0)][HideInInspector] public int sendTimeout = 0;
    }
}