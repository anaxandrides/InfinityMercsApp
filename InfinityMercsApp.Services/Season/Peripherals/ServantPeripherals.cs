using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;

namespace InfinityMercsApp.Services.Season.Peripherals;

/// <summary>A company's Servant and Ancillary peripherals, which stay equipment of their trooper rather than troopers of their own.</summary>
public sealed class ServantPeripherals(IArmyDataService armyData)
{
    /// <summary>The entry indexes of the company's Servant peripherals, read from their skills in the army data.</summary>
    public IReadOnlySet<int> Of(SavedCompanyFile company) =>
        company.Entries
            .Where(entry => SeasonTroopers.IsServantPeripheral(entry, armyData.GetFactionSnapshot(SeasonTroopers.SourceFactionId(entry))?.Filters))
            .Select(entry => entry.EntryIndex)
            .ToHashSet();
}
