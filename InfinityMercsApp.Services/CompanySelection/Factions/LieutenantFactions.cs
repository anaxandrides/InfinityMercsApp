using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Services.ArmyData;

namespace InfinityMercsApp.Services.CompanySelection.Factions;

/// <summary>Which factions have a unit able to be lieutenant: those a generated company's captain can come from.</summary>
public static class LieutenantFactions
{
    /// <summary>Whether a Mercs unit of the faction has a lieutenant option.</summary>
    public static bool Has(IArmyDataService armyData, int factionId, CancellationToken cancellationToken = default)
    {
        var skills = (armyData.GetFactionSnapshot(factionId, cancellationToken)?.Filters ?? ArmyFilterCatalog.Empty)
            .Names(ArmyFilterSection.Skills);
        return armyData
            .GetMergedMercsArmyList([factionId], cancellationToken)
            .Any(entry => entry.ProfileGroups.HasLieutenantOption(skills));
    }
}
