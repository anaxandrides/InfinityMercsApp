using InfinityMercsApp.Domain.Models.Army.Traits;
using InfinityMercsApp.Domain.Models.Filtering;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.UnitTests.Support;
using InfinityMercsApp.ViewModels.Company.Selection;
using InfinityMercsApp.ViewModels.Company.Selection.Rules;
using InfinityMercsApp.ViewModels.Components.UnitFilter;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Selection;

public sealed class ProxyPackCompanySelectionTests
{
    private readonly SelectionTestKit _kit = new();

    private async Task<CompanySelectionViewModel> OpenWithFactionAsync()
    {
        var viewModel = await SelectionTestKit.OpenAsync(new CompanySelectionViewModel(_kit.Services, new ProxyPackCompanyRules(ArmySourceSelectionMode.VanillaFactions)));
        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Slots.Factions.First(faction => faction.Id == Army101Data.PanOceania));
        return viewModel;
    }

    [Fact]
    public async Task InitializeAsync_ProxyPack_OffersEveryFaction()
    {
        var viewModel = await SelectionTestKit.OpenAsync(new CompanySelectionViewModel(_kit.Services, new ProxyPackCompanyRules(ArmySourceSelectionMode.VanillaFactions)));

        Assert.Equal("Proxy Pack", viewModel.Rules.CompanyTypeLabel);
        Assert.Equal([Army101Data.PanOceania, SelectionTestKit.Sectorial], viewModel.Slots.Factions.Select(faction => faction.Id).Order());
    }

    [Fact]
    public async Task SelectFactionCommand_ProxyPack_ShowsOnlyRegularTroopers()
    {
        var viewModel = await OpenWithFactionAsync();

        var visible = viewModel.UnitList.Units.Where(unit => unit.IsVisible).ToList();
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
        var request = Assert.IsType<UnitFilterViewModel>(viewModel.UnitList.Filter).Request;
        Assert.False(request.LieutenantOnlyUnitsEnabled);
        Assert.False(request.TeamsViewEnabled);

        viewModel.ApplyUnitFilterCommand.Execute(new UnitFilterCriteria { LieutenantOnlyUnits = true, TeamsView = true });

        Assert.False(viewModel.UnitList.LieutenantOnlyUnits);
        Assert.False(viewModel.UnitList.TeamsView);
        Assert.False(viewModel.UnitList.ActiveFilter.LieutenantOnlyUnits);
    }

    [Fact]
    public async Task AddProfileToMercsCompanyCommand_FirstTrooper_IsTheLoneWolfCaptain()
    {
        var viewModel = await OpenWithFactionAsync();
        var trooper = viewModel.Preview.Profiles.First(profile => profile is { IsVisible: true, IsLieutenant: false });

        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(trooper);

        var wolf = Assert.Single(viewModel.Roster.Entries);
        Assert.True(wolf.IsLieutenant);
        Assert.True(wolf.IsLoneWolf);
        Assert.Contains("Transfer Consciousness", wolf.SavedSkills);
        Assert.Contains("Tactical Awareness", wolf.SavedSkills);
        Assert.True(viewModel.IsCompanyValid);
    }

    [Fact]
    public async Task AddProfileToMercsCompanyCommand_SecondTrooper_AddsNothingAndBlocksTheRest()
    {
        var viewModel = await OpenWithFactionAsync();
        var profile = viewModel.Preview.Profiles.First(candidate => candidate.IsVisible);
        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(profile);

        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(viewModel.Preview.Profiles.First(candidate => candidate.IsVisible));

        Assert.Single(viewModel.Roster.Entries);
        Assert.All(viewModel.Preview.Profiles, candidate => Assert.True(candidate.IsLieutenantBlocked));
    }
}
