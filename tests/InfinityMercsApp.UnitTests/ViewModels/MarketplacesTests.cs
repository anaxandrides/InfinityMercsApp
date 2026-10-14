using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.Views.Adaptive;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace InfinityMercsApp.UnitTests.ViewModels;

public sealed class MarketplaceRowTests
{
    [Fact]
    public void QuantityNeverGoesBelowZero()
    {
        var row = new AugmentRow(new StoreAugment { Name = "Cube" }) { Quantity = 2 };

        row.Quantity = -1;

        Assert.Equal(0, row.Quantity);
    }

    [Fact]
    public void AnItemCostsCreditsAndSwc()
    {
        var row = new StoreItemRow(new StoreItem { Name = "Pistol", CostCr = 3, CostSwc = 0.5m });

        Assert.Equal("3cr / 0.5swc", row.CostDisplay);
        Assert.Equal("Pistol", row.CartName);
    }

    [Fact]
    public void ATroopTypeIsNamedByArmourAndType()
    {
        var row = new TroopTypeRow(new StoreTroopType { ArmorName = "Light Armour", Type = "LI", CostCr = 10, Arm = 1 });

        Assert.Equal("Light Armour (LI)", row.DisplayName);
        Assert.Equal("Light Armour (LI)", row.CartName);
        Assert.Equal("ARM 1  |  BTS —", row.StatDisplay);
        Assert.Equal("10cr", row.CostDisplay);
        Assert.False(row.HasAbilities);
    }

    [Fact]
    public void AnUnnamedTroopTypeIsJustItsArmour() =>
        Assert.Equal("Heavy Armour", new TroopTypeRow(new StoreTroopType { ArmorName = "Heavy Armour" }).DisplayName);

    [Fact]
    public void AnAugmentShowsADashWithoutARequirement()
    {
        var row = new AugmentRow(new StoreAugment { Name = "Cube", CostCr = 4 });

        Assert.False(row.HasRequirement);
        Assert.Equal("—", row.RequirementDisplay);
        Assert.Equal("4cr", row.CostDisplay);
    }

    [Fact]
    public void GroupsItemsByCategoryInStoreOrder()
    {
        var groups = StoreItemGroup.From(
        [
            new StoreItem { Name = "Pistol", Category = "Weapons" },
            new StoreItem { Name = "Medkit", Category = "Gear" },
            new StoreItem { Name = "Rifle", Category = "Weapons" }
        ]);

        Assert.Equal(["Weapons", "Gear"], groups.Select(group => group.Category));
        Assert.Equal(["Pistol", "Rifle"], groups[0].Items.Select(item => item.Name));
    }

    [Fact]
    public void CartLinesAreTheRowsWithAQuantity()
    {
        IEnumerable<MarketplaceRow> rows =
        [
            new StoreItemRow(new StoreItem { Name = "Pistol", CostCr = 3 }) { Quantity = 2 },
            new AugmentRow(new StoreAugment { Name = "Cube", CostCr = 4 })
        ];

        var lines = MarketplaceCartLine.From(rows);

        Assert.Equal<MarketplaceCartLine>([new("Pistol", 2, 6)], lines);
        Assert.Equal("x2", lines[0].QuantityDisplay);
        Assert.Equal("6cr", lines[0].CostDisplay);
    }
}

public sealed class MarketplaceDetailTests
{
    [Fact]
    public void AnItemShowsItsCostCategoryWeaponsAndWikiPage()
    {
        var weapon = new Weapon { Name = "Pistol" };
        var detail = MarketplaceDetail.For(
            new StoreItemRow(new StoreItem { Name = "Pistol", CostCr = 3, Category = "Weapons", WikiUrl = "https://wiki/pistol", WikiSection = "Stats" }),
            [weapon]);

        Assert.Equal("Pistol", detail.Title);
        Assert.Equal<DetailRow>([new("Cost", "3cr"), new("Category", "Weapons")], detail.Rows);
        Assert.Equal([weapon], detail.Weapons);
        Assert.Equal("https://wiki/pistol", detail.WikiUrl);
        Assert.Equal("Stats", detail.WikiSection);
        Assert.False(detail.HasAbilities);
    }

