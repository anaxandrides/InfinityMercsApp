using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.Factions;
using InfinityMercsApp.Services.Navigation;
using NSubstitute;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;

namespace InfinityMercsApp.UnitTests.Services.CompanySelection.Factions;

public sealed class CompanyFactionCatalogTests
{
    private readonly IArmyDataService _armyData = Substitute.For<IArmyDataService>();

    // Svalarheima's Winter Force sits by the Neoterran Capitaline Army, as in the Infinity Army, not among the S's.
    [Fact]
    public async Task LoadAsync_Sectorials_KeepsTheInfinityArmyOrder()
    {
        Stored(
            Sectorial(201, 202, "Imperial Service"),
            Sectorial(101, 106, "Svalarheima's Winter Force"),
            Sectorial(101, 104, "Neoterran Capitaline Army"),
            Sectorial(301, 305, "Tartary Army Corps"),
            Sectorial(101, 103, "Military Orders"));

        var factions = await new CompanyFactionCatalog(_armyData).LoadAsync(ArmySourceSelectionMode.Sectorials);

        Assert.Equal([103, 104, 106, 202, 305], factions.Select(faction => faction.Id));
    }

    [Fact]
    public async Task LoadAsync_VanillaFactions_KeepsTheInfinityArmyOrder()
    {
        Stored(Sectorial(301, 301, "Ariadna"), Sectorial(101, 101, "PanOceania"), Sectorial(101, 104, "Neoterran Capitaline Army"));

        var factions = await new CompanyFactionCatalog(_armyData).LoadAsync(ArmySourceSelectionMode.VanillaFactions);

        Assert.Equal([101, 301], factions.Select(faction => faction.Id));
    }

    private void Stored(params IEnumerable<FactionRecord> factions) =>
        _armyData.GetMetadataFactions(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(factions.ToList());

    private static FactionRecord Sectorial(int parentId, int id, string name) => new() { Id = id, ParentId = parentId, Name = name };
}
