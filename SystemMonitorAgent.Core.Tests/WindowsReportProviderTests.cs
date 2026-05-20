using System.Collections.Immutable;
using SystemMonitorAgent.Core.Monitoring.Providers;
using Xunit;

namespace SystemMonitorAgent.Core.Tests;

public sealed class WindowsReportProviderTests
{
    [Fact]
    public void ValidateSystem_ReturnsOperatingSystemWindowsFlag()
    {
        var provider = new WindowsReportProvider();

        var result = provider.ValidateSystem();

        Assert.Equal(OperatingSystem.IsWindows(), result);
    }

    [Fact]
    public async Task GetReport_ReturnsReportWithRequiredFields_OnWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var provider = new WindowsReportProvider();
        var processNames = ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, "not-existing-test-process");

        var report = await provider.GetReport(processNames);

        Assert.False(string.IsNullOrWhiteSpace(report.HostName));
        Assert.False(string.IsNullOrWhiteSpace(report.Name));
        Assert.False(string.IsNullOrWhiteSpace(report.Version));
        Assert.True(report.Ram.TotalGb > 0);
        Assert.InRange(report.CpuLoadPercent, 0, 100);
        Assert.Contains("not-existing-test-process", report.PresenceProcesses.Keys);
        Assert.NotNull(report.IpAddresses);
        Assert.NotNull(report.RunningProcesses);
        Assert.NotNull(report.FreeDiskSpaces);
    }
}
