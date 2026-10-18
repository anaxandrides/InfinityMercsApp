using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services.ArmyData;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;

namespace InfinityMercsApp.Services.CompanySelection.Units;

/// <summary>The units a company selection screen lists for its source factions.</summary>
public sealed class SelectionUnitCatalog(IArmyDataService armyData, ISpecOpsProvider specOps)
{
    /// <summary>A faction's listed units (<paramref name="resumes"/>, which differ by company) with its Spec-Ops units and names.</summary>
    public FactionUnits Read(int factionId, IReadOnlyList<ArmyResumeRecord> resumes, CancellationToken cancellationToken = default)
    {
        var snapshot = armyData.GetFactionSnapshot(factionId, cancellationToken);
        return new FactionUnits(
            factionId,
            resumes,
            specOps.GetSpecopsUnitsByFaction(factionId, cancellationToken),
            CompanyUnitDetailsShared.BuildIdNameLookup(snapshot?.Filters, ArmyFilterSection.Type),
            CompanyUnitDetailsShared.BuildIdNameLookup(snapshot?.Filters, ArmyFilterSection.Category),
            snapshot?.FireteamChart);
    }

    /// <summary>
    /// Every faction's units then its Spec-Ops units ("Name - Spec Ops"), the first of each name kept, and the
    /// factions' fireteams merged.
    /// </summary>
    public static MergedUnits Merge(IEnumerable<FactionUnits> factions)
    {
        var units = new Dictionary<string, SelectionUnitSource>(StringComparer.OrdinalIgnoreCase);
        var teams = new Dictionary<string, MergedFireteam>(StringComparer.OrdinalIgnoreCase);
        foreach (var faction in factions)
        {
            MergedFireteam.MergeChart(faction.FireteamChart, teams);
            var resumeByUnitId = faction.Resumes.GroupBy(x => x.UnitId).ToDictionary(x => x.Key, x => x.First());

            foreach (var resume in faction.Resumes)
            {
                var key = resume.Name.Trim();
                if (key.Length > 0 && !units.ContainsKey(key))
                {
                    units[key] = new SelectionUnitSource(
                        faction.FactionId,
                        resume.UnitId,
                        resume.Name,
                        resume.Slug,
                        resume.Type,
                        CompanyUnitDetailsShared.IsCharacterCategory(resume, faction.CategoryLookup),
                        CompanyUnitDetailsShared.BuildUnitSubtitle(resume, faction.TypeLookup, faction.CategoryLookup),
                        IsSpecOps: false,
                        resume.Logo);
                }
            }

            foreach (var specOpsUnit in faction.SpecOps.OrderBy(x => x.EntryOrder))
            {
                resumeByUnitId.TryGetValue(specOpsUnit.UnitId, out var resume);
                var baseName = string.IsNullOrWhiteSpace(specOpsUnit.Name)
                    ? resume?.Name ?? $"Unit {specOpsUnit.UnitId}"
                    : specOpsUnit.Name.Trim();
                var key = $"{baseName} - Spec Ops";
                if (!units.ContainsKey(key))
                {
                    units[key] = new SelectionUnitSource(
                        faction.FactionId,
                        specOpsUnit.UnitId,
                        key,
                        specOpsUnit.Slug,
                        resume?.Type,
                        resume is not null && CompanyUnitDetailsShared.IsCharacterCategory(resume, faction.CategoryLookup),
                        resume is not null ? CompanyUnitDetailsShared.BuildUnitSubtitle(resume, faction.TypeLookup, faction.CategoryLookup) : "Spec Ops",
                        IsSpecOps: true,
                        resume?.Logo);
                }
            }
        }

        return new MergedUnits(units.Values.ToList(), teams.Values.ToList());
    }
}
