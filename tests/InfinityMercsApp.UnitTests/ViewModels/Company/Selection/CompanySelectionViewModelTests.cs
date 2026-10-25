using InfinityMercsApp.Domain.Models.Filtering;
using InfinityMercsApp.Presentation.Company.Selection;
using InfinityMercsApp.Presentation.Layouts;
using InfinityMercsApp.Services.CompanySelection.Captain;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.UnitTests.Support;
using InfinityMercsApp.ViewModels.Company.Selection;
using InfinityMercsApp.ViewModels.Company.Selection.Rules;
using InfinityMercsApp.ViewModels.Components.UnitFilter;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Selection;

public sealed class CompanySelectionViewModelTests
{
    private readonly SelectionTestKit _kit = new();

    private Task<CompanySelectionViewModel> OpenAsync(ArmySourceSelectionMode mode = ArmySourceSelectionMode.VanillaFactions) =>
        SelectionTestKit.OpenAsync(new CompanySelectionViewModel(_kit.Services, new StandardCompanyRules(mode)));

    private async Task<CompanySelectionViewModel> OpenWithFactionAsync()
    {
        var viewModel = await OpenAsync();
        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Slots.Factions[0]);
        return viewModel;
    }

    // Shows lieutenants only and picks the first; returns its lieutenant profile.
    private static async Task<ViewerProfileItem> SelectLieutenantAsync(CompanySelectionViewModel viewModel)
    {
        viewModel.ApplyUnitFilterCommand.Execute(new UnitFilterCriteria { LieutenantOnlyUnits = true });
        var lieutenant = viewModel.UnitList.Units.First(unit => unit.IsVisible);
        await viewModel.SelectUnitFromListCommand.ExecuteAsync(lieutenant);
        var profile = viewModel.Preview.Profiles.FirstOrDefault(candidate => candidate.IsLieutenant && candidate.IsVisible);
        Assert.NotNull(profile);
        return profile;
    }

    [Fact]
    public async Task InitializeAsync_Vanilla_ListsVanillaFactionsInOneSlot()
    {
        var viewModel = await OpenAsync();

        Assert.Equal("Choose your faction:", viewModel.Heading);
        Assert.Equal("Standard Company - Vanilla", viewModel.Rules.CompanyTypeLabel);
        Assert.False(viewModel.Slots.HasRightSlot);
        Assert.Equal([Army101Data.PanOceania], viewModel.Slots.Factions.Select(faction => faction.Id));
        Assert.True(viewModel.ShowFactionStrip);
        Assert.Equal(CompanyUnitDetail.NoUnitHeading, viewModel.Preview.Detail.UnitNameHeading);
    }

    [Fact]
    public async Task InitializeAsync_Sectorials_HasTwoSlots()
    {
        var viewModel = await OpenAsync(ArmySourceSelectionMode.Sectorials);

        Assert.Equal("Choose your sectorials", viewModel.Heading);
        Assert.Equal("Standard Company - Sectorial", viewModel.Rules.CompanyTypeLabel);
        Assert.True(viewModel.Slots.HasRightSlot);
        Assert.Equal([SelectionTestKit.Sectorial], viewModel.Slots.Factions.Select(faction => faction.Id));

        viewModel.SelectSlotCommand.Execute(1);
        Assert.Equal(1, viewModel.Slots.ActiveIndex);
    }

    [Fact]
    public async Task SelectFactionCommand_Faction_LoadsItsArmyFirst()
    {
        var viewModel = await OpenWithFactionAsync();

        await _kit.ArmyLoader.Received().EnsureAsync(
            Arg.Is<IReadOnlyCollection<int>>(ids => ids.Contains(viewModel.Slots.Factions[0].Id)), Arg.Any<CancellationToken>());
        await _kit.ArmyLoader.DidNotReceive().EnsureEveryArmyAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SelectFactionCommand_Faction_LoadsItsUnitsAndShowsTheFirst()
    {
        var viewModel = await OpenWithFactionAsync();

        Assert.Equal("PanOceania", viewModel.Slots.Left?.Name);
        Assert.Equal("SVGCache/factions/101.svg", viewModel.Slots.LeftLogoSource);
        Assert.True(viewModel.Slots.Factions[0].IsSelected);
        Assert.False(viewModel.ShowFactionStrip);
        Assert.NotEmpty(viewModel.UnitList.Units);
        Assert.NotEmpty(viewModel.UnitList.TeamEntries);
        Assert.False(viewModel.UnitList.TeamsView);

        var first = viewModel.UnitList.Units.First(unit => unit.IsVisible);
        Assert.Same(first, viewModel.Preview.SelectedUnit);
        Assert.True(first.IsSelected);
        Assert.Equal(first.Name, viewModel.Preview.Detail.UnitNameHeading);
        Assert.NotEqual("-", viewModel.Preview.Detail.Stats.Mov);
        Assert.NotEmpty(viewModel.Preview.Profiles);
        Assert.EndsWith("configurations loaded.", viewModel.Preview.ProfilesStatus);
    }

    [Fact]
    public async Task SelectUnitFromListCommand_AnotherUnit_MovesTheSelection()
    {
        var viewModel = await OpenWithFactionAsync();
        var first = viewModel.Preview.SelectedUnit;
        var other = viewModel.UnitList.Units.Where(unit => unit.IsVisible).Skip(1).First();

        await viewModel.SelectUnitFromListCommand.ExecuteAsync(other);

        Assert.Same(other, viewModel.Preview.SelectedUnit);
        Assert.False(first?.IsSelected);
        Assert.Equal(other.Name, viewModel.Preview.Detail.UnitNameHeading);
    }

    [Fact]
    public async Task ApplyUnitFilterCommand_LieutenantsOnly_ShowsOnlyUnitsThatCanLead()
    {
        var viewModel = await OpenWithFactionAsync();
        var visibleBefore = viewModel.UnitList.Units.Count(unit => unit.IsVisible);

        viewModel.ApplyUnitFilterCommand.Execute(new UnitFilterCriteria { LieutenantOnlyUnits = true });

        Assert.True(viewModel.UnitList.LieutenantOnlyUnits);
        Assert.True(viewModel.UnitList.IsFilterActive);
        Assert.InRange(viewModel.UnitList.Units.Count(unit => unit.IsVisible), 1, visibleBefore - 1);
    }

    [Fact]
    public async Task UnitFilterApplyCommand_Criteria_FiltersAndCloses()
    {
        var viewModel = await OpenWithFactionAsync();
        viewModel.OpenUnitFilterCommand.Execute(null);
        var popup = Assert.IsType<UnitFilterViewModel>(viewModel.UnitList.Filter);

        popup.SelectedLieutenantOnlyUnits = true;
        popup.ApplyCommand.Execute(null);

        Assert.False(viewModel.UnitList.IsFilterOpen);
        Assert.True(viewModel.UnitList.LieutenantOnlyUnits);
        Assert.True(viewModel.UnitList.IsFilterActive);
    }

    [Fact]
    public async Task OpenUnitFilterCommand_CriteriaApplied_OpensWithThem()
    {
        var viewModel = await OpenWithFactionAsync();

        viewModel.OpenUnitFilterCommand.Execute(null);

        var request = Assert.IsType<UnitFilterViewModel>(viewModel.UnitList.Filter).Request;
        Assert.True(viewModel.UnitList.IsFilterOpen);
        Assert.True(request.TeamsViewEnabled);
        Assert.True(request.LieutenantOnlyUnitsEnabled);
        Assert.NotEmpty(request.Options.Classification);

        viewModel.CloseUnitFilterCommand.Execute(null);
        Assert.False(viewModel.UnitList.IsFilterOpen);
    }

    [Fact]
    public async Task AddProfileToMercsCompanyCommand_Lieutenant_MakesTheCompanyValid()
    {
        var viewModel = await OpenWithFactionAsync();
        var profile = await SelectLieutenantAsync(viewModel);

        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(profile);

        var entry = Assert.Single(viewModel.Roster.Entries);
        Assert.True(entry.IsLieutenant);
        Assert.Equal(entry.CostValue.ToString(), viewModel.SeasonPointsCapText);
        Assert.True(viewModel.IsCompanyValid);
        Assert.True(viewModel.StartCompanyCommand.CanExecute(null));
        Assert.False(viewModel.HasStartBlockedText);
        Assert.True(profile.IsLieutenantBlocked);
    }

    [Fact]
    public async Task AddProfileToMercsCompanyCommand_NoLieutenant_LeavesTheCompanyInvalid()
    {
        var viewModel = await OpenWithFactionAsync();
        var trooper = viewModel.Preview.Profiles.First(profile => !profile.IsLieutenant && profile.IsVisible);

        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(trooper);

        Assert.Single(viewModel.Roster.Entries);
        Assert.False(viewModel.IsCompanyValid);
        Assert.False(viewModel.StartCompanyCommand.CanExecute(null));
        Assert.Equal("Choose a Lieutenant to start the company.", viewModel.StartBlockedText);
    }

    [Fact]
    public async Task AddProfileToMercsCompanyCommand_BlockedProfile_AddsNothing()
    {
        var viewModel = await OpenWithFactionAsync();
        var profile = await SelectLieutenantAsync(viewModel);
        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(profile);

        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(profile);

        Assert.Single(viewModel.Roster.Entries);
    }

    [Fact]
    public async Task SelectedStartSeasonPoints_BelowTheCompanysCost_InvalidatesIt()
    {
        var viewModel = await OpenWithFactionAsync();
        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(await SelectLieutenantAsync(viewModel));

        viewModel.SelectedStartSeasonPoints = "1";

        Assert.False(viewModel.IsCompanyValid);
        Assert.Equal("The company is over its points limit.", viewModel.StartBlockedText);
    }

    [Fact]
    public async Task RemoveMercsCompanyEntryCommand_Trooper_RecountsThePoints()
    {
        var viewModel = await OpenWithFactionAsync();
        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(await SelectLieutenantAsync(viewModel));

        viewModel.RemoveMercsCompanyEntryCommand.Execute(viewModel.Roster.Entries[0]);

        Assert.Empty(viewModel.Roster.Entries);
        Assert.Equal("0", viewModel.SeasonPointsCapText);
        Assert.False(viewModel.IsCompanyValid);
    }

    [Fact]
    public async Task SelectMercsCompanyEntryCommand_Trooper_ShowsItsUnit()
    {
        var viewModel = await OpenWithFactionAsync();
        var profile = await SelectLieutenantAsync(viewModel);
        var lieutenantUnit = viewModel.Preview.SelectedUnit;
        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(profile);
        await viewModel.SelectUnitFromListCommand.ExecuteAsync(viewModel.UnitList.Units.First(unit => unit.IsVisible && !ReferenceEquals(unit, lieutenantUnit)));

        await viewModel.SelectMercsCompanyEntryCommand.ExecuteAsync(viewModel.Roster.Entries[0]);

        Assert.Same(lieutenantUnit, viewModel.Preview.SelectedUnit);
    }

    [Fact]
    public async Task SelectFactionCommand_FactionChanged_EmptiesTheCompany()
    {
        var viewModel = await OpenAsync(ArmySourceSelectionMode.Sectorials);
        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Slots.Factions[0]);
        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(await SelectLieutenantAsync(viewModel));
        var other = new InfinityMercsApp.ViewModels.Company.Selection.Items.ArmyFactionSelectionItem
        {
            Id = Army101Data.PanOceania,
            ParentId = Army101Data.PanOceania,
            Name = "PanOceania"
        };
        viewModel.SelectSlotCommand.Execute(0);

        await viewModel.SelectFactionCommand.ExecuteAsync(other);

        Assert.Empty(viewModel.Roster.Entries);
    }

    [Fact]
    public async Task StartCompanyCommand_NoName_FlagsTheName()
    {
        var viewModel = await OpenWithFactionAsync();
        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(await SelectLieutenantAsync(viewModel));

        // The name box starts empty and shows its placeholder.
        Assert.Equal(string.Empty, viewModel.Roster.CompanyName);
        await viewModel.StartCompanyCommand.ExecuteAsync(null);

        Assert.True(viewModel.Roster.ShowCompanyNameValidationError);
        await _kit.CaptainDialog.DidNotReceive().ShowAsync(Arg.Any<CaptainUpgradePopupContext>());

        viewModel.Roster.CompanyName = "Blue Lions";
        Assert.False(viewModel.Roster.ShowCompanyNameValidationError);
    }

    [Fact]
    public async Task StartCompanyCommand_CaptainPopupCancelled_StaysOnTheScreen()
    {
        _kit.CaptainDialog.ShowAsync(Arg.Any<CaptainUpgradePopupContext>()).Returns((SavedImprovedCaptainStats?)null);
        var viewModel = await OpenWithFactionAsync();
        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(await SelectLieutenantAsync(viewModel));
        viewModel.Roster.CompanyName = "Blue Lions";

        await viewModel.StartCompanyCommand.ExecuteAsync(null);

        await _kit.CaptainDialog.Received(1).ShowAsync(Arg.Any<CaptainUpgradePopupContext>());
        await _kit.Navigation.DidNotReceive().NavigateToAsync(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>());
    }

    [Fact]
    public async Task ShowPaneCommand_Compact_ShowsOneWorkspaceAtATime()
    {
        var viewModel = await OpenWithFactionAsync();
        viewModel.ApplyLayoutCommand.Execute(AdaptiveLayoutMode.Compact);

        Assert.True(viewModel.ShowsUnitsPane);
        Assert.False(viewModel.ShowsRightPane);
        Assert.True(viewModel.IsUnitsPaneActive);

        viewModel.ShowPaneCommand.Execute(CompanySelectionPane.Company);

        Assert.False(viewModel.ShowsUnitsPane);
        Assert.True(viewModel.ShowsCompanyPane);
        Assert.False(viewModel.ShowsDetailPane);
        Assert.Equal("0,0,*", viewModel.RightPaneRows);

        viewModel.ApplyLayoutCommand.Execute(AdaptiveLayoutMode.Medium);
        Assert.True(viewModel.ShowsUnitsPane && viewModel.ShowsDetailPane && viewModel.ShowsCompanyPane);
        Assert.Equal("*,3,*", viewModel.RightPaneRows);
    }

    [Fact]
    public async Task RightPaneRows_NoUnitSelected_GiveTheCompanyAllTheHeight()
    {
        var viewModel = await OpenAsync();
        viewModel.ApplyLayoutCommand.Execute(AdaptiveLayoutMode.Medium);

        Assert.Null(viewModel.Preview.SelectedUnit);
        Assert.False(viewModel.ShowsDetailPane);
        Assert.Equal("0,0,*", viewModel.RightPaneRows);

        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Slots.Factions[0]);

        Assert.NotNull(viewModel.Preview.SelectedUnit);
        Assert.True(viewModel.ShowsDetailPane);
        Assert.Equal("*,3,*", viewModel.RightPaneRows);
    }

    [Fact]
    public async Task SelectSlotCommand_TappedTwice_OpensThenClosesThePicker()
    {
        var viewModel = await OpenWithFactionAsync();
        viewModel.CloseFactionStripCommand.Execute(null);

        viewModel.SelectSlotCommand.Execute(0);

        Assert.True(viewModel.ShowFactionStrip);
        Assert.Equal(0, viewModel.Slots.ActiveIndex);

        viewModel.SelectSlotCommand.Execute(0);

        Assert.False(viewModel.ShowFactionStrip);
    }

    [Fact]
    public async Task IsFactionOverlayOpen_Compact_OnlyThen()
    {
        var viewModel = await OpenAsync();
        Assert.False(viewModel.IsFactionOverlayOpen);

        viewModel.ApplyLayoutCommand.Execute(AdaptiveLayoutMode.Compact);
        Assert.True(viewModel.IsFactionOverlayOpen);

        viewModel.CloseFactionStripCommand.Execute(null);
        Assert.False(viewModel.IsFactionOverlayOpen);

        viewModel.OpenFactionStripCommand.Execute(null);
        Assert.True(viewModel.ShowFactionStrip);
    }
}
