using System.Collections.Immutable;

namespace SystemMonitorAgent.Core.Interfaces;

public interface IMonitoringAgent
{
    Task Monitor(
        ImmutableHashSet<string> monitoringProcessNames,
        CancellationToken cancellationToken = default);
}