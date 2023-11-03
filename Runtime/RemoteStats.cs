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

namespace Omni.Core
{
    public struct RemoteStats
    {
        public double Time;
        public int Length;

        public RemoteStats(double time, int length)
        {
            Time = time;
            Length = length;
        }
    }
}