using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Lieutenants;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Services.CompanySelection.Captain;
using InfinityMercsApp.ViewModels.Captain;

namespace InfinityMercsApp.UnitTests.ViewModels.Captain;

public sealed class ConfigureCaptainViewModelTests
{
    private const string FusilierStatline = "MOV 10-5 | CC 13 | BS 12 | PH 10 | WIP 12 | ARM 1 | BTS 0 | VITA 1 | S 2";

    private static CaptainUpgradePopupContext Context(
        int unitCost = 20,
        string statline = FusilierStatline,
        int? baseExperienceOverride = null,
        string popupTitle = "Captain Configuration",
        string confirmText = "FOUND COMPANY",
        string defaultName = "Captain") => new()
    {
        Unit = new CaptainUnitPopupInfo
        {
            Name = "Fusilier",
            Cost = unitCost,
            Statline = statline,
            RangedWeapons = "Combi Rifle",
            CcWeapons = "Pistol",
            Skills = "Lieutenant",
            Equipment = "-",
            PackagedLogoPath = "SVGCache/units/fusilier.svg"
        },
        OptionFactionId = 101,
        OptionFactionName = "PanOceania",
        WeaponOptions = ["(2) - Sniper Rifle", " (1) - Grenades ", "(2) - sniper rifle", ""],
        SkillOptions = ["(3) - Lt", "(2) - Courage"],
        EquipmentOptions = ["(4) - Multispectral Visor L1"],
        PopupTitle = popupTitle,
        ConfirmButtonText = confirmText,
        DefaultUnitCustomName = defaultName,
        BaseExperienceOverride = baseExperienceOverride
    };

    private static CaptainStatUpgrade UpgradeOf(ConfigureCaptainViewModel viewModel, Stat stat)
    {
        var upgrade = viewModel.StatUpgrades.FirstOrDefault(candidate => candidate.Stat == stat);
        Assert.NotNull(upgrade);
        return upgrade;
    }

    private static void PickTier(ConfigureCaptainViewModel viewModel, Stat stat, int tier)
    {
        var upgrade = UpgradeOf(viewModel, stat);
        upgrade.SelectedOption = upgrade.Options.First(option => option.Tier == tier);
    }

    [Fact]
    public void StartsWithNothingBoughtAndTheUnitsPointsUnderTheCapAsExperience()
    {
        var viewModel = new ConfigureCaptainViewModel(Context(unitCost: 20));

        Assert.Equal("Captain", viewModel.CaptainName);
        Assert.Equal("Upgrade Options (PanOceania)", viewModel.UpgradeOptionsHeader);
        Assert.Equal(8, viewModel.ExperienceRemaining);
        Assert.Equal("Exp Remaining: 8", viewModel.ExperienceRemainingText);
        Assert.False(viewModel.IsOverspent);
        Assert.True(viewModel.ConfirmCommand.CanExecute(null));
        Assert.All(viewModel.StatUpgrades, upgrade => Assert.Equal(0, upgrade.Tier));
        Assert.All(viewModel.ChoiceGroups.SelectMany(group => group.Slots), slot => Assert.Equal(CaptainChoiceLabel.None, slot.SelectedChoice));
        Assert.Equal("Combi Rifle", viewModel.RangedText);
        Assert.Equal("Pistol", viewModel.CcText);
        Assert.Equal(ProfileText.Empty, viewModel.EquipmentText);
    }

    [Fact]
    public void StatPickersAreLaidOutInOrder() =>
        Assert.Equal(
            [Stat.Cc, Stat.Bs, Stat.Ph, Stat.Wip, Stat.Arm, Stat.Bts, Stat.Vita],
            new ConfigureCaptainViewModel(Context()).StatUpgrades.Select(upgrade => upgrade.Stat));

    [Fact]
    public void ABaseExperienceOverrideReplacesTheUnitCostBudget() =>
        Assert.Equal(3, new ConfigureCaptainViewModel(Context(unitCost: 10, baseExperienceOverride: 3)).ExperienceRemaining);

    [Fact]
    public void BlankTextsFallBackToDefaults()
    {
        var viewModel = new ConfigureCaptainViewModel(Context(popupTitle: " ", confirmText: "", defaultName: "  "));

        Assert.Equal("Captain Configuration", viewModel.Title);
        Assert.Equal("FOUND COMPANY", viewModel.ConfirmText);
        Assert.Equal(ConfigureCaptainViewModel.DefaultCaptainName, viewModel.CaptainName);
    }

