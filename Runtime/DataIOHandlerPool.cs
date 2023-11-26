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

using System.Collections.Generic;
using static Omni.Core.PlatformSettings;

#pragma warning disable

namespace Omni.Core
{
    /// <summary>
    /// Pool of reusable DataIOHandlers for efficient memory usage and CPU performance.
    /// </summary>
    internal class DataIOHandlerPool
    {
        private readonly Stack<DataIOHandler> pool = new();

        public DataIOHandlerPool(int length = 128)
        {
            for (int i = 0; i < length; i++)
            {
                pool.Push(new DataIOHandler(ServerSettings.maxPacketSize, true));
            }
        }

        public DataIOHandler Get()
        {
            if (pool.Count == 0)
            {
                OmniLogger.Print("Pool: No DataIOHandler's are currently available. A temporary DataIOHandler will be created to handle this data.");
                return new DataIOHandler(ServerSettings.maxPacketSize, true);
            }
            else
            {
                return pool.Pop();
            }
        }

        public void Release(DataIOHandler IOHandler)
        {
            IOHandler.Write();
            pool.Push(IOHandler);
        }

        public int Count => pool.Count;
    }
}