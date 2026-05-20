using System.Collections.Immutable;
using SystemMonitorAgent.Core.Interfaces;
using SystemMonitorAgent.Core.Monitoring.Models.Options;

namespace SystemMonitorAgent.Host;

public sealed class MonitoringWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<MonitoringWorker> _logger;
    private readonly MonitoringOptions _monitoringOptions;

    public MonitoringWorker(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<MonitoringWorker> logger,
        MonitoringOptions monitoringOptions)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _monitoringOptions = monitoringOptions;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var processNames = ImmutableHashSet.Create(
            StringComparer.OrdinalIgnoreCase,
            _monitoringOptions.MonitoringProcesses.ToArray());

        _logger.LogInformation(
            "Monitoring worker started. IntervalSeconds: {IntervalSeconds}, MonitoringProcesses: {MonitoringProcesses}.",
            _monitoringOptions.MonitoringIntervalInSeconds,
            string.Join(", ", processNames));

        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_monitoringOptions.MonitoringIntervalInSeconds));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    
                    var monitoringAgent = scope.ServiceProvider.GetRequiredService<IMonitoringAgent>();

                    await monitoringAgent.Monitor(processNames, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Monitoring cycle failed.");
                }

                try
                {
                    await timer.WaitForNextTickAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
        finally
        {
            _logger.LogInformation("Monitoring worker stopped.");
        }
    }
}
