using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Lieutenants;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Services.CompanySelection.Captain;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.Logos;
using InfinityMercsApp.ViewModels.Captain.Items;

namespace InfinityMercsApp.ViewModels.Captain;

/// <summary>
/// Configures a company captain before the company is founded: stat tiers and extra weapons, skills and
/// equipment, all paid for from a starting experience budget. <see cref="Completion"/> yields the chosen
/// upgrades, or null when the user backs out.
/// </summary>
public sealed partial class ConfigureCaptainViewModel : ObservableObject
{
    public const string DefaultCaptainName = "Captain";

    private readonly CaptainUpgradePopupContext _context;
    private readonly IReadOnlyList<StatValue> _statline;
    private readonly TaskCompletionSource<SavedImprovedCaptainStats?> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly CaptainStatUpgrade _cc;
    private readonly CaptainStatUpgrade _bs;
    private readonly CaptainStatUpgrade _ph;
    private readonly CaptainStatUpgrade _wip;
    private readonly CaptainStatUpgrade _arm;
    private readonly CaptainStatUpgrade _bts;
    private readonly CaptainStatUpgrade _vitality;

    [ObservableProperty]
    public partial string CaptainName { get; set; }

    [ObservableProperty]
    public partial string CaptainNameDraft { get; set; }

    [ObservableProperty]
    public partial bool IsEditingName { get; set; }

    [ObservableProperty]
    public partial CaptainStatBonuses SelectedBonuses { get; set; } = CaptainStatBonuses.None;

    /// <summary>The unit's statline with the chosen bonuses applied.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<CaptainStatCell> StatCells { get; set; } = [];

    [ObservableProperty]
    public partial string RangedText { get; set; } = ProfileText.Empty;

    /// <summary>The skills with chosen ones appended, split so lieutenant mentions can be highlighted.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<TextRun> SkillRuns { get; set; } = [];

    [ObservableProperty]
    public partial string EquipmentText { get; set; } = ProfileText.Empty;

    [ObservableProperty]
    public partial int SpentExperience { get; set; }

    [ObservableProperty]
    public partial int ExperienceRemaining { get; set; }

    [ObservableProperty]
    public partial string ExperienceRemainingText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsOverspent { get; set; }

    /// <summary>What breaks the rules in the build so far, one message per problem.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProblems))]
    public partial IReadOnlyList<string> Problems { get; set; } = [];

    /// <summary>True when the build follows the rules and can be confirmed.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    public partial bool IsValid { get; set; } = true;

    public ConfigureCaptainViewModel(CaptainUpgradePopupContext context)
    {
        _context = context;
        _statline = Statline.Entries(context.Unit.Statline);
        CaptainName = TextOr(context.DefaultUnitCustomName, DefaultCaptainName);
        CaptainNameDraft = CaptainName;

        var baseStats = Statline.NumericStats(context.Unit.Statline);
        int BaseOf(params IEnumerable<Stat> stats) =>
            stats.Select(stat => baseStats.TryGetValue(stat, out var value) ? (int?)value : null).FirstOrDefault(value => value.HasValue) ?? 0;

        _cc = CaptainStatUpgrade.For(Stat.Cc, BaseOf(Stat.Cc), context.StatChart, Recalculate);
        _bs = CaptainStatUpgrade.For(Stat.Bs, BaseOf(Stat.Bs), context.StatChart, Recalculate);
        _ph = CaptainStatUpgrade.For(Stat.Ph, BaseOf(Stat.Ph), context.StatChart, Recalculate);
        _wip = CaptainStatUpgrade.For(Stat.Wip, BaseOf(Stat.Wip), context.StatChart, Recalculate);
        _arm = CaptainStatUpgrade.For(Stat.Arm, BaseOf(Stat.Arm), context.StatChart, Recalculate);
        _bts = CaptainStatUpgrade.For(Stat.Bts, BaseOf(Stat.Bts), context.StatChart, Recalculate);
        _vitality = CaptainStatUpgrade.For(Stat.Vita, BaseOf(Stat.Vita, Stat.Str, Stat.W), context.StatChart, Recalculate);
        StatUpgrades = [_cc, _bs, _ph, _wip, _arm, _bts, _vitality];

        Weapons = CaptainChoiceGroup.For("Weapons", context.WeaponOptions, Recalculate);
        Skills = CaptainChoiceGroup.For("Skills", context.SkillOptions, Recalculate);
        Equipment = CaptainChoiceGroup.For("Equipment", context.EquipmentOptions, Recalculate);
        ChoiceGroups = [Weapons, Skills, Equipment];
        Recalculate();
    }

    public string Title => TextOr(_context.PopupTitle, "Captain Configuration");

    public string ConfirmText => TextOr(_context.ConfirmButtonText, "FOUND COMPANY");

    public string? LogoSource => UnitLogo.Source(_context.Unit.CachedLogoPath, _context.Unit.PackagedLogoPath);

    public string UpgradeOptionsHeader => $"Upgrade Options ({_context.OptionFactionName})";

