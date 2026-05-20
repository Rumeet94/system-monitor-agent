using System.Diagnostics;

namespace SystemMonitorAgent.Core.Monitoring.Extensions;

public static class ProcessExtensions
{
    public static string? GetSafeName(this Process process)
    {
        try
        {
            return process.ProcessName;
        }
        catch
        {
            return null;
        }
    }
}