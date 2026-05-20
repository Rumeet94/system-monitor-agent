namespace SystemMonitorAgent.Core.Monitoring.Models.Systems;

internal sealed record SystemRamInfo(double TotalGb, double UsedGb, double FreeGb, double UsedPercent);