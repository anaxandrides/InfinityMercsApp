using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Services.ArmyData;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;

namespace InfinityMercsApp.Services.CompanySelection.Units;

/// <summary>The units a company selection screen lists for its source factions.</summary>
public sealed class SelectionUnitCatalog(IArmyDataService armyData)
{
    /// <summary>
    /// A faction's listed units (<paramref name="resumes"/>, which differ by company) with the Spec-Ops it offers as
    /// captain (<paramref name="specOps"/>; none for a company that can't have one) and its names.
    /// </summary>
    public FactionUnits Read(
        int factionId,
        IReadOnlyList<ArmyResumeRecord> resumes,
        IReadOnlyList<ArmyResumeRecord> specOps,
        CancellationToken cancellationToken = default)
    {
        var snapshot = armyData.GetFactionSnapshot(factionId, cancellationToken);
        return new FactionUnits(
            factionId,
            resumes,
            specOps,
            (snapshot?.Filters ?? ArmyFilterCatalog.Empty).Names(ArmyFilterSection.Type),
            (snapshot?.Filters ?? ArmyFilterCatalog.Empty).Names(ArmyFilterSection.Category),
            snapshot?.FireteamChart);
    }

    /// <summary>
    /// Every faction's units then the Spec-Ops it offers, the first of each name kept, and the factions' fireteams
    /// merged.
    /// </summary>
    public static MergedUnits Merge(IEnumerable<FactionUnits> factions)
    {
        var units = new Dictionary<string, SelectionUnitSource>(StringComparer.OrdinalIgnoreCase);
        var teams = new Dictionary<string, MergedFireteam>(StringComparer.OrdinalIgnoreCase);
        foreach (var faction in factions)
        {
            MergedFireteam.MergeChart(faction.FireteamChart, teams);
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
                        UnitClassification.IsCharacter(resume, faction.CategoryLookup),
                        UnitClassification.Subtitle(resume, faction.TypeLookup, faction.CategoryLookup),
                        IsSpecOps: false,
                        resume.Logo);
                }
            }

            foreach (var resume in faction.SpecOps)
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
                        IsCharacter: false,
                        UnitClassification.Subtitle(resume, faction.TypeLookup, faction.CategoryLookup),
                        IsSpecOps: true,
                        resume.Logo);
                }
            }
        }

        return new MergedUnits(units.Values.ToList(), teams.Values.ToList());
    }
}
