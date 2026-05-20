using SystemMonitorAgent.Core.Monitoring.Models.Systems;

namespace SystemMonitorAgent.Core.Monitoring.Models.Options;

public sealed record MonitoringOptions
{
    public ApiMonitoringOptions? ApiMonitoringOptions { get; init; }
    public IReadOnlyCollection<string> MonitoringProcesses { get; init; } = [];
    public int MonitoringIntervalInSeconds { get; init; } = 30;
    public SystemType SystemType { get; init; } = SystemType.Windows;
    public SystemMonitoringMode MonitoringMode { get; init; } = SystemMonitoringMode.ApiReport;
};