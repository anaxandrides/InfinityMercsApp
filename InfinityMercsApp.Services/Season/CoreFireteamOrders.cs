using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Company;

namespace InfinityMercsApp.Services.Season;

/// <summary>
/// Who a Cohesive company's core fireteam leaves out, and so gives Irregular orders. It is worked out afresh from
/// the troopers still with the company, so a retirement can free a fireteam place for someone outside it.
/// </summary>
public static class CoreFireteamOrders
{
    /// <summary>
    /// The entries left out of the core fireteam: the active troopers, lieutenant first and then in recruitment order,
    /// each claim a place while one they fit has room. Peripherals take no place. Null when the company saved no core
    /// fireteam, so each trooper's profile decides.
    /// </summary>
    public static IReadOnlySet<int>? Irregular(SavedCompanyFile company, SeasonFile? season)
    {
        if (company.CoreFireteamSlots.Count == 0)
        {
            return null;
        }

        var troopers = company.Entries
            .Where(entry => !entry.IsPeripheralUnit && season?.IsRetired(entry.EntryIndex) != true)
            .OrderByDescending(entry => entry.IsLieutenant)
            .ThenBy(entry => entry.EntryIndex)
            .ToList();
        var claimed = FireteamLevels.ClaimSlots(
            troopers.Select(entry => new FireteamMember(entry.BaseUnitName, entry.Name, entry.SourceUnitId, entry.SourceFactionId)).ToList(),
            company.CoreFireteamSlots);
        return troopers.Where((_, position) => !claimed[position]).Select(entry => entry.EntryIndex).ToHashSet();
    }
}
