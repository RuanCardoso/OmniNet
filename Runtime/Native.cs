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

namespace Omni.Core
{
    internal static class Native
    {
        #region Windows
        // Check if the code is running in the Unity Editor on a Windows platform
        // or in a standalone build on a Windows platform.
#if UNITY_EDITOR_WIN || (!UNITY_EDITOR && UNITY_STANDALONE_WIN)
        // Import the 'setsockopt' function from the 'Ws2_32.dll' library.
        // This function is used for setting socket options in Windows.
        [DllImport("Ws2_32.dll", SetLastError = true)]
        internal static extern int setsockopt(IntPtr s, SocketOptionLevel level, SocketOptionName optname, int optval, int optlen);
#endif
        #endregion

        #region Linux
        // Check if the code is running in the Unity Editor on a Linux platform
        // or in a standalone build on a Linux platform.
#if UNITY_EDITOR_LINUX || (!UNITY_EDITOR && UNITY_STANDALONE_LINUX)
        // Import the 'setsockopt' function from the 'libc.so.6' library.
        // This function is used for setting socket options in Linux.
        [DllImport("libc.so.6", SetLastError = true)]
        internal static extern int setsockopt(IntPtr s, SocketOptionLevel level, SocketOptionName optname, int optval, int optlen);
#endif
        #endregion

        // Note: Mac is not included because the server is only compatible with Windows and Linux.
    }
}