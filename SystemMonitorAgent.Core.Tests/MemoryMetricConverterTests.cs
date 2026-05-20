using SystemMonitorAgent.Core.Monitoring.Helpers;
using Xunit;

namespace SystemMonitorAgent.Core.Tests;

public sealed class MemoryMetricConverterTests
{
    [Fact]
    public void RoundGbFromBytes_ConvertsBytesToGigabytes()
    {
        var result = MemoryMetricConverter.RoundGbFromBytes(1_073_741_824);

        Assert.Equal(1, result);
    }

    [Fact]
    public void RoundGbFromKb_ConvertsKilobytesToGigabytes()
    {
        var result = MemoryMetricConverter.RoundGbFromKb(1_048_576);

        Assert.Equal(1, result);
    }
}
