using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Domain.Models.Metadata.Wiki;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Infrastructure.Providers.Metadata;
using InfinityMercsApp.Presentation.Layouts;
using InfinityMercsApp.Presentation.Marketplaces;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Wiki;
using InfinityMercsApp.ViewModels.Components;
using InfinityMercsApp.ViewModels.Marketplaces;
using InfinityMercsApp.ViewModels.Marketplaces.Items;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using InfinityMercsApp.Services.Links;

namespace InfinityMercsApp.UnitTests.ViewModels.Marketplaces;

public sealed class MarketplacesViewModelTests
{
    private readonly IStoreProvider _stores = Substitute.For<IStoreProvider>();
    private readonly IMetadataProvider _metadata = Substitute.For<IMetadataProvider>();
    private readonly IWikiDescriptionService _wiki = Substitute.For<IWikiDescriptionService>();
    private readonly ILinkLauncher _launcher = Substitute.For<ILinkLauncher>();

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
        new(Substitute.For<INavigationService>(), _stores, new MarketplaceDetailViewModel(_metadata, _wiki, _launcher));

    private async Task<MarketplacesViewModel> WithBlackMarketAsync()
    {
        var viewModel = CreateViewModel();
        await viewModel.SelectStoreCommand.ExecuteAsync("Black Market");
        return viewModel;
    }

    [Fact]
    public void Constructor_Stores_ListsEveryStoreShowingNone()
    {
        var viewModel = CreateViewModel();

        Assert.Equal(["Black Market", "Bazaar"], viewModel.StoreNames);
        Assert.False(viewModel.HasStore);
        Assert.False(viewModel.DetailPopup.IsOpen);
    }

    [Fact]
    public async Task SelectStoreCommand_KnownStore_ShowsIt()
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
    public async Task SelectStoreCommand_StoreWithFactions_ListsThem()
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
    public async Task SelectedStoreName_Set_LoadsTheStore()
    {
        var viewModel = CreateViewModel();

        viewModel.SelectedStoreName = "Black Market";
        Assert.NotNull(viewModel.SelectStoreCommand.ExecutionTask);
        await viewModel.SelectStoreCommand.ExecutionTask;

        Assert.Equal("Black Market", viewModel.StoreName);
    }

    [Fact]
    public async Task SelectStoreCommand_UnknownStore_KeepsTheCurrentOne()
    {
        var viewModel = await WithBlackMarketAsync();

        await viewModel.SelectStoreCommand.ExecuteAsync("Nowhere");

        Assert.True(viewModel.HasStore);
        Assert.Equal("Black Market", viewModel.StoreName);
    }

    [Fact]
    public async Task SelectStoreCommand_Null_HidesTheStore()
    {
        var viewModel = await WithBlackMarketAsync();

        await viewModel.SelectStoreCommand.ExecuteAsync(null);

        Assert.False(viewModel.HasStore);
    }

    [Fact]
    public async Task ShowCart_WideLayout_OnlyWithAStore()
    {
        var viewModel = CreateViewModel();
        viewModel.ApplyLayoutCommand.Execute(AdaptiveLayoutMode.Wide);
        Assert.False(viewModel.ShowCart);

        await viewModel.SelectStoreCommand.ExecuteAsync("Black Market");
        Assert.True(viewModel.ShowCart);

        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        viewModel.ApplyLayoutCommand.Execute(AdaptiveLayoutMode.Expanded);
        Assert.False(viewModel.ShowCart);
        Assert.Contains(nameof(MarketplacesViewModel.ShowCart), changed);
    }

    [Fact]
    public async Task IncrementCommand_Rows_FillsTheCartAndTotal()
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
    public async Task SelectStoreCommand_CartFilled_EmptiesTheCart()
    {
        var viewModel = await WithBlackMarketAsync();
        viewModel.IncrementCommand.Execute(viewModel.AugmentRows[0]);

        await viewModel.SelectStoreCommand.ExecuteAsync("Black Market");

        Assert.True(viewModel.IsCartEmpty);
    }

    [Fact]
    public async Task ShowDetailCommand_ItemWithWikiPage_LoadsItsDescription()
    {
        IReadOnlyList<WikiContentBlock> blocks = [new WikiContentBlock { Type = WikiBlockType.Paragraph, Text = "Shoots." }];
        _wiki.FetchContentAsync("https://wiki/pistol", null, Arg.Any<CancellationToken>()).Returns(blocks);
        _metadata.SearchWeaponsByName("Pistol").Returns([new Weapon { Name = "Pistol" }]);
        var viewModel = await WithBlackMarketAsync();

        await viewModel.ShowDetailCommand.ExecuteAsync(viewModel.ItemGroups[0].Items[0]);

        Assert.True(viewModel.DetailPopup.IsOpen);
        var detail = Assert.IsType<MarketplaceDetail>(viewModel.DetailPopup.Detail);
        Assert.Equal("Pistol", detail.Title);
        Assert.Single(detail.Weapons);
        Assert.Equal(WikiStatus.Loaded, viewModel.DetailPopup.WikiStatus);
        Assert.True(viewModel.DetailPopup.HasWiki && viewModel.DetailPopup.HasWikiBlocks);
        Assert.Same(blocks, viewModel.DetailPopup.WikiBlocks);
    }

