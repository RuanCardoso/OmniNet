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

using Omni.Internal.Transport;
using UnityEngine;

namespace Omni.Core
{
    public abstract class CustomTransportSettings : MonoBehaviour
    {
        public abstract void OnTransportSettings(TransportSettings transportSettings, RuntimePlatform runtimePlatform);
    }
}