    [Fact]
    public void TextsAreTrimmed()
    {
        var viewModel = new ConfigureCaptainViewModel(Context(popupTitle: " TAG Spec Ops ", confirmText: " Finalize Tag ", defaultName: " TAG "));

        Assert.Equal("TAG Spec Ops", viewModel.Title);
        Assert.Equal("Finalize Tag", viewModel.ConfirmText);
        Assert.Equal("TAG", viewModel.CaptainName);
    }

    [Fact]
    public void TheLogoIsThePackagedOneWhenNothingIsCached() =>
        Assert.Equal("SVGCache/units/fusilier.svg", new ConfigureCaptainViewModel(Context()).LogoSource);

    [Fact]
    public void TheStatlineShowsEveryStatUnmodifiedAtFirst()
    {
        var viewModel = new ConfigureCaptainViewModel(Context());

        Assert.True(viewModel.HasStatline);
        Assert.Equal(
            ["MOV", "CC", "BS", "PH", "WIP", "ARM", "BTS", "VITA", "S"],
            viewModel.StatCells.Select(cell => cell.Key));
        Assert.All(viewModel.StatCells, cell => Assert.False(cell.IsModified));
    }

    [Fact]
    public void AnUnreadableStatlineHasNoCells()
    {
        var viewModel = new ConfigureCaptainViewModel(Context(statline: "-"));

        Assert.False(viewModel.HasStatline);
        Assert.Empty(viewModel.StatCells);
    }

    [Fact]
    public void BuyingAStatTierRaisesItInTheStatlineAndSpendsItsCost()
    {
        var viewModel = new ConfigureCaptainViewModel(Context(unitCost: 20));
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        PickTier(viewModel, Stat.Cc, 2);

        var cc = viewModel.StatCells.Single(cell => cell.Key == "CC");
        Assert.Equal(new CaptainStatCell("CC", "18", true), cc);
        Assert.Equal(3, viewModel.ExperienceRemaining);
        Assert.Contains(nameof(ConfigureCaptainViewModel.StatCells), changed);
        Assert.Contains(nameof(ConfigureCaptainViewModel.ExperienceRemaining), changed);
        Assert.Contains(nameof(ConfigureCaptainViewModel.IsOverspent), changed);
    }

    [Fact]
    public void VitalityIsUpgradedFromStrWhenTheUnitHasNoVita()
    {
        var viewModel = new ConfigureCaptainViewModel(Context(statline: "CC 13 | STR 1"));

        PickTier(viewModel, Stat.Vita, 1);

        Assert.Equal(new CaptainStatCell("STR", "2", true), viewModel.StatCells.Single(cell => cell.Key == "STR"));
        Assert.Equal(1, viewModel.SelectedBonuses.Vitality);
    }

    [Fact]
    public void OverspendingTurnsTheBudgetNegativeAndBlocksConfirm()
    {
        var viewModel = new ConfigureCaptainViewModel(Context(unitCost: 26));
        var canExecuteChanges = 0;
        viewModel.ConfirmCommand.CanExecuteChanged += (_, _) => canExecuteChanges++;

        PickTier(viewModel, Stat.Arm, 1);

        Assert.Equal(-3, viewModel.ExperienceRemaining);
        Assert.Equal("Exp Remaining: -3", viewModel.ExperienceRemainingText);
        Assert.True(viewModel.IsOverspent);
        Assert.False(viewModel.ConfirmCommand.CanExecute(null));
        Assert.True(canExecuteChanges > 0);
    }

    [Fact]
    public void ChoiceSlotsOfferNoneThenTrimmedDistinctOptions() =>
        Assert.Equal(
            [CaptainChoiceLabel.None, "(2) - Sniper Rifle", "(1) - Grenades"],
            new ConfigureCaptainViewModel(Context()).Weapons.First.Options);

    [Fact]
    public void ChosenWeaponsAreMarkedOnTheProfileOnceAndCostExperience()
    {
        var viewModel = new ConfigureCaptainViewModel(Context(unitCost: 20));

        viewModel.Weapons.First.SelectedChoice = "(2) - Sniper Rifle";
        viewModel.Weapons.Second.SelectedChoice = "(2) - Sniper Rifle";
        viewModel.Weapons.Third.SelectedChoice = "(1) - Grenades";

        Assert.Equal(string.Join(Environment.NewLine, "Combi Rifle", "+ Sniper Rifle", "+ Grenades"), viewModel.RangedText);
        Assert.Equal(3, viewModel.ExperienceRemaining);
    }

    [Fact]
    public void ChosenEquipmentReplacesTheEmptyPlaceholder()
    {
        var viewModel = new ConfigureCaptainViewModel(Context());

        viewModel.Equipment.Second.SelectedChoice = "(4) - Multispectral Visor L1";

        Assert.Equal("+ Multispectral Visor L1", viewModel.EquipmentText);
    }

