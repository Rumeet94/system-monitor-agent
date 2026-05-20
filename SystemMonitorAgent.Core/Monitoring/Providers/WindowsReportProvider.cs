using System.Collections.Immutable;
using System.Diagnostics;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using SystemMonitorAgent.Core.Interfaces;
using SystemMonitorAgent.Core.Monitoring.Extensions;
using SystemMonitorAgent.Core.Monitoring.Helpers;
using SystemMonitorAgent.Core.Monitoring.Models.Systems;

namespace SystemMonitorAgent.Core.Monitoring.Providers;

internal sealed class WindowsReportProvider : ISystemReportProvider
{
    public bool ValidateSystem()
    {
        return OperatingSystem.IsWindows();
    }

    public async Task<SystemDiagnosticReport> GetReport(
        ImmutableHashSet<string> monitoringProcessNames,
        CancellationToken cancellationToken = default)
    {
        var system = GetWindowsInfo();
        var processes = GetProcesses();
        
        var runningProcessNames = processes
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        
        var processPresence = monitoringProcessNames.ToDictionary(
            name => name,
            name => runningProcessNames.Contains(Path.GetFileNameWithoutExtension(name)),
            StringComparer.OrdinalIgnoreCase);

        var freeDiskSpaces = DriveInfo
            .GetDrives()
            .Where(d => d.IsReady)
            .ToDictionary(
                k => k.Name,
                v => $"{MemoryMetricConverter.RoundGbFromBytes(v.TotalFreeSpace)} GB ({Math.Round(v.TotalFreeSpace * 100.0 / v.TotalSize, 2)} %)");

        var cpuLoadPercent = await GetCpuLoadPercent(cancellationToken);

        return new SystemDiagnosticReport(
            system.Name,
            system.Version,
            system.BuildNumber,
            system.Architecture,
            Environment.MachineName,
            system.Uptime,
            cpuLoadPercent,
            system.Ram,
            GetIpAddresses(),
            processes,
            processPresence,
            freeDiskSpaces);
    }

    private static IReadOnlyCollection<SystemIpAddressInfo> GetIpAddresses()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic =>
                nic.OperationalStatus == OperationalStatus.Up &&
                nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(nic =>
                nic.GetIPProperties().UnicastAddresses
                    .Where(ip =>
                        ip.Address.AddressFamily == AddressFamily.InterNetwork ||
                        ip.Address.AddressFamily == AddressFamily.InterNetworkV6)
                    .Where(ip =>
                        !IPAddress.IsLoopback(ip.Address) &&
                        !ip.Address.IsIPv6LinkLocal)
                    .Select(ip => new SystemIpAddressInfo(
                        nic.Name,
                        ip.Address.AddressFamily == AddressFamily.InterNetwork
                            ? "IPv4"
                            : "IPv6",
                        ip.Address.ToString())
                    ))
            .ToArray();
    }

    private static (string Name, string Version, string BuildNumber, string Architecture, string Uptime, SystemRamInfo Ram) GetWindowsInfo()
    {
        const string script = "SELECT Caption, Version, BuildNumber, OSArchitecture, LastBootUpTime, " +
                              "TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem";

        using var searcher = new ManagementObjectSearcher(script);
        using var result = searcher.Get();

        var system = result.Cast<ManagementObject>().FirstOrDefault()
            ?? throw new InvalidOperationException("Win32_OperatingSystem information not found");

        var lastBoot = ManagementDateTimeConverter.ToDateTime(
            system["LastBootUpTime"]?.ToString() ?? throw new InvalidOperationException("LastBootUpTime not found"));

        var totalRamKb = Convert.ToUInt64(system["TotalVisibleMemorySize"]);
        var freeRamKb = Convert.ToUInt64(system["FreePhysicalMemory"]);
        var usedRamKb = totalRamKb - freeRamKb;

        var ram = new SystemRamInfo(
            MemoryMetricConverter.RoundGbFromKb(totalRamKb),
            MemoryMetricConverter.RoundGbFromKb(usedRamKb),
            MemoryMetricConverter.RoundGbFromKb(freeRamKb),
            Math.Round(usedRamKb * 100.0 / totalRamKb, 2));

        var uptime = (DateTime.Now - lastBoot).ToString(@"dd\.hh\:mm\:ss");
        
        return
        (
            system["Caption"]?.ToString() ?? string.Empty,
            system["Version"]?.ToString() ?? string.Empty,
            system["BuildNumber"]?.ToString() ?? string.Empty,
            system["OSArchitecture"]?.ToString() ?? string.Empty,
            uptime,
            ram
        );
    }

    private static async Task<double> GetCpuLoadPercent(CancellationToken cancellationToken)
    {
        using var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        
        _ = cpuCounter.NextValue();
        
        await Task.Delay(1000, cancellationToken);

        return Math.Round(cpuCounter.NextValue(), 2);
    }

    private static IReadOnlyCollection<SystemProcessInfo> GetProcesses()
    {
        var list = new List<SystemProcessInfo>();

        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    var name = process.GetSafeName();

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    list.Add(new SystemProcessInfo(
                        process.Id,
                        name,
                        MemoryMetricConverter.RoundGbFromBytes(process.WorkingSet64)));
                }
                catch
                {
                    // process can be stopped right now, so ignore
                }
            }
        }

        return list;
    }
}