    /// <summary>CC, BS, PH, WIP, ARM, BTS and VITA, in the order the pickers are laid out.</summary>
    public IReadOnlyList<CaptainStatUpgrade> StatUpgrades { get; }

    public CaptainChoiceGroup Weapons { get; }

    public CaptainChoiceGroup Skills { get; }

    public CaptainChoiceGroup Equipment { get; }

    public IReadOnlyList<CaptainChoiceGroup> ChoiceGroups { get; }

    public bool HasStatline => _statline.Count > 0;

    public string CcText => TextOr(_context.Unit.CcWeapons, ProfileText.Empty);

    public int StartingExperience =>
        CaptainUpgradeRules.StartingExperience(_context.Unit.Cost, _context.BaseExperienceOverride);

    public bool HasProblems => Problems.Count > 0;

    /// <summary>Completes once with the chosen upgrades on confirm, or null on back.</summary>
    public Task<SavedImprovedCaptainStats?> Completion => _completion.Task;

    [RelayCommand]
    private void EditName()
    {
        CaptainNameDraft = CaptainName;
        IsEditingName = true;
    }

    /// <summary>Keeps the edited name; a blank one falls back to "Captain".</summary>
    [RelayCommand]
    private void SaveName()
    {
        CaptainName = TextOr(CaptainNameDraft, DefaultCaptainName);
        CaptainNameDraft = CaptainName;
        IsEditingName = false;
    }

    [RelayCommand]
    private void RejectName()
    {
        CaptainNameDraft = CaptainName;
        IsEditingName = false;
    }

    [RelayCommand(CanExecute = nameof(IsValid))]
    private void Confirm()
    {
        if (IsEditingName)
        {
            SaveName();
        }

        _completion.TrySetResult(BuildResult());
    }

    [RelayCommand]
    private void Cancel() => _completion.TrySetResult(null);

    /// <summary>The captain as configured so far.</summary>
    public SavedImprovedCaptainStats BuildResult() => new()
    {
        IsEnabled = true,
        CaptainName = CaptainName,
        CcTier = _cc.Tier,
        BsTier = _bs.Tier,
        PhTier = _ph.Tier,
        WipTier = _wip.Tier,
        ArmTier = _arm.Tier,
        BtsTier = _bts.Tier,
        VitalityTier = _vitality.Tier,
        CcBonus = _cc.Bonus,
        BsBonus = _bs.Bonus,
        PhBonus = _ph.Bonus,
        WipBonus = _wip.Bonus,
        ArmBonus = _arm.Bonus,
        BtsBonus = _bts.Bonus,
        VitalityBonus = _vitality.Bonus,
        WeaponChoice1 = Weapons.First.ChosenName,
        WeaponChoice2 = Weapons.Second.ChosenName,
        WeaponChoice3 = Weapons.Third.ChosenName,
        SkillChoice1 = Skills.First.ChosenName,
        SkillChoice2 = Skills.Second.ChosenName,
        SkillChoice3 = Skills.Third.ChosenName,
        EquipmentChoice1 = Equipment.First.ChosenName,
        EquipmentChoice2 = Equipment.Second.ChosenName,
        EquipmentChoice3 = Equipment.Third.ChosenName,
        OptionFactionId = _context.OptionFactionId,
        OptionFactionName = _context.OptionFactionName
    };

    /// <summary>The captain as the rules see it, for working out its cost and checking it.</summary>
    public CaptainBuild Build() => new(
        StartingExperience,
        StatUpgrades.Select(upgrade => upgrade.Pick).ToList(),
        ChoiceGroups.Select(group => group.Picks).ToList());

    // Works out everything shown from the stat and choice pickers, and whether the build can be confirmed.
    private void Recalculate()
    {
        SelectedBonuses = new CaptainStatBonuses(_cc.Bonus, _bs.Bonus, _ph.Bonus, _wip.Bonus, _arm.Bonus, _bts.Bonus, _vitality.Bonus);
        StatCells = _statline.Select(entry => CaptainStatCell.For(entry, SelectedBonuses)).ToList();
        RangedText = ProfileText.AppendMarked(_context.Unit.RangedWeapons, Weapons.ChosenNames);
        SkillRuns = LieutenantRules.HighlightRuns(ProfileText.AppendMarked(_context.Unit.Skills, Skills.ChosenNames));
        EquipmentText = ProfileText.AppendMarked(_context.Unit.Equipment, Equipment.ChosenNames);

        var build = Build();
        SpentExperience = build.SpentExperience;
        ExperienceRemaining = build.ExperienceRemaining;
        ExperienceRemainingText = $"Exp Remaining: {ExperienceRemaining}";
        IsOverspent = ExperienceRemaining < 0;
        Problems = CaptainBuildValidation.Problems(build).Select(problem => problem.Message).ToList();
        IsValid = Problems.Count == 0;
    }

    private static string TextOr(string? text, string fallback) =>
        string.IsNullOrWhiteSpace(text) ? fallback : text.Trim();
}
