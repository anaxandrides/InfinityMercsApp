using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Services.CompanySelection.StartCompany;
using InfinityMercsApp.UnitTests.Support;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Services.CompanySelection.StartCompany;

public sealed class StartingGearCatalogTests
{
    private readonly IStoreProvider _stores = Substitute.For<IStoreProvider>();

    public StartingGearCatalogTests() =>
        _stores.GetAvailableStoresAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(
        [
            Store("Base Market", [], Item("Combi Rifle", "Primary", 5), Item("Light Armor", "Armor", 4), Item("SWC +0.5", "Exchange", 10)),
            Store("Number One", ["PanOceania"], Item("Combi Rifle", "Primary", 4), Item("Stun Pistol", "Sidearm", 1)),
            Store("Salaam Suuk", ["Haqqislam"], Item("Shock Mine", "Secondary", 3)),
            Store("Medical Services", [], Item("Regular Training", "Services", 2))
        ]);

    private static Store Store(string name, IReadOnlyList<string> factions, params IEnumerable<StoreItem> items) =>
        new() { Name = name, AssociatedFactions = factions, Items = items.ToList() };

    private static StoreItem Item(string name, string category, int cost) => new() { Name = name, Category = category, CostCr = cost };

    [Fact]
    public async Task ForAsync_PanOceania_OffersTheBaseMarketAndItsOwnWarMarketCheapestFirst()
    {
        var armyData = Army101Data.Create();
        armyData.GetMetadataFactionById(Army101Data.PanOceania, Arg.Any<CancellationToken>())
            .Returns(new Domain.Models.Metadata.Faction { Id = Army101Data.PanOceania, ParentId = Army101Data.PanOceania, Name = "PanOceania" });

        var options = await new StartingGearCatalog(_stores, armyData).ForAsync(Army101Data.PanOceania);

        Assert.Equal(["Stun Pistol (1 CR, Number One)", "Combi Rifle (4 CR, Number One)"], options.Select(option => option.Label));
    }
}
