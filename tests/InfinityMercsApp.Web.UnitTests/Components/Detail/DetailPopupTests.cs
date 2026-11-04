using Bunit;
using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Domain.Models.Metadata.Ranges;
using InfinityMercsApp.Domain.Models.Metadata.Wiki;
using InfinityMercsApp.Infrastructure.Providers.Metadata;
using InfinityMercsApp.Presentation;
using InfinityMercsApp.Presentation.Marketplaces;
using InfinityMercsApp.Presentation.Weapons;
using InfinityMercsApp.Services.Links;
using InfinityMercsApp.Services.Wiki;
using InfinityMercsApp.ViewModels.Components;
using InfinityMercsApp.Web.Components.Detail;
using NSubstitute;

namespace InfinityMercsApp.Web.UnitTests.Components.Detail;

/// <summary>A weapon or market row in full: its facts, abilities, wiki description and weapon profiles.</summary>
public sealed class DetailPopupTests : BunitContext
{
    private const string WikiPage = "https://wiki.example/Combi_Rifle";

    private readonly IWikiDescriptionService _wiki = Substitute.For<IWikiDescriptionService>();
    private readonly MarketplaceDetailViewModel _detail;

    public DetailPopupTests() =>
        _detail = new MarketplaceDetailViewModel(Substitute.For<IMetadataProvider>(), _wiki, Substitute.For<ILinkLauncher>());

    private static Weapon CombiRifle() => new()
    {
        Name = "Combi Rifle",
        Mode = "Fire",
        Burst = "3",
        Damage = "13",
        Saving = "ARM",
        Properties = ["Suppressive Fire"],
        Distance = new WeaponDistance
        {
            Bands =
            [
                new WeaponDistanceBand { Name = WeaponDistance.ShortBand, Max = 20, Mod = "3" },
                new WeaponDistanceBand { Name = WeaponDistance.MediumBand, Max = 40, Mod = "0" },
                new WeaponDistanceBand { Name = WeaponDistance.LongBand, Max = 60, Mod = "-3" },
                new WeaponDistanceBand { Name = WeaponDistance.MaximumBand, Max = 120, Mod = "-6" }
            ]
        }
    };

    [Fact]
    public async Task Render_RowWithAbilitiesAndWeapon_ShowsThemAll()
    {
        var popup = Render<DetailPopup>(parameters => parameters.Add(p => p.ViewModel, _detail));

        await popup.InvokeAsync(() => _detail.ShowAsync(new MarketplaceDetail("Fusilier Armor", [new DetailRow("Cost", "10cr")])
        {
            Abilities = [new AbilityLink("Courage", "https://wiki.example/Courage"), new AbilityLink("+1 ARM", null)],
            Weapons = [CombiRifle()]
        }));

        Assert.Equal("Fusilier Armor", popup.Find("h2").TextContent);
        Assert.Equal("10cr", popup.Find(".kit dd").TextContent);
        Assert.Equal("https://wiki.example/Courage", popup.Find(".abilities a").GetAttribute("href"));
        Assert.Contains("+1 ARM", popup.Find(".abilities").TextContent, StringComparison.Ordinal);
        Assert.Single(popup.FindAll(".weapon-card"));
        Assert.Equal(4, popup.FindAll(".range-bar .band").Count);
    }

    [Fact]
    public async Task Render_WikiDescription_ShowsItsBlocks()
    {
        _wiki.FetchContentAsync(WikiPage, null, Arg.Any<CancellationToken>()).Returns(
        [
            new WikiContentBlock { Type = WikiBlockType.SectionHeader, Text = "Combi Rifle" },
            new WikiContentBlock { Type = WikiBlockType.Paragraph, Text = "A rifle.", Bold = true },
            new WikiContentBlock { Type = WikiBlockType.BulletItem, Text = "Short range", IndentLevel = 1 }
        ]);
        var popup = Render<DetailPopup>(parameters => parameters.Add(p => p.ViewModel, _detail));

        await popup.InvokeAsync(() => _detail.ShowAsync(new MarketplaceDetail("Combi Rifle", []) { WikiUrl = WikiPage }));

        popup.WaitForAssertion(() => Assert.Equal("Combi Rifle", popup.Find(".wiki-header").TextContent));
        Assert.NotNull(popup.Find("p.bold"));
        Assert.NotNull(popup.Find(".wiki-bullet.indented"));
    }

