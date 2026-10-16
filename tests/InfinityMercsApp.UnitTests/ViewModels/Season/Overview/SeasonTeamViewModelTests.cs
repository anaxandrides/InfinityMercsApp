using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Season.Inventory;
using InfinityMercsApp.Domain.Season.Troopers;
using InfinityMercsApp.ViewModels.Company.Viewer;
using InfinityMercsApp.ViewModels.Season.Overview;
using NSubstitute;
using InfinityMercsApp.ViewModels.Season.Overview.Items;
using InfinityMercsApp.ViewModels.Components;
using InfinityMercsApp.ViewModels.Company.Viewer.Items;

namespace InfinityMercsApp.UnitTests.ViewModels.Season.Overview;

public sealed class SeasonTeamViewModelTests
{
    private readonly SeasonTestKit _kit = new();
    private readonly MarketplaceDetailViewModel _popup;
    private readonly SeasonTeamViewModel _team;

    public SeasonTeamViewModelTests()
    {
        _popup = _kit.Popup();
        _team = new SeasonTeamViewModel(new CompanyRoster(_kit.ArmyData), new UnitDetailViewModel(_kit.ArmyData, _kit.Settings), _kit.PackageFiles, _kit.SeasonFiles, _popup, _kit.Dialogs);
    }

    // The company's Combi Rifle, two pistols (one bought at each market), Light Armour and a WIP augment.
    private static SeasonInventory Owned() => SeasonInventory.From(
        new SeasonFile
        {
            Rounds =
            [
                new SeasonRound
                {
                    RoundIndex = 1,
                    Marketplace = new SeasonMarketplace
                    {
                        Transactions =
                        [
                            SeasonTestKit.Bought("Number One", "Pistol"),
                            SeasonTestKit.Bought("Number One", "Spitfire"),
                            SeasonTestKit.Bought("Number One", "Light Armour (LI)"),
                            SeasonTestKit.Bought("Number One", "WIP=12"),
                            SeasonTestKit.Bought("Number One", "Courage"),
                            SeasonTestKit.Bought("Jade Temu", "Monofilament Blade")
                        ]
                    }
                }
            ]
        },
        Catalog);

    private static InventoryCatalog Catalog { get; } = InventoryCatalog.From([SeasonTestKit.NumberOne, SeasonTestKit.JadeTemu]);

    // The Spitfire takes 1 SWC; the company has 2 unless a test says otherwise.
    private async Task LoadAsync(CompanyLoadouts? loadouts = null, double companySwc = 2)
    {
        _kit.Season.UnitGear = (loadouts ?? new CompanyLoadouts()).ToSeasonGear();
        await _team.LoadAsync(SeasonTestKit.Company(), SeasonTestKit.SeasonPath, _kit.Season);
        _team.ShowOwnedGear(Owned(), Catalog, companySwc);
    }

    private CompanyLoadouts SavedLoadouts() => CompanyLoadouts.From(_kit.Season.UnitGear);

    private GearSlotPicker Picker(GearSlot slot) => _team.GearSlots.Single(picker => picker.Slot == slot);

    private CompanyViewerUnitListItem Unit(string name) => _team.Units.Single(unit => unit.Name == name);

    [Fact]
    public async Task LoadingSelectsTheLieutenantFirst()
    {
        await LoadAsync();

        Assert.Equal("Captain", _team.PickerTitle);
        Assert.Equal("Fusilier", _team.SelectedBaseName);
        Assert.True(_team.HasSelectedBaseName);
        Assert.Equal("LI", _team.SelectedUnitType);
        Assert.Equal(1, _team.Detail.Icons.Lieutenants);
        Assert.Equal("Lieutenant", _team.Skills.Left[0].Text);
        Assert.Equal(ProfileLine.None, _team.Equipment.Left.Single());
    }

    [Fact]
    public async Task LoadingWorksOutEachTroopersLogo()
    {
        _kit.PackageFiles.ExistsAsync("SVGCache/units/101-11.svg").Returns(true);

        await LoadAsync();

        Assert.Equal("SVGCache/units/101-11.svg", Unit("Bolt").LogoSource);
        Assert.Equal("SVGCache/units/101-12.svg", _team.Detail.LogoSource);
    }

    [Fact]
    public async Task TheUnitPickerOpensAndClosesOnSelection()
    {
        await LoadAsync();

        _team.ToggleUnitPickerCommand.Execute(null);
        Assert.True(_team.IsUnitPickerOpen);
        _team.SelectUnitCommand.Execute(Unit("Bolt"));

        Assert.False(_team.IsUnitPickerOpen);
        Assert.Equal("Bolt", _team.PickerTitle);
        Assert.True(Unit("Bolt").IsSelected);
        Assert.False(Unit("Captain").IsSelected);
        Assert.False(_team.HasSelectedBaseName);
        Assert.Equal("Combi Rifle", _team.RangedWeapons.Left.Single().Text);
    }

    [Fact]
    public async Task TappingOutsideClosesTheUnitPicker()
    {
        await LoadAsync();
        _team.ToggleUnitPickerCommand.Execute(null);

        _team.CloseUnitPickerCommand.Execute(null);

        Assert.False(_team.IsUnitPickerOpen);
    }