    [Fact]
    public void ABlankWikiLinkIsNoWikiPage() =>
        Assert.Null(MarketplaceDetail.For(new StoreItemRow(new StoreItem { Name = "Kit", WikiUrl = " " }), []).WikiUrl);

    [Fact]
    public void ATroopTypeShowsItsArmourAndAbilities()
    {
        var links = SkillWikiLinks.From([new Skill { Name = "Mimetism", Wiki = "https://wiki/mimetism" }]);

        var detail = MarketplaceDetail.For(
            new TroopTypeRow(new StoreTroopType { ArmorName = "Light Armour", Type = "LI", CostCr = 10, Arm = 1, Bts = 3, Abilities = "Mimetism (-3), Pistol" }),
            links);

        Assert.Equal("Light Armour (LI)", detail.Title);
        Assert.Equal<DetailRow>([new("Cost", "10cr"), new("ARM", "1"), new("BTS", "3")], detail.Rows);
        Assert.Equal<AbilityLink>([new("Mimetism (-3)", "https://wiki/mimetism"), new("Pistol", null)], detail.Abilities);
        Assert.True(detail.HasAbilities);
        Assert.Null(detail.WikiUrl);
    }

    [Fact]
    public void AnAugmentShowsItsRequirementAndNoteWhenItHasThem()
    {
        Assert.Equal<DetailRow>(
            [new("Cost", "4cr"), new("Requires", "Doctor"), new("Note", "Per trooper")],
            MarketplaceDetail.For(new AugmentRow(new StoreAugment { Name = "Cube", CostCr = 4, Requirement = "Doctor", CostNote = "Per trooper" })).Rows);
        Assert.Equal<DetailRow>(
            [new("Cost", "4cr")],
            MarketplaceDetail.For(new AugmentRow(new StoreAugment { Name = "Cube", CostCr = 4 })).Rows);
    }
}

public sealed class MarketplacesViewModelTests
{
    private readonly IStoreProvider _stores = Substitute.For<IStoreProvider>();
    private readonly IMetadataProvider _metadata = Substitute.For<IMetadataProvider>();
    private readonly IWikiDescriptionService _wiki = Substitute.For<IWikiDescriptionService>();
    private readonly ILauncher _launcher = Substitute.For<ILauncher>();

    private static readonly Store BlackMarket = new()
    {
        Name = "Black Market",
        AssociatedType = "Mercenary",
        Alignment = "Chaotic",
        Items =
        [
            new StoreItem { Name = "Pistol", CostCr = 3, Category = "Weapons", WikiUrl = "https://wiki/pistol" },
            new StoreItem { Name = "Medkit", CostCr = 5, Category = "Gear" }
        ],
        TroopTypes = [new StoreTroopType { ArmorName = "Light Armour", Type = "LI", CostCr = 10, Abilities = "Mimetism (-3)" }],
        Augments = [new StoreAugment { Name = "Cube", CostCr = 4 }]
    };

    public MarketplacesViewModelTests()
    {
        _stores.GetAllStoreNames().Returns(["Black Market", "Bazaar"]);
        _stores.GetStoreByNameAsync("Black Market", Arg.Any<CancellationToken>()).Returns(BlackMarket);
    }

    private MarketplacesViewModel CreateViewModel() =>
        new(Substitute.For<INavigationService>(), _stores, _metadata, _wiki, _launcher);

    private async Task<MarketplacesViewModel> WithBlackMarketAsync()
    {
        var viewModel = CreateViewModel();
        await viewModel.SelectStoreCommand.ExecuteAsync("Black Market");
        return viewModel;
    }

    [Fact]
    public void ListsEveryStoreAndShowsNoneAtFirst()
    {
        var viewModel = CreateViewModel();

        Assert.Equal(["Black Market", "Bazaar"], viewModel.StoreNames);
        Assert.False(viewModel.HasStore);
        Assert.False(viewModel.IsDetailOpen);
    }

