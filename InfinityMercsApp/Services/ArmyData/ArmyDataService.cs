using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Infrastructure.Providers.CompanyFactions;
using InfinityMercsApp.Infrastructure.Providers.Metadata;
using ArmyFactionRecord = InfinityMercsApp.Domain.Models.Army.Faction;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;
using ArmyUnitRecord = InfinityMercsApp.Domain.Models.Army.Unit;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;
using MercsArmyListEntry = InfinityMercsApp.Domain.Models.Army.MercsArmyListEntry;

namespace InfinityMercsApp.Services.ArmyData;

internal sealed class ArmyDataService(
    IMetadataProvider? metadataProvider,
    IFactionProvider? factionProvider,
    ICohesiveCompanyFactionQueryProvider cohesiveCompanyFactionQueryProvider,
    IAppSettingsProvider? appSettingsProvider) : IArmyDataService
{
    public IReadOnlyList<FactionRecord> GetMetadataFactions(bool includeDiscontinued = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (metadataProvider is null)
        {
            return [];
        }

        return metadataProvider
            .GetFactions(includeDiscontinued)
            .Select(CloneMetadataFaction)
            .ToList();
    }

    public FactionRecord? GetMetadataFactionById(int id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (metadataProvider is null || id <= 0)
        {
            return null;
        }

        var faction = metadataProvider.GetFactionById(id);
        return faction is null ? null : CloneMetadataFaction(faction);
    }

    public ArmyFactionRecord? GetFactionSnapshot(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (factionProvider is null || factionId <= 0)
        {
            return null;
        }

        return CloneArmyFaction(factionProvider.GetFactionSnapshot(factionId));
    }

    public IReadOnlyList<ArmyResumeRecord> GetResumeByFaction(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (factionProvider is null || factionId <= 0)
        {
            return [];
        }

        return factionProvider
            .GetResumeByFaction(factionId)
            .Select(CloneArmyResume)
            .ToList();
    }

    public IReadOnlyList<ArmyResumeRecord> GetResumeByFactionMercsOnly(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (factionProvider is null || factionId <= 0)
        {
            return [];
        }

        return factionProvider
            .GetResumeByFactionMercsOnly(factionId)
            .Select(CloneArmyResume)
            .ToList();
    }

    public ArmyUnitRecord? GetUnit(int factionId, int unitId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (factionProvider is null || factionId <= 0 || unitId <= 0)
        {
            return null;
        }

        return CloneArmyUnit(factionProvider.GetUnit(factionId, unitId));
    }

    public IReadOnlyList<MercsArmyListEntry> GetMergedMercsArmyList(
        IReadOnlyCollection<int> factionIds,
        CancellationToken cancellationToken = default)
    {
        if (factionIds.Count == 0)
        {
            return [];
        }

        var queryResult = cohesiveCompanyFactionQueryProvider.GetFilterQuerySource(factionIds, cancellationToken);
        return queryResult.MergedMercsListEntries;
    }

    public string FormatMovement(Movement? movement) =>
        Movement.Format(movement, appSettingsProvider?.GetShowUnitsInInches() ?? false);

    private static FactionRecord CloneMetadataFaction(FactionRecord faction)
    {
        return new FactionRecord
        {
            Id = faction.Id,
            ParentId = faction.ParentId,
            Name = faction.Name,
            Slug = faction.Slug,
            Discontinued = faction.Discontinued,
            Logo = faction.Logo
        };
    }

    private static ArmyFactionRecord? CloneArmyFaction(ArmyFactionRecord? faction)
    {
        if (faction is null)
        {
            return null;
        }

        return new ArmyFactionRecord
        {
            FactionId = faction.FactionId,
            Version = faction.Version,
            ImportedAtUnixSeconds = faction.ImportedAtUnixSeconds,
            ReinforcementsFactionId = faction.ReinforcementsFactionId,
            Filters = faction.Filters,
            Relations = faction.Relations,
            FireteamChart = faction.FireteamChart
        };
    }

    private static ArmyResumeRecord CloneArmyResume(ArmyResumeRecord resume)
    {
        return new ArmyResumeRecord
        {
            ResumeKey = resume.ResumeKey,
            FactionId = resume.FactionId,
            UnitId = resume.UnitId,
            IdArmy = resume.IdArmy,
            Isc = resume.Isc,
            Name = resume.Name,
            Slug = resume.Slug,
            Logo = resume.Logo,
            Type = resume.Type,
            Category = resume.Category
        };
    }

    private static ArmyUnitRecord? CloneArmyUnit(ArmyUnitRecord? unit)
    {
        if (unit is null)
        {
            return null;
        }

        return new ArmyUnitRecord
        {
            UnitKey = unit.UnitKey,
            FactionId = unit.FactionId,
            UnitId = unit.UnitId,
            IdArmy = unit.IdArmy,
            Canonical = unit.Canonical,
            Isc = unit.Isc,
            IscAbbr = unit.IscAbbr,
            Name = unit.Name,
            Slug = unit.Slug,
            ProfileGroups = unit.ProfileGroups,
            Options = unit.Options,
            Filters = unit.Filters,
            FactionIds = unit.FactionIds
        };
    }

}