    [Fact]
    public void LieutenantSkillsAreHighlighted()
    {
        var viewModel = new ConfigureCaptainViewModel(Context());

        viewModel.Skills.First.SelectedChoice = "(2) - Courage";

        Assert.Contains(new TextRun("Lieutenant", true), viewModel.SkillRuns);
        Assert.Contains(viewModel.SkillRuns, run => !run.IsLieutenant && run.Text.Contains("+ Courage"));
    }

    [Fact]
    public void EditingTheNameCanBeRejected()
    {
        var viewModel = new ConfigureCaptainViewModel(Context());

        viewModel.EditNameCommand.Execute(null);
        Assert.True(viewModel.IsEditingName);
        Assert.False(viewModel.IsNameReadOnly);
        viewModel.CaptainNameDraft = "Major Lunah";
        viewModel.RejectNameCommand.Execute(null);

        Assert.False(viewModel.IsEditingName);
        Assert.True(viewModel.IsNameReadOnly);
        Assert.Equal("Captain", viewModel.CaptainName);
        Assert.Equal("Captain", viewModel.CaptainNameDraft);
    }

    [Theory]
    [InlineData("  Major Lunah ", "Major Lunah")]
    [InlineData("   ", "Captain")]
    public void SavingTheNameTrimsItAndFallsBackWhenBlank(string typed, string expected)
    {
        var viewModel = new ConfigureCaptainViewModel(Context(defaultName: "Lieutenant"));

        viewModel.EditNameCommand.Execute(null);
        viewModel.CaptainNameDraft = typed;
        viewModel.SaveNameCommand.Execute(null);

        Assert.False(viewModel.IsEditingName);
        Assert.Equal(expected, viewModel.CaptainName);
        Assert.Equal(expected, viewModel.CaptainNameDraft);
    }

    [Fact]
    public async Task ConfirmCompletesWithTheConfiguredCaptain()
    {
        var viewModel = new ConfigureCaptainViewModel(Context(unitCost: 10));
        PickTier(viewModel, Stat.Cc, 1);
        PickTier(viewModel, Stat.Bts, 2);
        viewModel.Weapons.Second.SelectedChoice = "(1) - Grenades";
        viewModel.Skills.Third.SelectedChoice = "(3) - Lt";
        viewModel.Equipment.First.SelectedChoice = "(4) - Multispectral Visor L1";

        viewModel.ConfirmCommand.Execute(null);
        var result = await viewModel.Completion;

        Assert.NotNull(result);
        Assert.True(result.IsEnabled);
        Assert.Equal("Captain", result.CaptainName);
        Assert.Equal((1, 2), (result.CcTier, result.CcBonus));
        Assert.Equal((2, 6), (result.BtsTier, result.BtsBonus));
        Assert.Equal(0, result.ArmTier);
        Assert.Equal(new CaptainStatBonuses(2, 0, 0, 0, 0, 6, 0), result.Bonuses);
        Assert.Equal(["", "Grenades", ""], [result.WeaponChoice1, result.WeaponChoice2, result.WeaponChoice3]);
        Assert.Equal(["", "", "Lieutenant"], [result.SkillChoice1, result.SkillChoice2, result.SkillChoice3]);
        Assert.Equal(["Multispectral Visor L1", "", ""], [result.EquipmentChoice1, result.EquipmentChoice2, result.EquipmentChoice3]);
        Assert.Equal(101, result.OptionFactionId);
        Assert.Equal("PanOceania", result.OptionFactionName);
    }

    [Fact]
    public async Task ConfirmKeepsANameStillBeingEdited()
    {
        var viewModel = new ConfigureCaptainViewModel(Context());
        viewModel.EditNameCommand.Execute(null);
        viewModel.CaptainNameDraft = "Major Lunah";

        viewModel.ConfirmCommand.Execute(null);
        var result = await viewModel.Completion;

        Assert.NotNull(result);
        Assert.Equal("Major Lunah", result.CaptainName);
        Assert.False(viewModel.IsEditingName);
    }

    [Fact]
    public async Task CancelCompletesWithNothingAndLaterTapsAreIgnored()
    {
        var viewModel = new ConfigureCaptainViewModel(Context());

        viewModel.CancelCommand.Execute(null);
        viewModel.ConfirmCommand.Execute(null);

        Assert.Null(await viewModel.Completion);
    }

    [Fact]
    public void NothingCompletesUntilTheUserDecides() =>
        Assert.False(new ConfigureCaptainViewModel(Context()).Completion.IsCompleted);
}
