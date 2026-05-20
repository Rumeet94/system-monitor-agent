using System.Collections.Immutable;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using SystemMonitorAgent.Core.Interfaces;

namespace SystemMonitorAgent.Core.Monitoring.Agents;

internal sealed class ApiMonitoringReportSender : IMonitoringAgent
{
    private readonly HttpClient _httpClient;
    private readonly ISystemReportProvider _provider;
    private readonly ILogger<ApiMonitoringReportSender> _logger;

    public ApiMonitoringReportSender(
        HttpClient httpClient,
        ISystemReportProvider provider,
        ILogger<ApiMonitoringReportSender> logger)
    {
        _httpClient = httpClient;
        _provider = provider;
        _logger = logger;
    }

    public async Task Monitor(
        ImmutableHashSet<string> monitoringProcessNames,
        CancellationToken cancellationToken = default)
    {
        if (!_provider.ValidateSystem())
        {
            throw new PlatformNotSupportedException("System diagnostic report is available only on Windows.");
        }

        var report = await _provider.GetReport(monitoringProcessNames, cancellationToken);

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                string.Empty,
                report,
                cancellationToken: cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "System diagnostic report was sent to API. StatusCode: {StatusCode}.",
                    response.StatusCode);

                return;
            }

            _logger.LogWarning(
                "System diagnostic report was not accepted by API after retries. StatusCode: {StatusCode}.",
                response.StatusCode);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "System diagnostic report sending failed after retries.");
        }
    }
}
