namespace SystemMonitorAgent.Core.Monitoring.Models.Options;

public sealed record ApiMonitoringOptions(
    string Endpoint,
    int TimeoutInSeconds = 10,
    int RetryCount = 3,
    int RetryIntervalInSeconds = 3);