    [Fact]
    public async Task ShowsTheSelectedStore()
    {
        var viewModel = await WithBlackMarketAsync();

        Assert.True(viewModel.HasStore);
        Assert.Equal("Black Market", viewModel.StoreName);
        Assert.Equal("Factions: All (Neutral)", viewModel.StoreFactionsDisplay);
        Assert.Equal("Type: Mercenary   |   Alignment: Chaotic", viewModel.StoreMetaDisplay);
        Assert.Equal(["Weapons", "Gear"], viewModel.ItemGroups.Select(group => group.Category));
        Assert.Equal(["Light Armour (LI)"], viewModel.TroopTypeRows.Select(row => row.DisplayName));
        Assert.Equal(["Cube"], viewModel.AugmentRows.Select(row => row.Name));
        Assert.True(viewModel.HasItems && viewModel.HasTroopTypes && viewModel.HasAugments);
        Assert.True(viewModel.IsCartEmpty);
        Assert.Equal("Total: 0cr", viewModel.CartTotalDisplay);
    }

    [Fact]
    public async Task ListsTheFactionsAStoreServes()
    {
        _stores.GetStoreByNameAsync("Bazaar", Arg.Any<CancellationToken>())
            .Returns(new Store { Name = "Bazaar", AssociatedFactions = ["Haqqislam", "Nomads"] });
        var viewModel = CreateViewModel();

        await viewModel.SelectStoreCommand.ExecuteAsync("Bazaar");

        Assert.Equal("Factions: Haqqislam, Nomads", viewModel.StoreFactionsDisplay);
        Assert.Equal(string.Empty, viewModel.StoreMetaDisplay);
        Assert.False(viewModel.HasItems || viewModel.HasTroopTypes || viewModel.HasAugments);
    }

    [Fact]
    public async Task PickingAStoreLoadsIt()
    {
        var viewModel = CreateViewModel();

        viewModel.SelectedStoreName = "Black Market";
        await viewModel.SelectStoreCommand.ExecutionTask!;

        Assert.Equal("Black Market", viewModel.StoreName);
    }

    [Fact]
    public async Task AnUnknownStoreLeavesTheCurrentOneShowing()
    {
        var viewModel = await WithBlackMarketAsync();

        await viewModel.SelectStoreCommand.ExecuteAsync("Nowhere");

        Assert.True(viewModel.HasStore);
        Assert.Equal("Black Market", viewModel.StoreName);
    }

    [Fact]
    public async Task ClearingTheStoreHidesIt()
    {
        var viewModel = await WithBlackMarketAsync();

        await viewModel.SelectStoreCommand.ExecuteAsync(null);

        Assert.False(viewModel.HasStore);
    }

    [Fact]
    public async Task TheCartShowsOnlyWithAStoreAndQuantityControls()
    {
        var viewModel = CreateViewModel();
        viewModel.ShowQuantityControls = true;
        Assert.False(viewModel.ShowCart);

        await viewModel.SelectStoreCommand.ExecuteAsync("Black Market");
        Assert.True(viewModel.ShowCart);

        viewModel.ShowQuantityControls = false;
        Assert.False(viewModel.ShowCart);
    }

    [Fact]
    public async Task QuantitiesFillTheCartAndTotal()
    {
        var viewModel = await WithBlackMarketAsync();
        var pistol = viewModel.ItemGroups[0].Items[0];
        var armour = viewModel.TroopTypeRows[0];

        viewModel.IncrementCommand.Execute(pistol);
        viewModel.IncrementCommand.Execute(pistol);
        viewModel.IncrementCommand.Execute(armour);
        viewModel.DecrementCommand.Execute(viewModel.AugmentRows[0]);

        Assert.Equal<MarketplaceCartLine>([new("Pistol", 2, 6), new("Light Armour (LI)", 1, 10)], viewModel.CartLines);
        Assert.Equal("Total: 16cr", viewModel.CartTotalDisplay);
        Assert.True(viewModel.HasCartLines);
        Assert.Equal(0, viewModel.AugmentRows[0].Quantity);

        viewModel.DecrementCommand.Execute(armour);
        Assert.Equal("Total: 6cr", viewModel.CartTotalDisplay);
    }

