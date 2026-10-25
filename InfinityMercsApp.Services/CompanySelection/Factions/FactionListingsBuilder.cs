using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.Teams;
using InfinityMercsApp.Services.Navigation;

namespace InfinityMercsApp.Services.CompanySelection.Factions;

/// <summary>
/// Works out <see cref="FactionListings"/> from the armies stored, with the same checks the selection screens make:
/// <see cref="LieutenantFactions"/> and <see cref="CoreFireteamEligibility"/> over the sectorials they list.
/// </summary>
public sealed class FactionListingsBuilder(IArmyDataService armyData)
{
    public async Task<FactionListings> BuildAsync(CancellationToken cancellationToken = default)
    {
        var sectorials = await new CompanyFactionCatalog(armyData).LoadAsync(ArmySourceSelectionMode.Sectorials, cancellationToken);
        var eligibility = new CoreFireteamEligibility(armyData);
        return new FactionListings
        {
            LieutenantFactionIds = sectorials
                .Where(faction => LieutenantFactions.Has(armyData, faction.Id, cancellationToken))
                .Select(faction => faction.Id)
                .ToList(),
            Cohesive = FactionListings.PointsLimits
                .Select(points => new CohesiveListing(
                    points,
                    sectorials
                        .Select(faction => new FactionFireteams(
                            faction.Id, eligibility.ValidCoreFireteams(faction.Id, UnitFilterCriteria.None, points, cancellationToken)))
                        .Where(faction => faction.ValidCoreFireteams.Count > 0)
                        .ToList()))
                .ToList()
        };
    }
}
