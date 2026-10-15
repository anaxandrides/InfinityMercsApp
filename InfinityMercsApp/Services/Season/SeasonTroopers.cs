using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Troopers;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;

namespace InfinityMercsApp.Services.Season;

/// <summary>Saved company troopers seen through the season so far.</summary>
public static class SeasonTroopers
{
    /// <summary>
    /// The trooper's season progress. Results saved before entries were recorded match the name the deployment
    /// screens used: the custom name, else the unit's.
    /// </summary>
    public static TrooperProgress ProgressOf(SavedCompanyEntry entry, SeasonFile? season)
    {
        var baseName = string.IsNullOrWhiteSpace(entry.BaseUnitName) ? entry.Name : entry.BaseUnitName;
        var name = string.IsNullOrWhiteSpace(entry.CustomName) ? baseName : entry.CustomName.Trim();
        return TrooperProgress.For(
            entry.EntryIndex,
            name,
            entry.ExperiencePoints,
            entry.Perks.Select(perk => perk.Id).ToList(),
            UnitTypeCodes.IsTag(entry.UnitTypeCode),
            season);
    }
}