    [Fact]
    public async Task ChangingStoreEmptiesTheCart()
    {
        var viewModel = await WithBlackMarketAsync();
        viewModel.IncrementCommand.Execute(viewModel.AugmentRows[0]);

        await viewModel.SelectStoreCommand.ExecuteAsync("Black Market");

        Assert.True(viewModel.IsCartEmpty);
    }

    [Fact]
    public async Task AnItemDetailLoadsItsWikiDescription()
    {
        IReadOnlyList<WikiContentBlock> blocks = [new WikiContentBlock { Type = WikiBlockType.Paragraph, Text = "Shoots." }];
        _wiki.FetchContentAsync("https://wiki/pistol", null, Arg.Any<CancellationToken>()).Returns(blocks);
        _metadata.SearchWeaponsByName("Pistol").Returns([new Weapon { Name = "Pistol" }]);
        var viewModel = await WithBlackMarketAsync();

        await viewModel.ShowDetailCommand.ExecuteAsync(viewModel.ItemGroups[0].Items[0]);

        Assert.True(viewModel.IsDetailOpen);
        Assert.Equal("Pistol", viewModel.Detail!.Title);
        Assert.Single(viewModel.Detail.Weapons);
        Assert.Equal(WikiStatus.Loaded, viewModel.WikiStatus);
        Assert.True(viewModel.HasWiki && viewModel.HasWikiBlocks);
        Assert.Same(blocks, viewModel.WikiBlocks);
    }

    [Fact]
    public async Task AnEmptyWikiPageSaysSo()
    {
        _wiki.FetchContentAsync("https://wiki/pistol", null, Arg.Any<CancellationToken>()).Returns([]);
        _metadata.SearchWeaponsByName("Pistol").Returns([]);
        var viewModel = await WithBlackMarketAsync();

        await viewModel.ShowDetailCommand.ExecuteAsync(viewModel.ItemGroups[0].Items[0]);

        Assert.Equal(WikiStatus.Empty, viewModel.WikiStatus);
        Assert.True(viewModel.IsWikiEmpty);
    }

    [Fact]
    public async Task AnItemWithoutAWikiPageFetchesNothing()
    {
        _metadata.SearchWeaponsByName("Medkit").Returns([]);
        var viewModel = await WithBlackMarketAsync();

        await viewModel.ShowDetailCommand.ExecuteAsync(viewModel.ItemGroups[1].Items[0]);

        Assert.Equal(WikiStatus.None, viewModel.WikiStatus);
        Assert.False(viewModel.HasWiki);
        await _wiki.DidNotReceiveWithAnyArgs().FetchContentAsync(default!);
    }

    [Fact]
    public async Task AWikiPageThatArrivesAfterThePopupClosesIsDropped()
    {
        var pending = new TaskCompletionSource<IReadOnlyList<WikiContentBlock>>();
        _wiki.FetchContentAsync("https://wiki/pistol", null, Arg.Any<CancellationToken>()).Returns(pending.Task);
        _metadata.SearchWeaponsByName("Pistol").Returns([]);
        var viewModel = await WithBlackMarketAsync();

        var showing = viewModel.ShowDetailCommand.ExecuteAsync(viewModel.ItemGroups[0].Items[0]);
        Assert.True(viewModel.IsWikiLoading);
        viewModel.CloseDetailCommand.Execute(null);
        pending.SetResult([new WikiContentBlock { Text = "Late" }]);
        await showing;

        Assert.False(viewModel.IsDetailOpen);
        Assert.Empty(viewModel.WikiBlocks);
    }

    [Fact]
    public async Task ATroopTypeDetailLinksItsSkills()
    {
        _metadata.GetSkills().Returns([new Skill { Name = "Mimetism", Wiki = "https://wiki/mimetism" }]);
        var viewModel = await WithBlackMarketAsync();

        await viewModel.ShowDetailCommand.ExecuteAsync(viewModel.TroopTypeRows[0]);

        Assert.Equal<AbilityLink>([new("Mimetism (-3)", "https://wiki/mimetism")], viewModel.Detail!.Abilities);
        Assert.Equal(WikiStatus.None, viewModel.WikiStatus);
    }

