using InfinityMercsApp.Services.CompanySelection.Models.Selection;
using InfinityMercsApp.UnitTests.Support;
using InfinityMercsApp.ViewModels.Company.Selection;
using AirborneGen = InfinityMercsApp.Infrastructure.Providers.CompanyFactions.AirborneCompanyFactionGenerator;
using InspiringGen = InfinityMercsApp.Infrastructure.Providers.CompanyFactions.InspiringCompanyFactionGenerator;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Selection;

public sealed class GeneratedFactionCompanySelectionViewModelTests
{
    private readonly SelectionTestKit _kit = new();

    public GeneratedFactionCompanySelectionViewModelTests()
    {
        _kit.AddFaction(AirborneGen.AirborneCompanyFactionId);
        _kit.AddFaction(InspiringGen.InspiringCompanyFactionId);
    }

    private async Task<TViewModel> OpenWithFactionAsync<TViewModel>(TViewModel viewModel)
        where TViewModel : GeneratedFactionCompanySelectionViewModel
    {
        await SelectionTestKit.OpenAsync(viewModel);
        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Factions[0]);
        return viewModel;
    }

    [Fact]
    public async Task TheGeneratedFactionFillsTheRightSlotAndTheLeftShowsLieutenants()
    {
        var viewModel = await SelectionTestKit.OpenAsync(new AirborneCompanySelectionViewModel(_kit.Services));

        Assert.Equal("Airborne Company", viewModel.CompanyTypeLabel);
        Assert.Equal(AirborneGen.AirborneCompanyFactionId, viewModel.RightSlotFaction?.Id);
        Assert.Equal(AirborneCompanySelectionViewModel.LogoPath, viewModel.RightSlotLogoSource);
        Assert.True(viewModel.LieutenantOnlyUnits);
        Assert.True(viewModel.IsUnitFilterActive);
        Assert.Equal([SelectionTestKit.Sectorial], viewModel.Factions.Select(faction => faction.Id));
    }

    [Fact]
    public async Task OnlyTheLeftSlotsUnitsAreListed()
    {
        var viewModel = await OpenWithFactionAsync(new AirborneCompanySelectionViewModel(_kit.Services));

        Assert.Equal(0, viewModel.ActiveSlotIndex);
        Assert.False(viewModel.ShowFactionStrip);
        Assert.All(viewModel.Units, unit => Assert.Equal(SelectionTestKit.Sectorial, unit.SourceFactionId));
        Assert.Contains(viewModel.Units, unit => !unit.IsVisible);
    }

    [Fact]
    public async Task TheAirborneCaptainJumpsInAndTheGeneratedSlotOpens()
    {
        var viewModel = await OpenWithFactionAsync(new AirborneCompanySelectionViewModel(_kit.Services));
        await viewModel.SelectUnitFromListCommand.ExecuteAsync(viewModel.Units.First(unit => unit.IsVisible));
        var lieutenant = viewModel.Profiles.First(profile => profile.IsLieutenant && profile.IsVisible);

        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(lieutenant);

        var captain = Assert.Single(viewModel.MercsCompanyEntries);
        Assert.Contains(AirborneCompanySelectionViewModel.ParachutistSkill, captain.SavedSkills);
        Assert.Contains(AirborneCompanySelectionViewModel.NetworkSupportSkill, captain.SavedSkills);
        Assert.Equal(1, viewModel.ActiveSlotIndex);
        Assert.False(viewModel.LieutenantOnlyUnits);
        await Task.Yield();
        Assert.All(viewModel.Units, unit => Assert.Equal(AirborneGen.AirborneCompanyFactionId, unit.SourceFactionId));
    }

    [Fact]
    public async Task TheLeftSlotGivesOnlyOneCaptain()
    {
        var viewModel = await OpenWithFactionAsync(new InspiringCompanySelectionViewModel(_kit.Services));
        var unit = viewModel.Units.First(candidate => candidate.IsVisible);
        await viewModel.SelectUnitFromListCommand.ExecuteAsync(unit);
        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(viewModel.Profiles.First(profile => profile.IsLieutenant && profile.IsVisible));

        viewModel.SelectSlotCommand.Execute(0);
        await Task.Yield();
        var again = viewModel.Units.FirstOrDefault(candidate => candidate.Id == unit.Id);

        Assert.True(again is null || !again.IsVisible);
        Assert.Contains(InspiringCompanySelectionViewModel.InspiringLeadershipSkill, viewModel.MercsCompanyEntries[0].SavedSkills);
    }

    [Fact]
    public async Task TheInspiringCompanyUsesItsOwnFaction()
    {
        var viewModel = await SelectionTestKit.OpenAsync(new InspiringCompanySelectionViewModel(_kit.Services));

        Assert.Equal("Inspiring Leader", viewModel.CompanyTypeLabel);
        Assert.Equal(InspiringGen.InspiringCompanyFactionId, viewModel.RightSlotFaction?.Id);
    }

    [Fact]
    public async Task TheFilterHasNoFireteamView()
    {
        var viewModel = await OpenWithFactionAsync(new AirborneCompanySelectionViewModel(_kit.Services));

        viewModel.OpenUnitFilterCommand.Execute(null);

        var request = Assert.IsType<InfinityMercsApp.Views.Controls.UnitFilter.UnitFilterRequest>(viewModel.UnitFilterRequest);
        Assert.False(request.TeamsViewEnabled);
        Assert.True(request.LieutenantOnlyUnits);
    }
}
