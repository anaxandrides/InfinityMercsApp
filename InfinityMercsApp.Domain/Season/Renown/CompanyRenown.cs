using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Season.Inventory;

namespace InfinityMercsApp.Domain.Season.Renown;

/// <summary>
/// How well known a company is: the renown of each of its troopers, what retired troopers kept, plus 10 for every
/// contract it has won.
/// </summary>
public static class CompanyRenown
{
    public const int VictoryRenown = 10;

    /// <summary>
    /// The company's renown from its active troopers' renown, what its retired troopers kept, and the contracts won
    /// this season.
    /// </summary>
    public static int Of(IEnumerable<TrooperRenown> troopers, SeasonFile? seasonFile) =>
        troopers.Sum(trooper => trooper.Total) + (seasonFile?.RetiredRenown ?? 0) + VictoryRenown * Victories(seasonFile);

    /// <summary>The contracts won this season; a bye round is not a win.</summary>
    public static int Victories(SeasonFile? seasonFile) =>
        seasonFile?.Rounds.Count(round => round.MissionResults is { Won: true, IsBye: false }) ?? 0;

    /// <summary>
    /// The base cost of the gear a trooper has equipped, as the markets list it. Gear no market sells adds nothing.
    /// </summary>
    public static int GearCost(IEnumerable<SeasonUnitGear> gear, InventoryCatalog catalog) =>
        gear.Where(item => !string.IsNullOrWhiteSpace(item.ItemName))
            .Sum(item => catalog.Find(item.ItemName)?.CostCr ?? 0);
}