    [Fact]
    public async Task ShowDetailCommand_EmptyWikiPage_SaysSo()
    {
        _wiki.FetchContentAsync("https://wiki/pistol", null, Arg.Any<CancellationToken>()).Returns([]);
        _metadata.SearchWeaponsByName("Pistol").Returns([]);
        var viewModel = await WithBlackMarketAsync();

        await viewModel.ShowDetailCommand.ExecuteAsync(viewModel.ItemGroups[0].Items[0]);

        Assert.Equal(WikiStatus.Empty, viewModel.DetailPopup.WikiStatus);
        Assert.True(viewModel.DetailPopup.IsWikiEmpty);
    }

    [Fact]
    public async Task ShowDetailCommand_NoWikiPage_FetchesNothing()
    {
        _metadata.SearchWeaponsByName("Medkit").Returns([]);
        var viewModel = await WithBlackMarketAsync();

        await viewModel.ShowDetailCommand.ExecuteAsync(viewModel.ItemGroups[1].Items[0]);

        Assert.Equal(WikiStatus.None, viewModel.DetailPopup.WikiStatus);
        Assert.False(viewModel.DetailPopup.HasWiki);
        await _wiki.DidNotReceiveWithAnyArgs().FetchContentAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task ShowDetailCommand_WikiArrivesAfterClose_DropsIt()
    {
        var pending = new TaskCompletionSource<IReadOnlyList<WikiContentBlock>>();
        _wiki.FetchContentAsync("https://wiki/pistol", null, Arg.Any<CancellationToken>()).Returns(pending.Task);
        _metadata.SearchWeaponsByName("Pistol").Returns([]);
        var viewModel = await WithBlackMarketAsync();

        var showing = viewModel.ShowDetailCommand.ExecuteAsync(viewModel.ItemGroups[0].Items[0]);
        Assert.True(viewModel.DetailPopup.IsWikiLoading);
        viewModel.DetailPopup.CloseCommand.Execute(null);
        pending.SetResult([new WikiContentBlock { Text = "Late" }]);
        await showing;

        Assert.False(viewModel.DetailPopup.IsOpen);
        Assert.Empty(viewModel.DetailPopup.WikiBlocks);
    }

    [Fact]
    public async Task ShowDetailCommand_TroopType_LinksItsSkills()
    {
        _metadata.GetSkills().Returns([new Skill { Name = "Mimetism", Wiki = "https://wiki/mimetism" }]);
        var viewModel = await WithBlackMarketAsync();

        await viewModel.ShowDetailCommand.ExecuteAsync(viewModel.TroopTypeRows[0]);

        var detail = Assert.IsType<MarketplaceDetail>(viewModel.DetailPopup.Detail);
        Assert.Equal<AbilityLink>([new("Mimetism (-3)", "https://wiki/mimetism")], detail.Abilities);
        Assert.Equal(WikiStatus.None, viewModel.DetailPopup.WikiStatus);
    }

    [Fact]
    public async Task DetailPopupCloseCommand_AugmentDetail_ClosesIt()
    {
        var viewModel = await WithBlackMarketAsync();

        await viewModel.ShowDetailCommand.ExecuteAsync(viewModel.AugmentRows[0]);
        var detail = Assert.IsType<MarketplaceDetail>(viewModel.DetailPopup.Detail);
        Assert.Equal("Cube", detail.Title);

        viewModel.DetailPopup.CloseCommand.Execute(null);
        Assert.Null(viewModel.DetailPopup.Detail);
    }

    [Fact]
    public async Task OpenLinkCommand_SkillLink_OpensIt()
    {
        await CreateViewModel().DetailPopup.OpenLinkCommand.ExecuteAsync("https://wiki/mimetism");

        await _launcher.Received(1).OpenAsync(Arg.Is<string>(uri => uri == "https://wiki/mimetism"));
    }

    [Fact]
    public async Task OpenLinkCommand_MissingOrFailingLink_IgnoresIt()
    {
        _launcher.OpenAsync(Arg.Any<string>()).ThrowsAsync(new InvalidOperationException("no browser"));
        var viewModel = CreateViewModel();

        await viewModel.DetailPopup.OpenLinkCommand.ExecuteAsync(null);
        await viewModel.DetailPopup.OpenLinkCommand.ExecuteAsync("https://wiki/mimetism");

        await _launcher.Received(1).OpenAsync(Arg.Any<string>());
    }
}
