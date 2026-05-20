namespace SystemMonitorAgent.Core.Monitoring.Models.Systems;

internal sealed record SystemDiagnosticReport(
    string Name,
    string Version,
    string BuildNumber,
    string Architecture,
    string HostName,
    string Uptime,
    double CpuLoadPercent,
    SystemRamInfo Ram,
    IReadOnlyCollection<SystemIpAddressInfo> IpAddresses,
    IReadOnlyCollection<SystemProcessInfo> RunningProcesses,
    IReadOnlyDictionary<string, bool> PresenceProcesses,
    IReadOnlyDictionary<string, string> FreeDiskSpaces);