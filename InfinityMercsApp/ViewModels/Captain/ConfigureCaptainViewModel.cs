using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Lieutenants;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Services.CompanySelection.Captain;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.Logos;

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
    private string _captainName;

    [ObservableProperty]
    private string _captainNameDraft;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNameReadOnly))]
    private bool _isEditingName;

    public ConfigureCaptainViewModel(CaptainUpgradePopupContext context)
    {
        _context = context;
        _statline = Statline.Entries(context.Unit.Statline);
        _captainName = TextOr(context.DefaultUnitCustomName, DefaultCaptainName);
        _captainNameDraft = _captainName;

        var baseStats = Statline.NumericStats(context.Unit.Statline);
        int BaseOf(params IEnumerable<Stat> stats) =>
            stats.Select(stat => baseStats.TryGetValue(stat, out var value) ? (int?)value : null).FirstOrDefault(value => value.HasValue) ?? 0;

        _cc = CaptainStatUpgrade.For(Stat.Cc, BaseOf(Stat.Cc), Refresh);
        _bs = CaptainStatUpgrade.For(Stat.Bs, BaseOf(Stat.Bs), Refresh);
        _ph = CaptainStatUpgrade.For(Stat.Ph, BaseOf(Stat.Ph), Refresh);
        _wip = CaptainStatUpgrade.For(Stat.Wip, BaseOf(Stat.Wip), Refresh);
        _arm = CaptainStatUpgrade.For(Stat.Arm, BaseOf(Stat.Arm), Refresh);
        _bts = CaptainStatUpgrade.For(Stat.Bts, BaseOf(Stat.Bts), Refresh);
        _vitality = CaptainStatUpgrade.For(Stat.Vita, BaseOf(Stat.Vita, Stat.Str, Stat.W), Refresh);
        StatUpgrades = [_cc, _bs, _ph, _wip, _arm, _bts, _vitality];

        Weapons = CaptainChoiceGroup.For("Weapons", context.WeaponOptions, Refresh);
        Skills = CaptainChoiceGroup.For("Skills", context.SkillOptions, Refresh);
        Equipment = CaptainChoiceGroup.For("Equipment", context.EquipmentOptions, Refresh);
        ChoiceGroups = [Weapons, Skills, Equipment];
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

    public bool IsNameReadOnly => !IsEditingName;

    public CaptainStatBonuses SelectedBonuses => new(
        _cc.Bonus, _bs.Bonus, _ph.Bonus, _wip.Bonus, _arm.Bonus, _bts.Bonus, _vitality.Bonus);

    /// <summary>The unit's statline with the chosen bonuses applied.</summary>
    public IReadOnlyList<CaptainStatCell> StatCells =>
        _statline.Select(entry => CaptainStatCell.For(entry, SelectedBonuses)).ToList();

    public bool HasStatline => _statline.Count > 0;

    public string RangedText => ProfileText.AppendMarked(_context.Unit.RangedWeapons, Weapons.ChosenNames);

    public string CcText => TextOr(_context.Unit.CcWeapons, ProfileText.Empty);

    /// <summary>The skills with chosen ones appended, split so lieutenant mentions can be highlighted.</summary>
    public IReadOnlyList<TextRun> SkillRuns =>
        LieutenantRules.HighlightRuns(ProfileText.AppendMarked(_context.Unit.Skills, Skills.ChosenNames));

    public string EquipmentText => ProfileText.AppendMarked(_context.Unit.Equipment, Equipment.ChosenNames);

    public int StartingExperience =>
        CaptainUpgradeRules.StartingExperience(_context.Unit.Cost, _context.BaseExperienceOverride);

    public int SpentExperience =>
        StatUpgrades.Sum(upgrade => upgrade.Cost) + ChoiceGroups.Sum(group => group.Cost);

    public int ExperienceRemaining => StartingExperience - SpentExperience;

    public string ExperienceRemainingText => $"Exp Remaining: {ExperienceRemaining}";

    public bool IsOverspent => ExperienceRemaining < 0;

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

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private void Confirm()
    {
        if (IsEditingName)
        {
            SaveName();
        }

        _completion.TrySetResult(BuildResult());
    }

    private bool CanConfirm() => !IsOverspent;

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

    private void Refresh()
    {
        OnPropertyChanged(nameof(SelectedBonuses));
        OnPropertyChanged(nameof(StatCells));
        OnPropertyChanged(nameof(RangedText));
        OnPropertyChanged(nameof(SkillRuns));
        OnPropertyChanged(nameof(EquipmentText));
        OnPropertyChanged(nameof(SpentExperience));
        OnPropertyChanged(nameof(ExperienceRemaining));
        OnPropertyChanged(nameof(ExperienceRemainingText));
        OnPropertyChanged(nameof(IsOverspent));
        ConfirmCommand.NotifyCanExecuteChanged();
    }

    private static string TextOr(string? text, string fallback) =>
        string.IsNullOrWhiteSpace(text) ? fallback : text.Trim();
}
