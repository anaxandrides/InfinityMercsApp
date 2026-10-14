using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Models.Army.Traits;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services.ArmyData;

namespace InfinityMercsApp.ViewModels.UnitBrowser;

/// <summary>
/// One unit as shown by the unit-detail display: stat line, trait icons, the equipment and skills all
/// its profiles share, and each configuration. Pages showing a saved trooper overlay its stats instead.
/// </summary>
public partial class UnitDetailViewModel(
    IArmyDataService armyData,
    IAppSettingsProvider? appSettings = null) : ObservableObject
{
    private const string NoUnit = "Select a unit";

    private Movement? _unitMovement;
    private IReadOnlyDictionary<int, string> _equipmentLookup = new Dictionary<int, string>();
    private IReadOnlyDictionary<int, string> _equipmentLinks = new Dictionary<int, string>();
    private IReadOnlyDictionary<int, string> _skillsLookup = new Dictionary<int, string>();
    private IReadOnlyDictionary<int, string> _skillsLinks = new Dictionary<int, string>();

    public ObservableCollection<ViewerProfileItem> Profiles { get; } = [];

    [ObservableProperty] private string _profilesStatus = "Select a unit.";
    [ObservableProperty] private string _unitNameHeading = NoUnit;
    [ObservableProperty] private string? _logoSource;

    [ObservableProperty] private string _unitMov = "-";
    [ObservableProperty] private string _unitCc = "-";
    [ObservableProperty] private string _unitBs = "-";
    [ObservableProperty] private string _unitPh = "-";
    [ObservableProperty] private string _unitWip = "-";
    [ObservableProperty] private string _unitArm = "-";
    [ObservableProperty] private string _unitBts = "-";
    [ObservableProperty] private string _unitVitalityHeader = Stat.Vita.Key();
    [ObservableProperty] private string _unitVitality = "-";
    [ObservableProperty] private string _unitS = "-";
    [ObservableProperty] private string _unitAva = "-";

    [ObservableProperty] private string _equipmentSummary = "Equipment: -";
    [ObservableProperty] private string _specialSkillsSummary = "Special Skills: -";
    [ObservableProperty] private FormattedString _equipmentSummaryFormatted = new();
    [ObservableProperty] private FormattedString _specialSkillsSummaryFormatted = new();

    [ObservableProperty] private bool _showRegularOrderIcon;
    [ObservableProperty] private bool _showIrregularOrderIcon;
    [ObservableProperty] private bool _showImpetuousIcon;
    [ObservableProperty] private bool _showTacticalAwarenessIcon;
    [ObservableProperty] private bool _showCubeIcon;
    [ObservableProperty] private bool _showCube2Icon;
    [ObservableProperty] private bool _showHackableIcon;

    [ObservableProperty] private string? _impetuousIconUrl;
    [ObservableProperty] private string? _tacticalAwarenessIconUrl;
    [ObservableProperty] private string? _cubeIconUrl;
    [ObservableProperty] private string? _cube2IconUrl;
    [ObservableProperty] private string? _hackableIconUrl;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowUnitsInCentimeters))]
    private bool _showUnitsInInches = true;

    public bool ShowUnitsInCentimeters
    {
        get => !ShowUnitsInInches;
        set => ShowUnitsInInches = !value;
    }

    partial void OnShowUnitsInInchesChanged(bool value) => UnitMov = armyData.FormatMovement(_unitMovement);

    /// <summary>Picks up the inches/centimetres preference from Settings.</summary>
    public void RefreshDisplayUnits()
    {
        try
        {
            if (appSettings is not null)
            {
                ShowUnitsInInches = appSettings.GetShowUnitsInInches();
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Reading the display units failed: {ex.Message}");
        }
    }

    /// <summary>Back to "Select a unit".</summary>
    public void Reset()
    {
        Profiles.Clear();
        ProfilesStatus = "Select a unit.";
        UnitNameHeading = NoUnit;
        LogoSource = null;
        ResetStats();
        EquipmentSummary = "Equipment: -";
        SpecialSkillsSummary = "Special Skills: -";
        _equipmentLookup = _equipmentLinks = _skillsLookup = _skillsLinks = new Dictionary<int, string>();
    }

    /// <summary>Loads the unit's stat line, trait icons, shared equipment and skills, and configurations.</summary>
    public void Load(UnitDetailRequest request, CancellationToken cancellationToken = default)
    {
        RefreshDisplayUnits();

        Profiles.Clear();
        UnitNameHeading = request.UnitName;
        LogoSource = request.LogoSource;
        EquipmentSummary = "Equipment: -";
        SpecialSkillsSummary = "Special Skills: -";
        EquipmentSummaryFormatted = UnitDetailFormatting.NamedSummary("Equipment", [], null, null, UnitDetailFormatting.EquipmentColor);
        SpecialSkillsSummaryFormatted = UnitDetailFormatting.NamedSummary("Special Skills", [], null, null, UnitDetailFormatting.SkillsColor);

        try
        {
            ProfilesStatus = "Loading profiles...";
            var unit = armyData.GetUnit(request.FactionId, request.UnitId, cancellationToken);
            var filters = armyData.GetFactionSnapshot(request.FactionId, cancellationToken)?.Filters ?? ArmyFilterCatalog.Empty;
            _equipmentLookup = filters.Names(ArmyFilterSection.Equip);
            _equipmentLinks = filters.Links(ArmyFilterSection.Equip);
            _skillsLookup = filters.Names(ArmyFilterSection.Skills);
            _skillsLinks = filters.Links(ArmyFilterSection.Skills);

            if (unit?.ProfileGroups is not { IsEmpty: false } profileGroups)
            {
                ProfilesStatus = "No profiles found for this unit.";
                return;
            }

            ShowStatsOfFirstProfile(profileGroups);
            ShowTraitIcons(profileGroups, filters);
            AddConfigurations(profileGroups, filters, request);
            ProfilesStatus = Profiles.Count == 0 ? "No configurations found for this unit." : $"{Profiles.Count} configurations loaded.";
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Loading unit profiles failed: {ex.Message}");
            ProfilesStatus = $"Failed to load profiles: {ex.Message}";
        }
    }

    /// <summary>Adds the chosen configuration's own equipment and skills to the shared summaries.</summary>
    public void ApplySelectedProfileTopSummaries(ViewerProfileItem profile)
    {
        var equipment = UnitDetailText.MergeSummaryAndUnique(EquipmentSummary, profile.UniqueEquipment);
        var skills = UnitDetailText.MergeSummaryAndUnique(SpecialSkillsSummary, profile.UniqueSkills);

        EquipmentSummary = $"Equipment: {equipment}";
        SpecialSkillsSummary = $"Special Skills: {skills}";
        EquipmentSummaryFormatted = UnitDetailFormatting.NamedSummary(
            "Equipment", UnitDetailText.SummaryValues(equipment), _equipmentLookup, _equipmentLinks, UnitDetailFormatting.EquipmentColor);
        SpecialSkillsSummaryFormatted = UnitDetailFormatting.NamedSummary(
            "Special Skills", UnitDetailText.SummaryValues(skills), _skillsLookup, _skillsLinks, UnitDetailFormatting.SkillsColor);
    }

    public void SetOrderTypeIconState(bool showRegular, bool showIrregular)
    {
        ShowRegularOrderIcon = showRegular;
        ShowIrregularOrderIcon = showIrregular;
    }

    public void SetTechTraitIconState(bool showCube, bool showCube2, bool showHackable)
    {
        ShowCubeIcon = showCube;
        ShowCube2Icon = showCube2;
        ShowHackableIcon = showHackable;
    }

    public void ApplyTacticalAwarenessOverride(bool hasTacticalAwareness) =>
        ShowTacticalAwarenessIcon = ShowTacticalAwarenessIcon || hasTacticalAwareness;

    public void ApplyHackableOverrideFromCurrentConfiguration(string? currentEquipment, string? currentSkills) =>
        ShowHackableIcon = ShowHackableIcon || UnitDetailText.ContainsHackable(currentEquipment, currentSkills);

    /// <summary>Shows an augmented stat value (e.g. "PH=14") in place of the profile's.</summary>
    public void ApplyAugmentStatOverride(StatOverride statOverride)
    {
        var value = statOverride.Value.ToString(CultureInfo.InvariantCulture);
        switch (statOverride.Stat)
        {
            case Stat.Cc: UnitCc = value; break;
            case Stat.Bs: UnitBs = value; break;
            case Stat.Ph: UnitPh = value; break;
            case Stat.Wip: UnitWip = value; break;
            case Stat.Arm: UnitArm = value; break;
            case Stat.Bts: UnitBts = value; break;
            case Stat.S: UnitS = value; break;
            case Stat.Vita or Stat.Str or Stat.W: UnitVitality = value; break;
        }
    }

    public void ApplyCaptainStatBonuses(CaptainStatBonuses bonuses)
    {
        UnitCc = CaptainStatBonuses.Apply(UnitCc, bonuses.Cc);
        UnitBs = CaptainStatBonuses.Apply(UnitBs, bonuses.Bs);
        UnitPh = CaptainStatBonuses.Apply(UnitPh, bonuses.Ph);
        UnitWip = CaptainStatBonuses.Apply(UnitWip, bonuses.Wip);
        UnitArm = CaptainStatBonuses.Apply(UnitArm, bonuses.Arm);
        UnitBts = CaptainStatBonuses.Apply(UnitBts, bonuses.Bts);
        UnitVitality = CaptainStatBonuses.Apply(UnitVitality, bonuses.Vitality);
    }

    /// <summary>Shows a saved trooper's stat line; a lieutenant shows no regular-order icon.</summary>
    public void ApplySavedUnitSnapshot(
        string unitNameHeading,
        string mov,
        string cc,
        string bs,
        string ph,
        string wip,
        string arm,
        string bts,
        string vitalityHeader,
        string vitality,
        string s,
        bool isLieutenant)
    {
        static string OrDash(string value) => string.IsNullOrWhiteSpace(value) ? "-" : value;

        UnitNameHeading = string.IsNullOrWhiteSpace(unitNameHeading) ? "Unit" : unitNameHeading;
        _unitMovement = Movement.TryParse(mov, out var movement) ? movement : null;
        UnitMov = _unitMovement is null ? OrDash(mov) : armyData.FormatMovement(_unitMovement);

        UnitCc = OrDash(cc);
        UnitBs = OrDash(bs);
        UnitPh = OrDash(ph);
        UnitWip = OrDash(wip);
        UnitArm = OrDash(arm);
        UnitBts = OrDash(bts);
        UnitVitalityHeader = string.IsNullOrWhiteSpace(vitalityHeader) ? Stat.Vita.Key() : vitalityHeader;
        UnitVitality = OrDash(vitality);
        UnitS = OrDash(s);
        UnitAva = "-";

        EquipmentSummary = "Equipment: -";
        SpecialSkillsSummary = "Special Skills: -";
        EquipmentSummaryFormatted = UnitDetailFormatting.NamedSummary("Equipment", [], null, null, UnitDetailFormatting.EquipmentColor);
        SpecialSkillsSummaryFormatted = UnitDetailFormatting.NamedSummary("Special Skills", [], null, null, UnitDetailFormatting.SkillsColor);

        ShowRegularOrderIcon = !isLieutenant;
        ShowIrregularOrderIcon = ShowImpetuousIcon = ShowTacticalAwarenessIcon = false;
        ShowCubeIcon = ShowCube2Icon = ShowHackableIcon = false;
    }

    private void ResetStats()
    {
        _unitMovement = null;
        ShowRegularOrderIcon = ShowIrregularOrderIcon = ShowImpetuousIcon = ShowTacticalAwarenessIcon = false;
        ShowCubeIcon = ShowCube2Icon = ShowHackableIcon = false;
        ImpetuousIconUrl = TacticalAwarenessIconUrl = CubeIconUrl = Cube2IconUrl = HackableIconUrl = null;
        UnitMov = UnitCc = UnitBs = UnitPh = UnitWip = UnitArm = UnitBts = UnitVitality = UnitS = UnitAva = "-";
        UnitVitalityHeader = Stat.Vita.Key();
    }

    private void ShowStatsOfFirstProfile(UnitProfileGroups profileGroups)
    {
        ResetStats();

        var profile = profileGroups.Groups.SelectMany(group => group.Profiles).FirstOrDefault();
        if (profile is null)
        {
            return;
        }

        _unitMovement = profile.Movement;
        UnitMov = armyData.FormatMovement(_unitMovement);
        UnitCc = UnitProfile.StatText(profile.Cc);
        UnitBs = UnitProfile.StatText(profile.Bs);
        UnitPh = UnitProfile.StatText(profile.Ph);
        UnitWip = UnitProfile.StatText(profile.Wip);
        UnitArm = UnitProfile.StatText(profile.Arm);
        UnitBts = UnitProfile.StatText(profile.Bts);
        UnitS = UnitProfile.StatText(profile.S);

        // The viewer shows AVA as sent (including -1) and labels structure troopers STR.
        UnitAva = profile.Ava switch
        {
            null => "-",
            255 => "T",
            var ava => ava.Value.ToString()
        };
        UnitVitalityHeader = (profile.Str ? Stat.Str : Stat.Vita).Key();
        UnitVitality = UnitProfile.StatText(profile.W);
    }

    private void ShowTraitIcons(UnitProfileGroups profileGroups, ArmyFilterCatalog filters)
    {
        var charsLookup = filters.Names(ArmyFilterSection.Chars);
        var charsLinks = filters.Links(ArmyFilterSection.Chars);

        // A trait's wiki link is looked for among characteristics, then equipment, then skills.
        string? LinkFor(Func<string, bool> isTrait) =>
            UnitDetailText.FirstLinkWhere(charsLookup, charsLinks, isTrait)
            ?? UnitDetailText.FirstLinkWhere(_equipmentLookup, _equipmentLinks, isTrait)
            ?? UnitDetailText.FirstLinkWhere(_skillsLookup, _skillsLinks, isTrait);

        var orderTraits = UnitTraits.ReadOrderTraits(profileGroups);
        ShowIrregularOrderIcon = orderTraits.HasIrregular;
        ShowRegularOrderIcon = !orderTraits.HasIrregular && orderTraits.HasRegular;
        ShowImpetuousIcon = orderTraits.HasImpetuous;
        ShowTacticalAwarenessIcon = orderTraits.HasTacticalAwareness;
        ImpetuousIconUrl = orderTraits.HasImpetuous
            ? UnitDetailText.FirstLinkWhere(_skillsLookup, _skillsLinks, name => name.Contains("impetuous", StringComparison.OrdinalIgnoreCase))
            : null;
        TacticalAwarenessIconUrl = orderTraits.HasTacticalAwareness
            ? UnitDetailText.FirstLinkWhere(_skillsLookup, _skillsLinks, name => name.Contains("tactical", StringComparison.OrdinalIgnoreCase))
            : null;

        var techTraits = UnitTraits.ReadTechTraits(profileGroups, _equipmentLookup, _skillsLookup, charsLookup);
        ShowCubeIcon = techTraits.HasCube;
        ShowCube2Icon = techTraits.HasCube2;
        ShowHackableIcon = techTraits.HasHackable;
        CubeIconUrl = techTraits.HasCube ? LinkFor(UnitDetailText.IsCubeName) : null;
        Cube2IconUrl = techTraits.HasCube2 ? LinkFor(UnitDetailText.IsCube2Name) : null;
        HackableIconUrl = techTraits.HasHackable ? LinkFor(name => name.Contains("hackable", StringComparison.OrdinalIgnoreCase)) : null;
    }

    // Lists each distinct configuration; the equipment and skills every profile has go in the summaries.
    private void AddConfigurations(UnitProfileGroups profileGroups, ArmyFilterCatalog filters, UnitDetailRequest request)
    {
        var weaponsLookup = filters.Names(ArmyFilterSection.Weapons);
        var weaponsLinks = filters.Links(ArmyFilterSection.Weapons);
        var peripheralLookup = filters.Names(ArmyFilterSection.Peripheral);
        var peripheralLinks = filters.Links(ArmyFilterSection.Peripheral);
        var entryNames = new ProfileEntryNames(filters.Extras, ShowUnitsInInches);

        HashSet<string>? commonEquip = null;
        HashSet<string>? commonSkills = null;
        var hasControllerGroups = profileGroups.Groups.Any(profileGroups.IsControllerGroup);
        var seenConfigurations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in profileGroups.Groups)
        {
            if (hasControllerGroups && !profileGroups.IsControllerGroup(group))
            {
                continue;
            }

            var groupName = group.Isc ?? string.Empty;

            foreach (var profile in group.Profiles)
            {
                var equip = entryNames.InApiOrder(profile.Equip, _equipmentLookup).Select(x => x.Name);
                var skills = entryNames.InApiOrder(profile.Skills, _skillsLookup).Select(x => x.Name);
                if (commonEquip is null)
                {
                    commonEquip = new HashSet<string>(equip, StringComparer.OrdinalIgnoreCase);
                    commonSkills = new HashSet<string>(skills, StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    commonEquip.IntersectWith(equip);
                    commonSkills!.IntersectWith(skills);
                }
            }

            foreach (var option in group.Options)
            {
                var isLieutenant = option.IsLieutenant(_skillsLookup);
                if (request.LieutenantOnly && !isLieutenant)
                {
                    continue;
                }

                var swc = string.IsNullOrWhiteSpace(option.Swc) ? "-" : option.Swc;
                if (request.MercsOnly && ProfileValues.IsPositiveSwc(swc))
                {
                    continue;
                }

                var optionName = string.IsNullOrWhiteSpace(option.Name) ? groupName : option.Name;
                var displayName = ViewerOptionNames.WithDetailTags(option, optionName, _equipmentLookup, _skillsLookup);
                var weapons = entryNames.InApiOrder(profileGroups.GetEntriesWithIncludes(option, ProfileEntryKind.Weapons), weaponsLookup);
                var rangedEntries = weapons.Where(x => !UnitDetailText.IsMeleeWeaponName(x.Name)).ToList();
                var meleeEntries = weapons.Where(x => UnitDetailText.IsMeleeWeaponName(x.Name)).ToList();
                var equipmentEntries = entryNames.InApiOrder(profileGroups.GetEntriesWithIncludes(option, ProfileEntryKind.Equip), _equipmentLookup).ToList();
                var skillEntries = UnitDetailText.ConfigurationSkillEntries(
                    entryNames.InApiOrder(profileGroups.GetEntriesWithIncludes(option, ProfileEntryKind.Skills), _skillsLookup));
                var peripheralEntries = entryNames.CountedWithIds(profileGroups.GetDisplayPeripheralEntries(group, option), peripheralLookup);

                var rangedWeapons = UnitDetailText.JoinOrDash(rangedEntries.Select(x => x.Name));
                var meleeWeapons = UnitDetailText.JoinOrDash(meleeEntries.Select(x => x.Name));
                var uniqueEquipment = UnitDetailText.JoinOrDash(equipmentEntries.Select(x => x.Name));
                var uniqueSkills = UnitDetailText.JoinOrDash(skillEntries.Select(x => x.Name));
                var peripherals = UnitDetailText.JoinOrDash(peripheralEntries.Select(x => x.Name));
                var cost = profileGroups.GetAdjustedCost(group, option);

                if (!seenConfigurations.Add($"{groupName}|{displayName}|{rangedWeapons}|{meleeWeapons}|{uniqueEquipment}|{uniqueSkills}|{peripherals}|{swc}|{cost}"))
                {
                    continue;
                }

                Profiles.Add(new ViewerProfileItem
                {
                    GroupName = groupName,
                    Name = displayName,
                    ProfileKey = $"{groupName}|{optionName}|{cost}|{swc}|lt:{(isLieutenant ? 1 : 0)}",
                    IsLieutenant = isLieutenant,
                    NameFormatted = UnitDetailFormatting.Name(displayName),
                    RangedWeapons = rangedWeapons,
                    RangedWeaponsFormatted = UnitDetailFormatting.Lines(UnitDetailText.LinkedLines(rangedEntries, weaponsLinks), UnitDetailFormatting.RangedColor),
                    MeleeWeapons = meleeWeapons,
                    MeleeWeaponsFormatted = UnitDetailFormatting.Lines(UnitDetailText.LinkedLines(meleeEntries, weaponsLinks), UnitDetailFormatting.MeleeColor),
                    UniqueEquipment = uniqueEquipment,
                    UniqueEquipmentFormatted = UnitDetailFormatting.Lines(UnitDetailText.LinkedLines(equipmentEntries, _equipmentLinks), UnitDetailFormatting.EquipmentColor),
                    UniqueSkills = uniqueSkills,
                    UniqueSkillsFormatted = UnitDetailFormatting.Lines(UnitDetailText.LinkedLines(skillEntries, _skillsLinks), UnitDetailFormatting.SkillsColor),
                    Peripherals = peripherals,
                    PeripheralsFormatted = UnitDetailFormatting.Lines(UnitDetailText.LinkedLines(peripheralEntries, peripheralLinks), UnitDetailFormatting.PeripheralColor),
                    Swc = swc,
                    SwcDisplay = request.MercsOnly ? string.Empty : $"SWC {swc}",
                    Cost = cost,
                    ShowProfileTacticalAwarenessIcon = !ShowTacticalAwarenessIcon &&
                        skillEntries.Any(x => x.Name.Contains("tactical awareness", StringComparison.OrdinalIgnoreCase))
                });
            }
        }

        var stableEquip = commonEquip ?? [];
        var stableSkills = commonSkills ?? [];
        EquipmentSummary = UnitDetailText.NamedSummary("Equipment", stableEquip);
        SpecialSkillsSummary = UnitDetailText.NamedSummary("Special Skills", stableSkills);
        EquipmentSummaryFormatted = UnitDetailFormatting.NamedSummary("Equipment", stableEquip, _equipmentLookup, _equipmentLinks, UnitDetailFormatting.EquipmentColor);
        SpecialSkillsSummaryFormatted = UnitDetailFormatting.NamedSummary("Special Skills", stableSkills, _skillsLookup, _skillsLinks, UnitDetailFormatting.SkillsColor);
    }
}
