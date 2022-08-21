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
using System.Linq;
using System.Net;

namespace Neutron.Core
{
    internal enum MessageType : byte
    {
        None = 0,
        Test = 1,
        Acknowledgement = 2,
        Zone = 3,
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

    internal enum NeutronObjectType : byte
    {
        DynamicallyInstantiated = 0,
        Static = 1,
        Player = 2,
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
    }

    internal static class Extensions
    {

    }
}