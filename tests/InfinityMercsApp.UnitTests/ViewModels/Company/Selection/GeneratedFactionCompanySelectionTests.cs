using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.CompanySelection.Recruitment;
using InfinityMercsApp.ViewModels.Company.Selection;
using InfinityMercsApp.ViewModels.Company.Selection.Rules;
using InfinityMercsApp.ViewModels.Components.UnitFilter;
using AirborneGen = InfinityMercsApp.Infrastructure.Providers.CompanyFactions.AirborneCompanyFactionGenerator;
using InspiringGen = InfinityMercsApp.Infrastructure.Providers.CompanyFactions.InspiringCompanyFactionGenerator;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Selection;

public sealed class GeneratedFactionCompanySelectionTests
{
    private readonly SelectionTestKit _kit = new();

    public GeneratedFactionCompanySelectionTests()
    {
        _kit.AddFaction(AirborneGen.AirborneCompanyFactionId);
        _kit.AddFaction(InspiringGen.InspiringCompanyFactionId);
    }

    private static async Task<CompanySelectionViewModel> OpenWithFactionAsync(CompanySelectionViewModel viewModel)
    {
        await SelectionTestKit.OpenAsync(viewModel);
        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Slots.Factions[0]);
        return viewModel;
    }

    [Fact]
    public async Task InitializeAsync_Airborne_FillsRightSlotAndShowsLieutenants()
    {
        var viewModel = await SelectionTestKit.OpenAsync(new CompanySelectionViewModel(_kit.Services, new AirborneCompanyRules()));

        Assert.Equal("Airborne Company", viewModel.Rules.CompanyTypeLabel);
        Assert.Equal(AirborneGen.AirborneCompanyFactionId, viewModel.Slots.Right?.Id);
        Assert.Equal(AirborneCompanyRules.LogoPath, viewModel.Slots.RightLogoSource);
        Assert.True(viewModel.UnitList.LieutenantOnlyUnits);
        Assert.True(viewModel.UnitList.IsFilterActive);
        Assert.Equal([SelectionTestKit.Sectorial], viewModel.Slots.Factions.Select(faction => faction.Id));
    }

    [Fact]
    public async Task SelectFactionCommand_LeftSlot_ListsOnlyItsUnits()
    {
        var viewModel = await OpenWithFactionAsync(new CompanySelectionViewModel(_kit.Services, new AirborneCompanyRules()));

        Assert.Equal(0, viewModel.Slots.ActiveIndex);
        Assert.False(viewModel.ShowFactionStrip);
        Assert.All(viewModel.UnitList.Units, unit => Assert.Equal(SelectionTestKit.Sectorial, unit.SourceFactionId));
        Assert.Contains(viewModel.UnitList.Units, unit => !unit.IsVisible);
    }

    [Fact]
    public async Task AddProfileToMercsCompanyCommand_AirborneCaptain_OpensTheGeneratedSlot()
    {
        var viewModel = await OpenWithFactionAsync(new CompanySelectionViewModel(_kit.Services, new AirborneCompanyRules()));
        await viewModel.SelectUnitFromListCommand.ExecuteAsync(viewModel.UnitList.Units.First(unit => unit.IsVisible));
        var lieutenant = viewModel.Preview.Profiles.First(profile => profile.IsLieutenant && profile.IsVisible);

        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(lieutenant);

        var captain = Assert.Single(viewModel.Roster.Entries);
        Assert.Contains(AirborneCompanyRules.ParachutistSkill, captain.SavedSkills);
        Assert.Contains(AirborneCompanyRules.NetworkSupportSkill, captain.SavedSkills);
        Assert.Equal(1, viewModel.Slots.ActiveIndex);
        Assert.False(viewModel.UnitList.LieutenantOnlyUnits);
        await Task.Yield();
        Assert.All(viewModel.UnitList.Units, unit => Assert.Equal(AirborneGen.AirborneCompanyFactionId, unit.SourceFactionId));
    }

    [Fact]
    public async Task AddProfileToMercsCompanyCommand_SecondLeftSlotTrooper_IsBlocked()
    {
        var viewModel = await OpenWithFactionAsync(new CompanySelectionViewModel(_kit.Services, new InspiringCompanyRules()));
        var unit = viewModel.UnitList.Units.First(candidate => candidate.IsVisible);
        await viewModel.SelectUnitFromListCommand.ExecuteAsync(unit);
        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(viewModel.Preview.Profiles.First(profile => profile.IsLieutenant && profile.IsVisible));

        viewModel.SelectSlotCommand.Execute(0);
        await Task.Yield();
        var again = viewModel.UnitList.Units.FirstOrDefault(candidate => candidate.Id == unit.Id);

        Assert.True(again is null || !again.IsVisible);
        Assert.Contains(InspiringCompanyRules.InspiringLeadershipSkill, viewModel.Roster.Entries[0].SavedSkills);
    }

    [Fact]
    public async Task InitializeAsync_Inspiring_UsesItsOwnFaction()
    {
        var viewModel = await SelectionTestKit.OpenAsync(new CompanySelectionViewModel(_kit.Services, new InspiringCompanyRules()));

        Assert.Equal("Inspiring Leader", viewModel.Rules.CompanyTypeLabel);
        Assert.Equal(InspiringGen.InspiringCompanyFactionId, viewModel.Slots.Right?.Id);
    }

    [Fact]
    public async Task OpenUnitFilterCommand_GeneratedCompany_HasNoFireteamView()
    {
        var viewModel = await OpenWithFactionAsync(new CompanySelectionViewModel(_kit.Services, new AirborneCompanyRules()));

        viewModel.OpenUnitFilterCommand.Execute(null);

        var request = Assert.IsType<UnitFilterViewModel>(viewModel.UnitList.Filter).Request;
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
            Entries = [new SavedCompanyEntry { EntryIndex = 0, Name = "Captain", IsLieutenant = true, CarriesCaptainUpgrades = true, SourceFactionId = SelectionTestKit.Sectorial, SourceUnitId = 1, Cost = 20 }]
        };
        var viewModel = new CompanySelectionViewModel(_kit.Services, new AirborneCompanyRules());
        viewModel.PrepareRecruitment(new CompanyRecruitment("/records/sky.json", "/records/sky-season.json", company, Credits: 30, RetiredEntries: []));

        await SelectionTestKit.OpenAsync(viewModel);

        Assert.Equal(SelectionTestKit.Sectorial, viewModel.Slots.Left?.Id);
        Assert.Equal(AirborneGen.AirborneCompanyFactionId, viewModel.Slots.Right?.Id);
        Assert.Equal(1, viewModel.Slots.ActiveIndex);
        Assert.False(viewModel.UnitList.LieutenantOnlyUnits);
        Assert.NotEmpty(viewModel.UnitList.Units);
        Assert.All(viewModel.UnitList.Units, unit => Assert.Equal(AirborneGen.AirborneCompanyFactionId, unit.SourceFactionId));
        Assert.Equal("30 CR LEFT", viewModel.CreditsLeftText);
    }
}
