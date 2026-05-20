using SystemMonitorAgent.Core.Monitoring.Extensions;
using SystemMonitorAgent.Core.Monitoring.Models.Options;
using SystemMonitorAgent.Host;
using SystemMonitorAgent.Host.Logging;

var builder = WebApplication.CreateBuilder(args);

const string serviceName = "System Monitor Agent";

builder.WebHost.UseUrls(builder.Configuration["HealthCheck:Urls"] ?? "http://localhost:5055");

var logFilePath = ResolveLogFilePath(builder.Configuration["FileLogging:Path"]);

Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);

builder.Services.AddWindowsService(options => { options.ServiceName = serviceName; });

builder.Logging.AddConsole();
builder.Logging.AddEventLog(options => { options.SourceName = serviceName; });
builder.Logging.AddProvider(new FileLoggerProvider(logFilePath));

using var startupLoggerFactory = LoggerFactory.Create(logging =>
{
    logging.AddConsole();
    logging.AddProvider(new FileLoggerProvider(logFilePath));
});

var startupLogger = startupLoggerFactory.CreateLogger("Startup");

try
{
    var monitoringOptions = builder.Configuration
        .GetSection(nameof(MonitoringOptions))
        .Get<MonitoringOptions>() ?? new MonitoringOptions();

    builder.Services.AddSingleton(monitoringOptions);
    builder.Services.AddSystemMonitorAgent(monitoringOptions);
    builder.Services.AddHostedService<MonitoringWorker>();
}
catch (Exception exception)
{
    startupLogger.LogCritical(
        exception,
        "Monitoring configuration failed. Application will be stopped.");

    return;
}

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Timestamp = DateTimeOffset.UtcNow
}));

await app.RunAsync();

static string ResolveLogFilePath(string? configuredPath)
{
    if (string.IsNullOrWhiteSpace(configuredPath))
    {
        return Path.Combine(
            AppContext.BaseDirectory,
            "logs",
            "system-monitor-agent.log");
    }

    var expandedPath = Environment.ExpandEnvironmentVariables(configuredPath);

    return Path.IsPathRooted(expandedPath)
        ? expandedPath
        : Path.Combine(AppContext.BaseDirectory, expandedPath);
}
