public struct RemoteStats
{
    public double LocalTime;
    public double NetworkTime;
    public double BatchedTime;

    public RemoteStats(double localTime, double networkTime, double batchedTime)
    {
        LocalTime = localTime;
        NetworkTime = networkTime;
        BatchedTime = batchedTime;
    }
}