    [Fact]
    public async Task EachSlotOffersTheOwnedGearThatFitsIt()
    {
        await LoadAsync();

        Assert.Equal(["(none)", "Spitfire (1 SWC)"], Picker(GearSlot.Primary).Options.Select(option => option.Display));
        Assert.Equal(["(none)", "Monofilament Blade"], Picker(GearSlot.Secondary).Options.Select(option => option.Display));
        Assert.Equal(["(none)", "Light Armour (LI)"], Picker(GearSlot.Armor).Options.Select(option => option.Display));
        Assert.Equal(["(none)"], Picker(GearSlot.Roles).Options.Select(option => option.Display));
        Assert.All(_team.GearSlots, picker => Assert.Equal(GearOption.None, picker.SelectedOption));
    }

    [Fact]
    public async Task PickingAWeaponAddsItToTheWeaponsAndSavesIt()
    {
        await LoadAsync();

        Picker(GearSlot.Primary).SelectedOption = Picker(GearSlot.Primary).Options[1];
        Picker(GearSlot.Secondary).SelectedOption = Picker(GearSlot.Secondary).Options[1];

        Assert.Equal("Spitfire", _team.RangedWeapons.Left.Single().Text);
        Assert.Equal("Monofilament Blade", _team.MeleeWeapons.Left.Single().Text);
        Assert.Equal("Spitfire", _kit.Season.UnitGear[2].Single(gear => gear.Slot == "Primary").ItemName);
        await _kit.SeasonFiles.Received(2).SaveAsync(SeasonTestKit.SeasonPath, _kit.Season);
    }

    [Fact]
    public async Task GearCarriedByAnotherTrooperIsMarkedAndMovesWhenPicked()
    {
        var loadouts = new CompanyLoadouts();
        loadouts.Assign(1, GearSlot.Armor, "Light Armour (LI)");
        await LoadAsync(loadouts);

        var armor = Picker(GearSlot.Armor);
        Assert.Equal("Light Armour (LI)  [Attached: Bolt]", armor.Options[1].Display);
        armor.SelectedOption = armor.Options[1];

        Assert.Equal("Light Armour (LI)", Picker(GearSlot.Armor).SelectedOption?.Display);
        Assert.Null(SavedLoadouts().For(1)[GearSlot.Armor]);
        _team.SelectUnitCommand.Execute(Unit("Bolt"));
        Assert.Equal("Light Armour (LI)  [Attached: Captain]", Picker(GearSlot.Armor).Options[1].Display);
        Assert.Equal(GearOption.None, Picker(GearSlot.Armor).SelectedOption);
    }

    [Fact]
    public async Task ChoosingNothingEmptiesTheSlot()
    {
        var loadouts = new CompanyLoadouts();
        loadouts.Assign(2, GearSlot.Primary, "Spitfire");
        await LoadAsync(loadouts);
        Assert.Equal("Spitfire", Picker(GearSlot.Primary).SelectedOption?.ItemName);

        Picker(GearSlot.Primary).SelectedOption = GearOption.None;

        Assert.Null(SavedLoadouts().For(2)[GearSlot.Primary]);
        Assert.Equal(ProfileLine.None, _team.RangedWeapons.Left.Single());
    }

    [Fact]
    public async Task TheTabShowsTheEquippedSwcAgainstTheCompanys()
    {
        var loadouts = new CompanyLoadouts();
        loadouts.Assign(1, GearSlot.Primary, "Spitfire");
        await LoadAsync(loadouts, companySwc: 1.5);

        Assert.Equal("SWC EQUIPPED 1 / 1.5", _team.SwcText);
    }

    [Fact]
    public async Task GearOverTheCompanysSwcIsRefused()
    {
        await LoadAsync(companySwc: 0.5);

        Picker(GearSlot.Primary).SelectedOption = Picker(GearSlot.Primary).Options[1];

        await _kit.Dialogs.Received(1).ShowAlertAsync("Not Enough SWC", Arg.Is<string>(text => text.Contains("Spitfire") && text.Contains("over its 0.5 SWC")));
        Assert.Equal(GearOption.None, Picker(GearSlot.Primary).SelectedOption);
        Assert.Empty(_kit.Season.UnitGear);
        await _kit.SeasonFiles.DidNotReceive().SaveAsync(Arg.Any<string?>(), Arg.Any<SeasonFile?>());
    }

