using Microsoft.Extensions.DependencyInjection;
using SystemMonitorAgent.Core.Interfaces;
using SystemMonitorAgent.Core.Monitoring.Agents;
using SystemMonitorAgent.Core.Monitoring.Models.Options;
using SystemMonitorAgent.Core.Monitoring.Models.Systems;
using SystemMonitorAgent.Core.Monitoring.Providers;

namespace SystemMonitorAgent.Core.Monitoring.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSystemMonitorAgent(this IServiceCollection services, MonitoringOptions options)
    {
        return services
            .AddSystemReportProvider(options.SystemType)
            .AddMonitoringAgent(options);
    }
    
    private static IServiceCollection AddMonitoringAgent(this IServiceCollection services, MonitoringOptions options)
    {
        if (options.MonitoringMode == SystemMonitoringMode.ApiReport)
        {
            if (string.IsNullOrWhiteSpace(options.ApiMonitoringOptions?.Endpoint))
            {
                throw new InvalidOperationException("Endpoint is required for ApiReportMonitoring mode.");
            }

            services
                .AddHttpClient<IMonitoringAgent, ApiMonitoringReportSender>((_, httpClient) =>
                {
                    httpClient.BaseAddress = new(options.ApiMonitoringOptions!.Endpoint);
                    httpClient.Timeout = TimeSpan.FromSeconds(options.ApiMonitoringOptions.TimeoutInSeconds);
                })
                .AddRetryPolicy(options.ApiMonitoringOptions);
        }

        return services;
    }
    
    private static IServiceCollection AddSystemReportProvider(this IServiceCollection services, SystemType systemType)
    {
        return systemType switch
        {
            SystemType.Windows => services.AddScoped<ISystemReportProvider, WindowsReportProvider>(),
            _ => throw new ArgumentOutOfRangeException(nameof(systemType), systemType, "Failed configure for system type")
        };
    }
}
