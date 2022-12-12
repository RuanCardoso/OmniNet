namespace Neutron.Core
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