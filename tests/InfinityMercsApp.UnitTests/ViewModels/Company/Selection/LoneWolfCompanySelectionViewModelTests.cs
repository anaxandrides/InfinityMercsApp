using InfinityMercsApp.Domain.Models.Army.Traits;
using InfinityMercsApp.Domain.Models.Filtering;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.UnitTests.Support;
using InfinityMercsApp.ViewModels.Company.Selection;
using InfinityMercsApp.Views.Controls.UnitFilter;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Selection;

public sealed class LoneWolfCompanySelectionViewModelTests
{
    private readonly SelectionTestKit _kit = new();

    private async Task<LoneWolfCompanySelectionViewModel> OpenWithFactionAsync()
    {
        var viewModel = await SelectionTestKit.OpenAsync(new LoneWolfCompanySelectionViewModel(_kit.Services, ArmySourceSelectionMode.VanillaFactions));
        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Factions.First(faction => faction.Id == Army101Data.PanOceania));
        return viewModel;
    }

    [Fact]
    public async Task EveryFactionIsOfferedVanillaOrSectorial()
    {
        var viewModel = await SelectionTestKit.OpenAsync(new LoneWolfCompanySelectionViewModel(_kit.Services, ArmySourceSelectionMode.VanillaFactions));

        Assert.Equal("Lone Wolf Company", viewModel.CompanyTypeLabel);
        Assert.Equal([Army101Data.PanOceania, SelectionTestKit.Sectorial], viewModel.Factions.Select(faction => faction.Id).Order());
    }

    [Fact]
    public async Task OnlyRegularTroopersShow()
    {
        var viewModel = await OpenWithFactionAsync();

        var visible = viewModel.Units.Where(unit => unit.IsVisible).ToList();
        Assert.NotEmpty(visible);
        Assert.All(visible, unit =>
        {
            var groups = _kit.ArmyData.GetUnit(unit.SourceFactionId, unit.Id)?.ProfileGroups;
            Assert.NotNull(groups);
            Assert.True(UnitTraits.ReadOrderTraits(groups).HasRegular);
        });
    }

    [Fact]
    public async Task TheFilterHasNoLieutenantOrFireteamOptions()
    {
        var viewModel = await OpenWithFactionAsync();

        viewModel.OpenUnitFilterCommand.Execute(null);
        var request = Assert.IsType<UnitFilterRequest>(viewModel.UnitFilterRequest);
        Assert.False(request.LieutenantOnlyUnitsEnabled);
        Assert.False(request.TeamsViewEnabled);

        viewModel.ApplyUnitFilterCommand.Execute(new UnitFilterCriteria { LieutenantOnlyUnits = true, TeamsView = true });

        Assert.False(viewModel.LieutenantOnlyUnits);
        Assert.False(viewModel.TeamsView);
        Assert.False(viewModel.ActiveUnitFilter.LieutenantOnlyUnits);
    }
}
