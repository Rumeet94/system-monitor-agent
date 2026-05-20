using System.Collections.Immutable;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using SystemMonitorAgent.Core.Interfaces;
using SystemMonitorAgent.Core.Monitoring.Agents;
using SystemMonitorAgent.Core.Monitoring.Models.Systems;
using Xunit;

namespace SystemMonitorAgent.Core.Tests;

public sealed class ApiMonitoringReportSenderTests
{
    [Fact]
    public async Task Monitor_SendsReportToApi()
    {
        var handler = new CapturingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        };

        var provider = new StubSystemReportProvider(CreateReport("test-host"));
        var sender = new ApiMonitoringReportSender(
            httpClient,
            provider,
            NullLogger<ApiMonitoringReportSender>.Instance);

        await sender.Monitor(ImmutableHashSet<string>.Empty);

        Assert.NotNull(handler.Request);
        Assert.Equal(HttpMethod.Post, handler.Request.Method);
        Assert.Equal("http://localhost:5000/", handler.Request.RequestUri?.ToString());
        Assert.NotNull(handler.Content);
        Assert.Contains("test-host", handler.Content);
    }

    [Fact]
    public async Task Monitor_DoesNotThrow_WhenApiRequestFailsAfterRetries()
    {
        var handler = new CapturingHttpMessageHandler(_ => throw new HttpRequestException("API is unavailable"));
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        };

        var sender = new ApiMonitoringReportSender(
            httpClient,
            new StubSystemReportProvider(CreateReport("test-host")),
            NullLogger<ApiMonitoringReportSender>.Instance);

        await sender.Monitor(ImmutableHashSet<string>.Empty);

        Assert.NotNull(handler.Request);
    }

    [Fact]
    public async Task Monitor_ThrowsPlatformNotSupportedException_WhenProviderDoesNotSupportSystem()
    {
        var sender = new ApiMonitoringReportSender(
            new HttpClient(new CapturingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))),
            new StubSystemReportProvider(CreateReport("test-host"), validateSystem: false),
            NullLogger<ApiMonitoringReportSender>.Instance);

        await Assert.ThrowsAsync<PlatformNotSupportedException>(() => sender.Monitor(ImmutableHashSet<string>.Empty));
    }

    private static SystemDiagnosticReport CreateReport(string hostName)
    {
        return new SystemDiagnosticReport(
            Name: "Windows",
            Version: "10.0",
            BuildNumber: "1",
            Architecture: "x64",
            HostName: hostName,
            Uptime: "00.00:00:01",
            CpuLoadPercent: 1,
            Ram: new SystemRamInfo(16, 4, 12, 25),
            IpAddresses: [],
            RunningProcesses: [],
            PresenceProcesses: new Dictionary<string, bool>(),
            FreeDiskSpaces: new Dictionary<string, string>());
    }

    private sealed class StubSystemReportProvider : ISystemReportProvider
    {
        private readonly SystemDiagnosticReport _report;
        private readonly bool _validateSystem;

        public StubSystemReportProvider(SystemDiagnosticReport report, bool validateSystem = true)
        {
            _report = report;
            _validateSystem = validateSystem;
        }

        public bool ValidateSystem()
        {
            return _validateSystem;
        }

        public Task<SystemDiagnosticReport> GetReport(
            ImmutableHashSet<string> monitoringProcessNames,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_report);
        }
    }

    private sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public CapturingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public HttpRequestMessage? Request { get; private set; }

        public string? Content { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;
            Content = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return _responseFactory(request);
        }
    }
}
