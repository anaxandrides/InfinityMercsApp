using InfinityMercsApp.Infrastructure.Providers;
using System.Globalization;
using ArmyFactionRecord = InfinityMercsApp.Domain.Models.Army.Faction;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;
using ArmyUnitRecord = InfinityMercsApp.Domain.Models.Army.Unit;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;
using MercsArmyListEntry = InfinityMercsApp.Domain.Models.Army.MercsArmyListEntry;

namespace InfinityMercsApp.Services;

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

    public async Task<IReadOnlyList<MercsArmyListEntry>> GetMergedMercsArmyListAsync(
        IReadOnlyCollection<int> factionIds,
        CancellationToken cancellationToken = default)
    {
        if (factionIds.Count == 0)
        {
            return [];
        }

        var queryResult = await cohesiveCompanyFactionQueryProvider.GetFilterQuerySourceAsync(factionIds, cancellationToken);
        return queryResult.MergedMercsListEntries;
    }

    public (int? FirstCm, int? SecondCm, string DisplayValue) ReadMoveValue(InfinityMercsApp.Domain.Models.Army.Profiles.UnitProfile profile)
    {
        var (firstCm, secondCm) = profile.MoveFirstSecondCm;
        return (firstCm, secondCm, FormatMoveValue(firstCm, secondCm));
    }

    public string FormatMoveValue(int? firstCm, int? secondCm)
    {
        if (firstCm is null || secondCm is null || firstCm <= 0 || secondCm <= 0)
        {
            return "-";
        }

        var showUnitsInInches = appSettingsProvider?.GetShowUnitsInInches() ?? false;
        if (showUnitsInInches)
        {
            var firstInches = firstCm.Value / 2.54d;
            var secondInches = secondCm.Value / 2.54d;
            return $"{Math.Round(firstInches):0}-{Math.Round(secondInches):0}";
        }

        return $"{firstCm.Value.ToString(CultureInfo.InvariantCulture)}-{secondCm.Value.ToString(CultureInfo.InvariantCulture)}";
    }

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
            ReinforcementsJson = faction.ReinforcementsJson,
            FiltersJson = faction.FiltersJson,
            FireteamsJson = faction.FireteamsJson,
            RelationsJson = faction.RelationsJson,
            SpecopsJson = faction.SpecopsJson,
            FireteamChartJson = faction.FireteamChartJson,
            RawJson = faction.RawJson
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
            ProfileGroupsJson = unit.ProfileGroupsJson,
            OptionsJson = unit.OptionsJson,
            FiltersJson = unit.FiltersJson,
            FactionsJson = unit.FactionsJson
        };
    }

}
