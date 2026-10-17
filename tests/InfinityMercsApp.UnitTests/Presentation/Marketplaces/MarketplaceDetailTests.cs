using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Domain.Models.Metadata.Wiki;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Presentation.Marketplaces;
using InfinityMercsApp.ViewModels.Marketplaces.Items;

namespace InfinityMercsApp.UnitTests.Presentation.Marketplaces;

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
