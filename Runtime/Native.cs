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
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Neutron.Core
{
    internal static class Native
    {
        #region Windows
#if UNITY_EDITOR_WIN || (!UNITY_EDITOR && UNITY_STANDALONE_WIN)
        [DllImport("Ws2_32.dll", SetLastError = true)]
        internal static extern int setsockopt(IntPtr s, SocketOptionLevel level, SocketOptionName optname, int optval, int optlen);
#endif
        #endregion

        #region Linux
#if UNITY_EDITOR_LINUX || (!UNITY_EDITOR && UNITY_STANDALONE_LINUX)
        [DllImport("libc.so.6", SetLastError = true)]
        internal static extern int setsockopt(IntPtr s, SocketOptionLevel level, SocketOptionName optname, int optval, int optlen);
#endif
        #endregion
    }
}