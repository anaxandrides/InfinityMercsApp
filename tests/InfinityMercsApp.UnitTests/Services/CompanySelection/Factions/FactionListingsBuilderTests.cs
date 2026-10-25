using InfinityMercsApp.Domain.Models.Filtering;
using InfinityMercsApp.Infrastructure.Json;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.Factions;
using InfinityMercsApp.Services.CompanySelection.Teams;
using InfinityMercsApp.UnitTests.Characterization;
using InfinityMercsApp.UnitTests.Infrastructure.Json;

namespace InfinityMercsApp.UnitTests.Services.CompanySelection.Factions;

/// <summary>The listings worked out ahead must say what the selection screens' own checks say.</summary>
public sealed class FactionListingsBuilderTests
{
    // A sectorial in the reference metadata, given the reference PanOceania army; the other sectorials have no army.
    private const int Sectorial = 102;

    private static async Task<IArmyDataService> ArmyDataAsync()
    {
        var armies = await StoredArmies.CreateAsync();
        armies.Import.Import(Sectorial, ArmyJsonReader.Shared.ReadArmyPayload(File.ReadAllText(RepoFiles.Resolve(ReferenceCorpus.Army101Path)))
            ?? throw new InvalidOperationException("The reference army didn't read."));
        return ArmyDataServices.Over(armies.Metadata, armies.Factions);
    }

    [Fact]
    public async Task BuildAsync_StoredSectorial_ListsItAsTheScreensWould()
    {
        var armyData = await ArmyDataAsync();

        var listings = await new FactionListingsBuilder(armyData).BuildAsync();

        Assert.Equal(LieutenantFactions.Has(armyData, Sectorial), listings.LieutenantFactionIds.Contains(Sectorial));
        var eligibility = new CoreFireteamEligibility(armyData);
        foreach (var points in FactionListings.PointsLimits)
        {
            var listing = listings.CohesiveAt(points) ?? throw new InvalidOperationException($"No listing at {points}.");
            var expected = eligibility.ValidCoreFireteams(Sectorial, UnitFilterCriteria.None, points);
            Assert.Equal(expected, listing.Factions.SingleOrDefault(faction => faction.FactionId == Sectorial)?.ValidCoreFireteams ?? []);
        }
    }

    [Fact]
    public async Task BuildAsync_ReferenceArmy_HasALieutenantSectorial() =>
        Assert.Equal([Sectorial], (await new FactionListingsBuilder(await ArmyDataAsync()).BuildAsync()).LieutenantFactionIds);
}
