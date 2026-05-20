namespace SystemMonitorAgent.Core.Monitoring.Helpers;

internal static class MemoryMetricConverter
{
    private const double RoundValue = 1024.0;
    
    public static double RoundGbFromBytes(long bytes)
    {
        return Math.Round(bytes / RoundValue / RoundValue / RoundValue, 2);
    }

    public static double RoundGbFromKb(ulong kb)
    {
        return Math.Round(kb / RoundValue / RoundValue, 2);
    }
}