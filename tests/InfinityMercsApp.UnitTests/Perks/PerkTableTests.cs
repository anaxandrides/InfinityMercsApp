using InfinityMercsApp.Domain.Models.Perks.Catalog;
using InfinityMercsApp.Domain.Models.Perks.Tables;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Reference;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Perks;

public sealed class PerkTableTests
{
    private static PerkNode Node(string id, string name, int tier, string? parentId = null) =>
        new() { Id = id, Name = name, Tier = tier, ParentId = parentId };

    private static PerkNodeList List(params IEnumerable<PerkNode> roots) =>
        new() { ListId = "test-list", ListName = "Test", Roots = roots.ToList() };

    [Fact]
    public void Options_Catalog_PutsAttributeListsFirstWithRolls()
    {
        var options = PerkTable.Options();

        Assert.Equal(["Initiative", "Cool", "Body", "Reflex", "Intelligence", "Empathy", "Mecha"], options.Take(7).Select(o => o.ListName));
        Assert.Equal(CompanyPerkCatalog.GetPerkListCatalogEntries().Count, options.Count);
        Assert.All(options, o => Assert.StartsWith($"{o.ListName} (", o.DisplayName));
    }

    [Fact]
    public void ForList_EveryCatalogList_ReturnsATable()
    {
        Assert.All(PerkTable.Options(), option =>
        {
            var table = PerkTable.ForList(option.ListName);
            Assert.NotNull(table);
            Assert.NotEmpty(table.Tracks);
            Assert.All(table.Tracks, track => Assert.Equal(table.TierCount, track.TierTexts.Count));
        });
    }

    [Fact]
    public void ForList_UnknownList_ReturnsNull() => Assert.Null(PerkTable.ForList("Nonsense"));

    [Fact]
    public void Build_TracksOfFewTiers_LaysOutByTierPaddedToFive()
    {
        var first = Node("x-track-2-tier-1", "Alpha", 1);
        first.AddChild(Node("x-track-2-tier-2", "Beta, Gamma", 2, "x-track-2-tier-1"));
        var other = Node("x-track-1-tier-1", "Delta", 1);

        var table = PerkTable.Build(List(first, other));
        Assert.NotNull(table);

        Assert.Equal(PerkTable.MinimumTierCount, table.TierCount);
        Assert.Equal([1, 2], table.Tracks.Select(t => t.TrackNumber));
        Assert.Equal("1", table.Tracks[0].RollLabel);
        Assert.Equal(["Alpha", $"Beta{Environment.NewLine}Gamma", "", "", ""], table.Tracks[1].TierTexts);
    }

    [Fact]
    public void Build_ParentAndChildOnOneTrack_MakesOneLink()
    {
        var parent = Node("x-track-1-tier-1", "A", 1);
        parent.AddChild(Node("x-track-1-tier-3", "B", 3, "x-track-1-tier-1"));
        parent.AddChild(Node("x-track-2-tier-2", "Other track", 2, "x-track-1-tier-1"));

        var table = PerkTable.Build(List(parent));
        Assert.NotNull(table);

        Assert.Equal([new PerkTableLink(1, 1, 3)], table.Links);
    }

    [Fact]
    public void Build_NodesWithoutTrackIds_ReturnsNull()
    {
        Assert.Null(PerkTable.Build(List(Node("loose", "A", 1))));
    }

    [Theory]
    [InlineData("Plain", "Plain")]
    [InlineData("Trailing,", "Trailing")]
    [InlineData(" ", "")]
    public void PerkText_Name_SplitsOnlyRealLists(string name, string expected) =>
        Assert.Equal(expected, PerkTable.PerkText(name));

    [Fact]
    public void RollLabel_Ranges_JoinsThem()
    {
        Assert.Equal("Always", PerkTable.RollLabel([]));
        Assert.Equal("1-5/11", PerkTable.RollLabel([new() { Min = 1, Max = 5 }, new() { Min = 11, Max = 11 }]));
    }

    [Fact]
    public void PerksTablesViewModel_SelectionChanged_ShowsTheSelectedTable()
    {
        var viewModel = new PerksTablesViewModel(Substitute.For<INavigationService>());

        Assert.Equal("Initiative", viewModel.SelectedPerkTableOption?.ListName);
        Assert.NotNull(viewModel.Table);

        viewModel.SelectedPerkTableOption = viewModel.PerkTableOptions[1];
        var expected = PerkTable.ForList("Cool");
        Assert.NotNull(expected);
        Assert.NotNull(viewModel.Table);
        Assert.Equal(expected.Tracks.Count, viewModel.Table.Tracks.Count);

        viewModel.SelectedPerkTableOption = null;
        Assert.Null(viewModel.Table);
    }
}
