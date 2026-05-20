using System.Collections.Immutable;
using SystemMonitorAgent.Core.Monitoring.Models;
using SystemMonitorAgent.Core.Monitoring.Models.Systems;

namespace SystemMonitorAgent.Core.Interfaces;

internal interface ISystemReportProvider
{
    bool ValidateSystem();
    
    Task<SystemDiagnosticReport> GetReport(
        ImmutableHashSet<string> monitoringProcessNames,
        CancellationToken cancellationToken = default);
}