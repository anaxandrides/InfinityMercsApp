using InfinityMercsApp.Domain.Models.Army;

namespace InfinityMercsApp.Infrastructure.Providers;

/// <summary>
/// Handles importing Army data and parsing it into structured objects.
/// </summary>
public interface IArmyImportProvider
{
    /// <summary>
    /// Stores an army (as read from the Army API, or generated) under <paramref name="factionId"/>, replacing
    /// whatever was stored for it before.
    /// </summary>
    Task ImportAsync(int factionId, ArmyDefinition army, CancellationToken cancellationToken = default);
}
