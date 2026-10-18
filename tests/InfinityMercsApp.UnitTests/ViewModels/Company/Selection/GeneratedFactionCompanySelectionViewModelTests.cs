using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.CompanySelection.Recruitment;
using InfinityMercsApp.ViewModels.Company.Selection;
using InfinityMercsApp.ViewModels.Components.UnitFilter;
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
    public async Task InitializeAsync_Airborne_FillsRightSlotAndShowsLieutenants()
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
    public async Task SelectFactionCommand_LeftSlot_ListsOnlyItsUnits()
    {
        var viewModel = await OpenWithFactionAsync(new AirborneCompanySelectionViewModel(_kit.Services));

        Assert.Equal(0, viewModel.ActiveSlotIndex);
        Assert.False(viewModel.ShowFactionStrip);
        Assert.All(viewModel.Units, unit => Assert.Equal(SelectionTestKit.Sectorial, unit.SourceFactionId));
        Assert.Contains(viewModel.Units, unit => !unit.IsVisible);
    }

    [Fact]
    public async Task AddProfileToMercsCompanyCommand_AirborneCaptain_OpensTheGeneratedSlot()
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
    public async Task AddProfileToMercsCompanyCommand_SecondLeftSlotTrooper_IsBlocked()
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
    public async Task InitializeAsync_Inspiring_UsesItsOwnFaction()
    {
        var viewModel = await SelectionTestKit.OpenAsync(new InspiringCompanySelectionViewModel(_kit.Services));

        Assert.Equal("Inspiring Leader", viewModel.CompanyTypeLabel);
        Assert.Equal(InspiringGen.InspiringCompanyFactionId, viewModel.RightSlotFaction?.Id);
    }

    [Fact]
    public async Task OpenUnitFilterCommand_GeneratedCompany_HasNoFireteamView()
    {
        var viewModel = await OpenWithFactionAsync(new AirborneCompanySelectionViewModel(_kit.Services));

        viewModel.OpenUnitFilterCommand.Execute(null);

        var request = Assert.IsType<UnitFilterViewModel>(viewModel.UnitFilter).Request;
        Assert.False(request.TeamsViewEnabled);
        Assert.True(request.LieutenantOnlyUnits);
    }

    [Fact]
    public async Task PrepareRecruitment_AirborneCompany_ListsTheGeneratedFactionsTroopers()
    {
        var company = new SavedCompanyFile
        {
            CompanyName = "Sky Team",
            CompanyType = "Airborne Company",
            SourceFactions = [new SavedCompanyFaction { FactionId = SelectionTestKit.Sectorial, FactionName = "Military Orders" }],
            Entries = [new SavedCompanyEntry { EntryIndex = 0, Name = "Captain", IsLieutenant = true, SourceFactionId = SelectionTestKit.Sectorial, SourceUnitId = 1, Cost = 20 }]
        };
        var viewModel = new AirborneCompanySelectionViewModel(_kit.Services);
        viewModel.PrepareRecruitment(new CompanyRecruitment("/records/sky.json", "/records/sky-season.json", company, Credits: 30, RetiredEntries: []));

        await SelectionTestKit.OpenAsync(viewModel);

        Assert.Equal(SelectionTestKit.Sectorial, viewModel.LeftSlotFaction?.Id);
        Assert.Equal(AirborneGen.AirborneCompanyFactionId, viewModel.RightSlotFaction?.Id);
        Assert.Equal(1, viewModel.ActiveSlotIndex);
        Assert.False(viewModel.LieutenantOnlyUnits);
        Assert.NotEmpty(viewModel.Units);
        Assert.All(viewModel.Units, unit => Assert.Equal(AirborneGen.AirborneCompanyFactionId, unit.SourceFactionId));
        Assert.Equal("30 CR LEFT", viewModel.CreditsLeftText);
    }
}