    [Fact]
    public async Task AnAugmentDetailClosesWithBack()
    {
        var viewModel = await WithBlackMarketAsync();

        await viewModel.ShowDetailCommand.ExecuteAsync(viewModel.AugmentRows[0]);
        Assert.Equal("Cube", viewModel.Detail!.Title);

        viewModel.CloseDetailCommand.Execute(null);
        Assert.Null(viewModel.Detail);
    }

    [Fact]
    public async Task OpensSkillLinks()
    {
        await CreateViewModel().OpenLinkCommand.ExecuteAsync("https://wiki/mimetism");

        await _launcher.Received(1).OpenAsync(Arg.Is<Uri>(uri => uri.ToString() == "https://wiki/mimetism"));
    }

    [Fact]
    public async Task IgnoresMissingAndFailingLinks()
    {
        _launcher.OpenAsync(Arg.Any<Uri>()).ThrowsAsync(new InvalidOperationException("no browser"));
        var viewModel = CreateViewModel();

        await viewModel.OpenLinkCommand.ExecuteAsync(null);
        await viewModel.OpenLinkCommand.ExecuteAsync("https://wiki/mimetism");

        await _launcher.Received(1).OpenAsync(Arg.Any<Uri>());
    }

    [Fact]
    public void StartsStackedForAPhone()
    {
        var viewModel = CreateViewModel();

        Assert.Equal("*", viewModel.ContentColumns);
        Assert.Equal("Auto,*", viewModel.ContentRows);
        Assert.Equal(new GridCell(1, 0), viewModel.SectionsCell);
        Assert.Equal(new GridCell(0, 0, RowSpan: 2), viewModel.PlaceholderCell);
        Assert.Equal("Auto,Auto,Auto", viewModel.SectionsRows);
        Assert.Equal(new GridCell(2, 0), viewModel.AugmentsCell);
        Assert.False(viewModel.ShowQuantityControls);
    }

    [Fact]
    public void MediumStacksTheStoreInfoButSplitsTheSections()
    {
        var viewModel = CreateViewModel();

        viewModel.ApplyLayoutCommand.Execute(AdaptiveLayoutMode.Medium);

        Assert.Equal("Auto,*", viewModel.ContentRows);
        Assert.Equal("*,*", viewModel.SectionsColumns);
        Assert.Equal(new GridCell(0, 0, RowSpan: 2), viewModel.ItemsCell);
        Assert.Equal(new GridCell(0, 1), viewModel.TroopTypesCell);
        Assert.Equal(new GridCell(1, 1), viewModel.AugmentsCell);
    }

    [Fact]
    public void ExpandedPutsTheStoreInfoInARail()
    {
        var viewModel = CreateViewModel();

        viewModel.ApplyLayoutCommand.Execute(AdaptiveLayoutMode.Expanded);

        Assert.Equal("280,*", viewModel.ContentColumns);
        Assert.Equal("*", viewModel.ContentRows);
        Assert.Equal(12, viewModel.ContentColumnSpacing);
        Assert.Equal(0, viewModel.ContentRowSpacing);
        Assert.Equal(new GridCell(0, 1), viewModel.SectionsCell);
        Assert.Equal(new GridCell(0, 0, ColumnSpan: 2), viewModel.PlaceholderCell);
        Assert.False(viewModel.ShowQuantityControls);
    }

    [Fact]
    public void WideAddsTheCartRailAndBuying()
    {
        var viewModel = CreateViewModel();

        viewModel.ApplyLayoutCommand.Execute(AdaptiveLayoutMode.Wide);

        Assert.Equal("280,*,320", viewModel.ContentColumns);
        Assert.Equal(new GridCell(0, 2), viewModel.CartCell);
        Assert.Equal(new GridCell(0, 0, ColumnSpan: 3), viewModel.PlaceholderCell);
        Assert.True(viewModel.ShowQuantityControls);

        viewModel.ApplyLayoutCommand.Execute(AdaptiveLayoutMode.Compact);
        Assert.False(viewModel.ShowQuantityControls);
        Assert.Equal(new GridCell(1, 0), viewModel.SectionsCell);
    }
}
