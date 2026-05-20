using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using SystemMonitorAgent.Core.Monitoring.Agents;
using SystemMonitorAgent.Core.Monitoring.Models.Options;

namespace SystemMonitorAgent.Core.Monitoring.Extensions;

public static class HttpClientBuilderExtensions
{
    public static IHttpClientBuilder AddRetryPolicy(this IHttpClientBuilder builder, ApiMonitoringOptions options) 
    {
        return builder.AddPolicyHandler((serviceProvider, _) =>
        {
            var logger = serviceProvider
                .GetRequiredService<ILogger<ApiMonitoringReportSender>>();

            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(response => response.StatusCode == HttpStatusCode.TooManyRequests)
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(
                    retryCount: options.RetryCount,
                    sleepDurationProvider: _ => TimeSpan.FromSeconds(options.RetryIntervalInSeconds),
                    onRetry: (outcome, delay, retryAttempt, _) =>
                    {
                        if (outcome.Exception is not null)
                        {
                            logger.LogInformation(
                                outcome.Exception,
                                "Monitoring API request failed. RetryAttempt: {RetryAttempt}, Delay: {Delay}.",
                                retryAttempt,
                                delay);
                        }
                        else
                        {
                            logger.LogInformation(
                                "Monitoring API returned unsuccessful response. StatusCode: {StatusCode}. RetryAttempt: {RetryAttempt}, Delay: {Delay}.",
                                outcome.Result.StatusCode,
                                retryAttempt,
                                delay);
                        }
                    });
        });
    }
}