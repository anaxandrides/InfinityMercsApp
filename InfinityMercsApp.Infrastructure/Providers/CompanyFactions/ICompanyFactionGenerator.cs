using InfinityMercsApp.Domain.Models.Army;

namespace InfinityMercsApp.Infrastructure.Providers.CompanyFactions;

/// <summary>
/// Builds one of the factions the Airborne, Inspiring and TAG companies draw on, from the armies stored. Building and
/// storing are separate, so the API can build the faction once and the web site store what the API built.
/// </summary>
public interface ICompanyFactionGenerator
{
    /// <summary>The generated faction's id (2001 and up).</summary>
    int FactionId { get; }

    /// <summary>The faction from the armies stored; null when they offer nothing for it.</summary>
    ArmyDefinition? Build(CancellationToken cancellationToken = default);

    /// <summary>Stores <paramref name="army"/> as the faction and lists it with the metadata's factions.</summary>
    void Store(ArmyDefinition army, CancellationToken cancellationToken = default);

    /// <summary>Builds the faction and stores it, unless the faction stored is already that version.</summary>
    void Generate(CancellationToken cancellationToken = default);
}
