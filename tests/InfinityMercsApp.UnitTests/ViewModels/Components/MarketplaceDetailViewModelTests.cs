using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Infrastructure.Providers.Metadata;
using InfinityMercsApp.Services.Wiki;
using InfinityMercsApp.ViewModels.Components;
using InfinityMercsApp.ViewModels.Marketplaces.Items;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Components;

public sealed class MarketplaceDetailViewModelTests
{
    private readonly IMetadataProvider _metadata = Substitute.For<IMetadataProvider>();

    private MarketplaceDetailViewModel Create() =>
        new(_metadata, Substitute.For<IWikiDescriptionService>(), Substitute.For<ILauncher>());

    [Fact]
    public async Task AWeaponShowsItsProfilesUnderItsName()
    {
        _metadata.SearchWeaponsByName("Spitfire").Returns([new Weapon { Name = "Spitfire" }]);
        var popup = Create();

        await popup.ShowWeaponAsync("Spitfire");

        Assert.True(popup.IsOpen);
        Assert.Equal("Spitfire", popup.Detail?.Title);
        Assert.Empty(popup.Detail?.Rows ?? []);
        Assert.Single(popup.Detail?.Weapons ?? []);
        Assert.False(popup.HasWiki);
    }

    [Fact]
    public async Task ARowItDoesNotKnowOpensNothing()
    {
        var popup = Create();

        await popup.ShowRowAsync(new UnknownRow());

        Assert.False(popup.IsOpen);
    }

    private sealed class UnknownRow() : MarketplaceRow("Mystery", 1)
    {
        public override string InventoryCategory => "General";
    }
}
