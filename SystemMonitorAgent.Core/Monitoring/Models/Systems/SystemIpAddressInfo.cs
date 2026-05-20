namespace SystemMonitorAgent.Core.Monitoring.Models.Systems;

internal sealed record SystemIpAddressInfo(string InterfaceName, string AddressFamily, string Address);