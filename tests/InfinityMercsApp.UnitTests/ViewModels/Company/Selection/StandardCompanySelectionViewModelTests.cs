using InfinityMercsApp.Domain.Models.Filtering;
using InfinityMercsApp.Presentation.Company.Selection;
using InfinityMercsApp.Presentation.Layouts;
using InfinityMercsApp.Services.CompanySelection.Captain;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.UnitTests.Support;
using InfinityMercsApp.ViewModels.Company.Selection;
using InfinityMercsApp.ViewModels.Components.UnitFilter;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Selection;

public sealed class StandardCompanySelectionViewModelTests
{
    private readonly SelectionTestKit _kit = new();

    private Task<StandardCompanySelectionViewModel> OpenAsync(ArmySourceSelectionMode mode = ArmySourceSelectionMode.VanillaFactions) =>
        SelectionTestKit.OpenAsync(new StandardCompanySelectionViewModel(_kit.Services, mode));

    private async Task<StandardCompanySelectionViewModel> OpenWithFactionAsync()
    {
        var viewModel = await OpenAsync();
        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Factions[0]);
        return viewModel;
    }

    // Shows lieutenants only and picks the first; returns its lieutenant profile.
    private static async Task<ViewerProfileItem> SelectLieutenantAsync(CompanySelectionViewModel viewModel)
    {
        viewModel.ApplyUnitFilterCommand.Execute(new UnitFilterCriteria { LieutenantOnlyUnits = true });
        var lieutenant = viewModel.Units.First(unit => unit.IsVisible);
        await viewModel.SelectUnitFromListCommand.ExecuteAsync(lieutenant);
        var profile = viewModel.Profiles.FirstOrDefault(candidate => candidate.IsLieutenant && candidate.IsVisible);
        Assert.NotNull(profile);
        return profile;
    }

    [Fact]
    public async Task AVanillaCompanyListsVanillaFactionsInOneSlot()
    {
        var viewModel = await OpenAsync();

        Assert.Equal("Choose your faction:", viewModel.Heading);
        Assert.Equal("Standard Company - Vanilla", viewModel.CompanyTypeLabel);
        Assert.False(viewModel.ShowRightSlot);
        Assert.Equal([Army101Data.PanOceania], viewModel.Factions.Select(faction => faction.Id));
        Assert.True(viewModel.ShowFactionStrip);
        Assert.Equal(CompanyUnitDetail.NoUnitHeading, viewModel.Detail.UnitNameHeading);
    }

    [Fact]
    public async Task ASectorialCompanyHasTwoSlots()
    {
        var viewModel = await OpenAsync(ArmySourceSelectionMode.Sectorials);

        Assert.Equal("Choose your sectorials", viewModel.Heading);
        Assert.Equal("Standard Company - Sectorial", viewModel.CompanyTypeLabel);
        Assert.True(viewModel.ShowRightSlot);
        Assert.Equal([SelectionTestKit.Sectorial], viewModel.Factions.Select(faction => faction.Id));

        viewModel.SelectSlotCommand.Execute(1);
        Assert.Equal(1, viewModel.ActiveSlotIndex);
    }

    [Fact]
    public async Task PickingAFactionLoadsItsUnitsAndShowsTheFirst()
    {
        var viewModel = await OpenWithFactionAsync();

        Assert.Equal("PanOceania", viewModel.LeftSlotFaction?.Name);
        Assert.Equal("SVGCache/factions/101.svg", viewModel.LeftSlotLogoSource);
        Assert.True(viewModel.Factions[0].IsSelected);
        Assert.False(viewModel.ShowFactionStrip);
        Assert.NotEmpty(viewModel.Units);
        Assert.NotEmpty(viewModel.TeamEntries);
        Assert.False(viewModel.TeamsView);

        var first = viewModel.Units.First(unit => unit.IsVisible);
        Assert.Same(first, viewModel.SelectedUnit);
        Assert.True(first.IsSelected);
        Assert.Equal(first.Name, viewModel.Detail.UnitNameHeading);
        Assert.NotEqual("-", viewModel.Detail.Stats.Mov);
        Assert.NotEmpty(viewModel.Profiles);
        Assert.EndsWith("configurations loaded.", viewModel.ProfilesStatus);
    }

    [Fact]
    public async Task SelectingAnotherUnitMovesTheSelection()
    {
        var viewModel = await OpenWithFactionAsync();
        var first = viewModel.SelectedUnit;
        var other = viewModel.Units.Where(unit => unit.IsVisible).Skip(1).First();

        await viewModel.SelectUnitFromListCommand.ExecuteAsync(other);

        Assert.Same(other, viewModel.SelectedUnit);
        Assert.False(first?.IsSelected);
        Assert.Equal(other.Name, viewModel.Detail.UnitNameHeading);
    }

    [Fact]
    public async Task TheLieutenantFilterShowsOnlyUnitsThatCanLead()
    {
        var viewModel = await OpenWithFactionAsync();
        var visibleBefore = viewModel.Units.Count(unit => unit.IsVisible);

        viewModel.ApplyUnitFilterCommand.Execute(new UnitFilterCriteria { LieutenantOnlyUnits = true });

        Assert.True(viewModel.LieutenantOnlyUnits);
        Assert.True(viewModel.IsUnitFilterActive);
        Assert.InRange(viewModel.Units.Count(unit => unit.IsVisible), 1, visibleBefore - 1);
    }

    [Fact]
    public async Task ApplyingThePopupFiltersTheUnitsAndClosesIt()
    {
        var viewModel = await OpenWithFactionAsync();
        viewModel.OpenUnitFilterCommand.Execute(null);
        var popup = Assert.IsType<UnitFilterViewModel>(viewModel.UnitFilter);

        popup.SelectedLieutenantOnlyUnits = true;
        popup.ApplyCommand.Execute(null);

        Assert.False(viewModel.IsUnitFilterOpen);
        Assert.True(viewModel.LieutenantOnlyUnits);
        Assert.True(viewModel.IsUnitFilterActive);
    }

    [Fact]
    public async Task TheFilterPopupOpensWithTheCurrentCriteria()
    {
        var viewModel = await OpenWithFactionAsync();

        viewModel.OpenUnitFilterCommand.Execute(null);

        var request = Assert.IsType<UnitFilterViewModel>(viewModel.UnitFilter).Request;
        Assert.True(viewModel.IsUnitFilterOpen);
        Assert.True(request.TeamsViewEnabled);
        Assert.True(request.LieutenantOnlyUnitsEnabled);
        Assert.NotEmpty(request.Options.Classification);

        viewModel.CloseUnitFilterCommand.Execute(null);
        Assert.False(viewModel.IsUnitFilterOpen);
    }

    [Fact]
    public async Task AddingALieutenantMakesTheCompanyValid()
    {
        var viewModel = await OpenWithFactionAsync();
        var profile = await SelectLieutenantAsync(viewModel);

        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(profile);

        var entry = Assert.Single(viewModel.MercsCompanyEntries);
        Assert.True(entry.IsLieutenant);
        Assert.Equal(entry.CostValue.ToString(), viewModel.SeasonPointsCapText);
        Assert.True(viewModel.IsCompanyValid);
        Assert.True(viewModel.StartCompanyCommand.CanExecute(null));
        Assert.True(profile.IsLieutenantBlocked);
    }

    [Fact]
    public async Task TroopersWithoutALieutenantLeaveTheCompanyInvalid()
    {
        var viewModel = await OpenWithFactionAsync();
        var trooper = viewModel.Profiles.First(profile => !profile.IsLieutenant && profile.IsVisible);

        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(trooper);

        Assert.Single(viewModel.MercsCompanyEntries);
        Assert.False(viewModel.IsCompanyValid);
        Assert.False(viewModel.StartCompanyCommand.CanExecute(null));
    }

    [Fact]
    public async Task ABlockedProfileIsNotAdded()
    {
        var viewModel = await OpenWithFactionAsync();
        var profile = await SelectLieutenantAsync(viewModel);
        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(profile);

        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(profile);

        Assert.Single(viewModel.MercsCompanyEntries);
    }

    [Fact]
    public async Task GoingOverThePointsLimitInvalidatesTheCompany()
    {
        var viewModel = await OpenWithFactionAsync();
        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(await SelectLieutenantAsync(viewModel));

        viewModel.SelectedStartSeasonPoints = "1";

        Assert.False(viewModel.IsCompanyValid);
    }

    [Fact]
    public async Task RemovingATrooperRecountsThePoints()
    {
        var viewModel = await OpenWithFactionAsync();
        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(await SelectLieutenantAsync(viewModel));

        viewModel.RemoveMercsCompanyEntryCommand.Execute(viewModel.MercsCompanyEntries[0]);

        Assert.Empty(viewModel.MercsCompanyEntries);
        Assert.Equal("0", viewModel.SeasonPointsCapText);
        Assert.False(viewModel.IsCompanyValid);
    }

    [Fact]
    public async Task SelectingACompanyTrooperShowsItsUnit()
    {
        var viewModel = await OpenWithFactionAsync();
        var profile = await SelectLieutenantAsync(viewModel);
        var lieutenantUnit = viewModel.SelectedUnit;
        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(profile);
        await viewModel.SelectUnitFromListCommand.ExecuteAsync(viewModel.Units.First(unit => unit.IsVisible && !ReferenceEquals(unit, lieutenantUnit)));

        await viewModel.SelectMercsCompanyEntryCommand.ExecuteAsync(viewModel.MercsCompanyEntries[0]);

        Assert.Same(lieutenantUnit, viewModel.SelectedUnit);
    }

    [Fact]
    public async Task ChangingTheFactionEmptiesTheCompany()
    {
        var viewModel = await OpenAsync(ArmySourceSelectionMode.Sectorials);
        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Factions[0]);
        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(await SelectLieutenantAsync(viewModel));
        var other = new InfinityMercsApp.ViewModels.Company.Selection.Items.ArmyFactionSelectionItem
        {
            Id = Army101Data.PanOceania,
            ParentId = Army101Data.PanOceania,
            Name = "PanOceania"
        };
        viewModel.SelectSlotCommand.Execute(0);

        await viewModel.SelectFactionCommand.ExecuteAsync(other);

        Assert.Empty(viewModel.MercsCompanyEntries);
    }

    [Fact]
    public async Task StartingNeedsACompanyName()
    {
        var viewModel = await OpenWithFactionAsync();
        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(await SelectLieutenantAsync(viewModel));

        // The name box starts empty and shows its placeholder.
        Assert.Equal(string.Empty, viewModel.CompanyName);
        await viewModel.StartCompanyCommand.ExecuteAsync(null);

        Assert.True(viewModel.ShowCompanyNameValidationError);
        await _kit.CaptainDialog.DidNotReceive().ShowAsync(Arg.Any<CaptainUpgradePopupContext>());

        viewModel.CompanyName = "Blue Lions";
        Assert.False(viewModel.ShowCompanyNameValidationError);
    }

    [Fact]
    public async Task BackingOutOfTheCaptainPopupStaysOnTheScreen()
    {
        _kit.CaptainDialog.ShowAsync(Arg.Any<CaptainUpgradePopupContext>()).Returns((SavedImprovedCaptainStats?)null);
        var viewModel = await OpenWithFactionAsync();
        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(await SelectLieutenantAsync(viewModel));
        viewModel.CompanyName = "Blue Lions";

        await viewModel.StartCompanyCommand.ExecuteAsync(null);

        await _kit.CaptainDialog.Received(1).ShowAsync(Arg.Any<CaptainUpgradePopupContext>());
        await _kit.Navigation.DidNotReceive().NavigateToAsync(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>());
    }

    [Fact]
    public async Task CompactScreensShowOneWorkspaceAtATime()
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
    public async Task WideScreensGiveTheCompanyAllTheHeightUntilAUnitIsSelected()
    {
        var viewModel = await OpenAsync();
        viewModel.ApplyLayoutCommand.Execute(AdaptiveLayoutMode.Medium);

        Assert.Null(viewModel.SelectedUnit);
        Assert.False(viewModel.ShowsDetailPane);
        Assert.Equal("0,0,*", viewModel.RightPaneRows);

        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Factions[0]);

        Assert.NotNull(viewModel.SelectedUnit);
        Assert.True(viewModel.ShowsDetailPane);
        Assert.Equal("*,3,*", viewModel.RightPaneRows);
    }

    [Fact]
    public async Task TappingAFactionSlotOpensThePickerAndTappingItAgainClosesIt()
    {
        var viewModel = await OpenWithFactionAsync();
        viewModel.CloseFactionStripCommand.Execute(null);

        viewModel.SelectSlotCommand.Execute(0);

        Assert.True(viewModel.ShowFactionStrip);
        Assert.Equal(0, viewModel.ActiveSlotIndex);

        viewModel.SelectSlotCommand.Execute(0);

        Assert.False(viewModel.ShowFactionStrip);
    }

    [Fact]
    public async Task TheFactionOverlayOnlyOpensOnCompactScreens()
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