    [Fact]
    public async Task GearUpToTheCompanysSwcIsAllowed()
    {
        await LoadAsync(companySwc: 1);

        Picker(GearSlot.Primary).SelectedOption = Picker(GearSlot.Primary).Options[1];

        Assert.Equal("Spitfire", SavedLoadouts().For(2)[GearSlot.Primary]);
        Assert.Equal("SWC EQUIPPED 1 / 1", _team.SwcText);
        await _kit.Dialogs.DidNotReceive().ShowAlertAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task MovingGearBetweenTroopersTakesNoMoreSwc()
    {
        var loadouts = new CompanyLoadouts();
        loadouts.Assign(1, GearSlot.Primary, "Spitfire");
        await LoadAsync(loadouts, companySwc: 1);

        Picker(GearSlot.Primary).SelectedOption = Picker(GearSlot.Primary).Options[1];

        Assert.Equal("Spitfire", SavedLoadouts().For(2)[GearSlot.Primary]);
        Assert.Null(SavedLoadouts().For(1)[GearSlot.Primary]);
    }

    [Fact]
    public async Task ACompanyAlreadyOverItsSwcCanStillTakeGearOff()
    {
        var loadouts = new CompanyLoadouts();
        loadouts.Assign(2, GearSlot.Primary, "Spitfire");
        await LoadAsync(loadouts, companySwc: 0);

        Picker(GearSlot.Primary).SelectedOption = GearOption.None;

        Assert.Null(SavedLoadouts().For(2)[GearSlot.Primary]);
        await _kit.Dialogs.DidNotReceive().ShowAlertAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task AStatAugmentChangesTheStatLine()
    {
        await LoadAsync();

        Picker(GearSlot.Augments).SelectedOption = Picker(GearSlot.Augments).Options.Single(option => option.ItemName == "WIP=12");

        Assert.Equal("12", _team.Detail.Stats.Wip);
        Assert.DoesNotContain(_team.Skills.Left.Concat(_team.Skills.Right), line => line.Text == "WIP=12");
    }

    [Fact]
    public async Task ASkillAugmentJoinsTheSkills()
    {
        await LoadAsync();

        Picker(GearSlot.Augments).SelectedOption = Picker(GearSlot.Augments).Options.Single(option => option.ItemName == "Courage");

        Assert.Equal(["Lieutenant", "Courage"], _team.Skills.Left.Concat(_team.Skills.Right).Select(line => line.Text));
    }

    [Fact]
    public async Task APickerClearingItselfIsNotAChoice()
    {
        await LoadAsync();

        Picker(GearSlot.Primary).SelectedOption = null;

        await _kit.SeasonFiles.DidNotReceiveWithAnyArgs().SaveAsync(default, default);
    }

    [Fact]
    public async Task TappingAWeaponShowsItsProfiles()
    {
        _kit.Metadata.SearchWeaponsByName("Combi Rifle").Returns([new Weapon { Name = "Combi Rifle" }]);
        await LoadAsync();
        _team.SelectUnitCommand.Execute(Unit("Bolt"));

        await _team.ShowWeaponCommand.ExecuteAsync(_team.RangedWeapons.Left[0]);

        Assert.Equal("Combi Rifle", _popup.Detail?.Title);
        Assert.Single(_popup.Detail?.Weapons ?? []);
    }

    [Fact]
    public async Task ThePlaceholderIsNotAWeapon()
    {
        await LoadAsync();

        await _team.ShowWeaponCommand.ExecuteAsync(ProfileLine.None);

        Assert.False(_popup.IsOpen);
    }

    [Fact]
    public async Task ChangingDisplayUnitsRedrawsTheTrooper()
    {
        await LoadAsync();
        _kit.Settings.GetShowUnitsInInches().Returns(false);

        _team.RefreshDisplayUnits();

        Assert.False(_team.Detail.ShowUnitsInInches);
        Assert.False(_popup.ShowUnitsInInches);
    }

    [Fact]
    public async Task ClearingEmptiesTheTab()
    {
        await LoadAsync();

        _team.Clear();

        Assert.Empty(_team.Units);
        Assert.Equal(SeasonTeamViewModel.NoUnitTitle, _team.PickerTitle);
    }

    [Fact]
    public async Task ATrooperShowsItsRankPerksAndInjuries()
    {
        _kit.Season.Rounds[0].MissionResults.UnitResults.Add(new SeasonMissionUnitResult { EntryIndex = 1, UnitName = "Bolt", XpGained = 16 });
        _kit.Season.TrooperFor(1).Perks.Add("reflex-track-8-tier-2");
        _kit.Season.TrooperFor(1).Injuries.Add(Injury.Arms);
        await LoadAsync();

        _team.SelectUnitCommand.Execute(Unit("Bolt"));

        Assert.Equal("Hired Gun · 16 XP", _team.RankText);
        Assert.Equal(["+1 BS"], _team.Perks);
        Assert.Equal(["Arms: -1 BS"], _team.Injuries);
        Assert.True(_team.HasPerks && _team.HasInjuries && _team.HasPerkPointsToSpend);
        Assert.Contains(_team.Skills.Left.Concat(_team.Skills.Right), line => line.Text == "Courage");
    }

    [Fact]
    public async Task InjuriesChangeTheStatLine()
    {
        _kit.Season.TrooperFor(2).Injuries.Add(Injury.BrainInjury);

        await LoadAsync();

        Assert.Equal("11", _team.Detail.Stats.Cc);
    }

    [Fact]
    public async Task ReloadingKeepsTheTrooperShown()
    {
        await LoadAsync();
        _team.SelectUnitCommand.Execute(Unit("Bolt"));

        await LoadAsync();

        Assert.Equal("Bolt", _team.SelectedName);
    }
}
