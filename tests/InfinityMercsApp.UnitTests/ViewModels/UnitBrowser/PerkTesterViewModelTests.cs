using InfinityMercsApp.Domain.Models.Perks.Catalog;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.UnitTests.Support;
using InfinityMercsApp.ViewModels.UnitBrowser;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.UnitBrowser;

public sealed class PerkTesterViewModelTests
{
    [Fact]
    public async Task ChoosingAUnitSelectsItsFirstConfiguration()
    {
        var data = Army101Data.Create();
        var viewModel = new PerkTesterViewModel(Substitute.For<INavigationService>(), data, new UnitDetailViewModel(data));
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Factions[0]);

        viewModel.SelectUnitCommand.Execute(viewModel.Units.First(u => u.Name == "SWISS GUARD"));

        Assert.True(viewModel.Detail.Profiles[0].IsSelected);
        Assert.Single(viewModel.Detail.Profiles, p => p.IsSelected);

        viewModel.SelectProfileCommand.Execute(viewModel.Detail.Profiles[^1]);
        Assert.True(viewModel.Detail.Profiles[^1].IsSelected);
        Assert.False(viewModel.Detail.Profiles[0].IsSelected);
    }

    [Fact]
    public void DescribesAPerkNode()
    {
        var nodeId = CompanyPerkCatalog.GetPerkNodeLists().First().Roots.First().Id;

        var perk = PerkTesterViewModel.Describe(nodeId);

        Assert.NotNull(perk);
        Assert.Contains("| Track ", perk.Meta);
        Assert.StartsWith("Gives: ", perk.Granted);
        Assert.Null(PerkTesterViewModel.Describe("no-such-perk"));
        Assert.Equal("Gives: -", PerkTesterViewModel.GrantedText(null));
    }
}