    [Fact]
    public async Task Render_WikiHasNothing_SaysSo()
    {
        _wiki.FetchContentAsync(WikiPage, null, Arg.Any<CancellationToken>()).Returns([]);
        var popup = Render<DetailPopup>(parameters => parameters.Add(p => p.ViewModel, _detail));

        await popup.InvokeAsync(() => _detail.ShowAsync(new MarketplaceDetail("Combi Rifle", []) { WikiUrl = WikiPage }));

        popup.WaitForAssertion(() => Assert.Contains("(No description available)", popup.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Render_WikiStillLoading_SaysSo()
    {
        var pending = new TaskCompletionSource<IReadOnlyList<WikiContentBlock>>();
        _wiki.FetchContentAsync(WikiPage, null, Arg.Any<CancellationToken>()).Returns(pending.Task);
        var popup = Render<DetailPopup>(parameters => parameters.Add(p => p.ViewModel, _detail));

        _ = popup.InvokeAsync(() => _detail.ShowAsync(new MarketplaceDetail("Combi Rifle", []) { WikiUrl = WikiPage }));

        popup.WaitForAssertion(() => Assert.Contains("Loading…", popup.Markup, StringComparison.Ordinal));
        pending.SetResult([]);
    }

    [Fact]
    public async Task Escape_Open_ClosesIt()
    {
        var popup = Render<DetailPopup>(parameters => parameters.Add(p => p.ViewModel, _detail));
        await popup.InvokeAsync(() => _detail.ShowAsync(new MarketplaceDetail("Combi Rifle", [])));

        popup.Find(".dialog").KeyDown("Escape");

        Assert.Empty(popup.FindAll(".dialog"));
    }

    [Fact]
    public async Task Backdrop_Open_ClosesIt()
    {
        var popup = Render<DetailPopup>(parameters => parameters.Add(p => p.ViewModel, _detail));
        await popup.InvokeAsync(() => _detail.ShowAsync(new MarketplaceDetail("Combi Rifle", [])));

        popup.Find(".backdrop").Click();

        Assert.Empty(popup.FindAll(".dialog"));
    }

    [Fact]
    public void WeaponCard_GameModifiers_AdjustBurstAndDamageAndEaseTheRange()
    {
        var card = Render<WeaponCardView>(parameters => parameters
            .Add(p => p.Weapon, CombiRifle())
            .Add(p => p.BurstBonus, 1)
            .Add(p => p.DamageReduction, 2)
            .Add(p => p.XVisor, true)
            .Add(p => p.InInches, false));

        Assert.Contains("4", card.FindAll(".weapon-stats dd")[0].TextContent, StringComparison.Ordinal);
        Assert.Contains("11", card.Markup, StringComparison.Ordinal);
        Assert.Contains("-3", card.FindAll(".range-bar .band")[3].TextContent, StringComparison.Ordinal);
        Assert.Contains("cm", card.Find(".range-bar .marks").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void RuleLink_WithAPage_LinksToIt()
    {
        var link = Render<RuleLink>(parameters => parameters.Add(p => p.Text, new LinkedText("Suppressive Fire", "https://wiki.example/SF")));

        Assert.Equal("https://wiki.example/SF", link.Find("a").GetAttribute("href"));
    }

    [Fact]
    public void RuleLink_WithoutAPage_IsPlainText()
    {
        var link = Render<RuleLink>(parameters => parameters.Add(p => p.Text, new LinkedText("Silhouette", null)));

        Assert.Empty(link.FindAll("a"));
        Assert.Equal("Silhouette", link.Find("span").TextContent);
    }

    [Fact]
    public void RangeBar_NoBands_ShowsNothing()
    {
        var bar = Render<RangeBarView>(parameters => parameters.Add(p => p.Bar, RangeBar.Empty));

        Assert.Empty(bar.FindAll(".range-bar"));
    }
}
