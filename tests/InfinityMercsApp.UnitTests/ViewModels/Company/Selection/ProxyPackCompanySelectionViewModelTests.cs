using InfinityMercsApp.Domain.Models.Army.Traits;
using InfinityMercsApp.Domain.Models.Filtering;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.UnitTests.Support;
using InfinityMercsApp.ViewModels.Company.Selection;
using InfinityMercsApp.ViewModels.Components.UnitFilter;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Selection;

public sealed class ProxyPackCompanySelectionViewModelTests
{
    private readonly SelectionTestKit _kit = new();

    private async Task<ProxyPackCompanySelectionViewModel> OpenWithFactionAsync()
    {
        var viewModel = await SelectionTestKit.OpenAsync(new ProxyPackCompanySelectionViewModel(_kit.Services, ArmySourceSelectionMode.VanillaFactions));
        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Factions.First(faction => faction.Id == Army101Data.PanOceania));
        return viewModel;
    }

    [Fact]
    public async Task InitializeAsync_ProxyPack_OffersEveryFaction()
    {
        var viewModel = await SelectionTestKit.OpenAsync(new ProxyPackCompanySelectionViewModel(_kit.Services, ArmySourceSelectionMode.VanillaFactions));

        Assert.Equal("Proxy Pack", viewModel.CompanyTypeLabel);
        Assert.Equal([Army101Data.PanOceania, SelectionTestKit.Sectorial], viewModel.Factions.Select(faction => faction.Id).Order());
    }

    [Fact]
    public async Task SelectFactionCommand_ProxyPack_ShowsOnlyRegularTroopers()
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
    public async Task OpenUnitFilterCommand_ProxyPack_HasNoLieutenantOrFireteamOptions()
    {
        var viewModel = await OpenWithFactionAsync();

        viewModel.OpenUnitFilterCommand.Execute(null);
        var request = Assert.IsType<UnitFilterViewModel>(viewModel.UnitFilter).Request;
        Assert.False(request.LieutenantOnlyUnitsEnabled);
        Assert.False(request.TeamsViewEnabled);

        viewModel.ApplyUnitFilterCommand.Execute(new UnitFilterCriteria { LieutenantOnlyUnits = true, TeamsView = true });

        Assert.False(viewModel.LieutenantOnlyUnits);
        Assert.False(viewModel.TeamsView);
        Assert.False(viewModel.ActiveUnitFilter.LieutenantOnlyUnits);
    }

    [Fact]
    public async Task AddProfileToMercsCompanyCommand_FirstTrooper_IsTheLoneWolfCaptain()
    {
        var viewModel = await OpenWithFactionAsync();
        var trooper = viewModel.Profiles.First(profile => profile is { IsVisible: true, IsLieutenant: false });

        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(trooper);

        var wolf = Assert.Single(viewModel.MercsCompanyEntries);
        Assert.True(wolf.IsLieutenant);
        Assert.Contains("Transfer Consciousness", wolf.SavedSkills);
        Assert.Contains("Tactical Awareness", wolf.SavedSkills);
        Assert.True(viewModel.IsCompanyValid);
    }

    [Fact]
    public async Task AddProfileToMercsCompanyCommand_SecondTrooper_AddsNothingAndBlocksTheRest()
    {
        var viewModel = await OpenWithFactionAsync();
        var profile = viewModel.Profiles.First(candidate => candidate.IsVisible);
        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(profile);

        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(viewModel.Profiles.First(candidate => candidate.IsVisible));

        Assert.Single(viewModel.MercsCompanyEntries);
        Assert.All(viewModel.Profiles, candidate => Assert.True(candidate.IsLieutenantBlocked));
    }
}
