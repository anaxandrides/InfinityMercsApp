using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Models.Army.Traits;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Presentation.Themes;
using InfinityMercsApp.Presentation.Units;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;

namespace InfinityMercsApp.ViewModels.Components;

/// <summary>
/// One unit as shown by the unit-detail display: stat line, trait icons, the equipment and skills all
/// its profiles share, and each configuration. Pages showing a saved trooper overlay its stats instead.
/// </summary>
public partial class UnitDetailViewModel(
    IArmyDataService armyData,
    IAppSettingsProvider? appSettings = null) : ObservableObject, IUnitDisplay
{
    private const string NoUnit = "Select a unit";

    private Movement? _unitMovement;
    private IReadOnlyDictionary<int, string> _equipmentLookup = new Dictionary<int, string>();
    private IReadOnlyDictionary<int, string> _equipmentLinks = new Dictionary<int, string>();
    private IReadOnlyDictionary<int, string> _skillsLookup = new Dictionary<int, string>();
    private IReadOnlyDictionary<int, string> _skillsLinks = new Dictionary<int, string>();

    public ObservableCollection<ViewerProfileItem> Profiles { get; } = [];

    [ObservableProperty] public partial string ProfilesStatus { get; set; } = "Select a unit.";
    [ObservableProperty] public partial string UnitNameHeading { get; set; } = NoUnit;
    [ObservableProperty] public partial string? LogoSource { get; set; }

    [ObservableProperty] public partial UnitStatLine Stats { get; set; } = UnitStatLine.Empty;
    [ObservableProperty] public partial UnitHeaderIcons Icons { get; set; } = UnitHeaderIcons.None;
    [ObservableProperty] public partial CompanyHeaderColors HeaderColors { get; set; } = CompanyHeaderColors.Default;

    /// <summary>A unit read from army data shows no separate peripheral statline.</summary>
    public PeripheralStatBlock? Peripheral => null;

    public bool HasPeripheral => false;

    [ObservableProperty] public partial string EquipmentSummary { get; set; } = "Equipment: -";
    [ObservableProperty] public partial string SpecialSkillsSummary { get; set; } = "Special Skills: -";
    [ObservableProperty] public partial FormattedString EquipmentSummaryFormatted { get; set; } = new();
    [ObservableProperty] public partial FormattedString SpecialSkillsSummaryFormatted { get; set; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowUnitsInCentimeters))]
    public partial bool ShowUnitsInInches { get; set; } = true;

    public bool ShowUnitsInCentimeters
    {
        get => !ShowUnitsInInches;
        set => ShowUnitsInInches = !value;
    }

    partial void OnShowUnitsInInchesChanged(bool value) => Stats = Stats with { Mov = armyData.FormatMovement(_unitMovement) };

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

    /// <summary>Shows an augmented stat value (e.g. "PH=14") in place of the profile's.</summary>
    public void ApplyAugmentStatOverride(StatOverride statOverride)
    {
        var value = statOverride.Value.ToString(CultureInfo.InvariantCulture);
        Stats = statOverride.Stat switch
        {
            Stat.Cc => Stats with { Cc = value },
            Stat.Bs => Stats with { Bs = value },
            Stat.Ph => Stats with { Ph = value },
            Stat.Wip => Stats with { Wip = value },
            Stat.Arm => Stats with { Arm = value },
            Stat.Bts => Stats with { Bts = value },
            Stat.S => Stats with { S = value },
            Stat.Vita or Stat.Str or Stat.W => Stats with { Vitality = value },
            _ => Stats
        };
    }

    /// <summary>
    /// Shows a saved trooper's stat line; blank values show as "-" and a MOV the army data can read is
    /// shown in the current units. The header icons are cleared for the caller to set.
    /// </summary>
    public void ApplySavedUnitSnapshot(string unitNameHeading, UnitStatLine stats)
    {
        static string OrDash(string value) => string.IsNullOrWhiteSpace(value) ? "-" : value;

        UnitNameHeading = string.IsNullOrWhiteSpace(unitNameHeading) ? "Unit" : unitNameHeading;
        _unitMovement = Movement.TryParse(stats.Mov, out var movement) ? movement : null;
        Stats = new UnitStatLine
        {
            Mov = _unitMovement is null ? OrDash(stats.Mov) : armyData.FormatMovement(_unitMovement),
            Cc = OrDash(stats.Cc),
            Bs = OrDash(stats.Bs),
            Ph = OrDash(stats.Ph),
            Wip = OrDash(stats.Wip),
            Arm = OrDash(stats.Arm),
            Bts = OrDash(stats.Bts),
            VitalityHeader = string.IsNullOrWhiteSpace(stats.VitalityHeader) ? Stat.Vita.Key() : stats.VitalityHeader,
            Vitality = OrDash(stats.Vitality),
            S = OrDash(stats.S)
        };

        EquipmentSummary = "Equipment: -";
        SpecialSkillsSummary = "Special Skills: -";
        EquipmentSummaryFormatted = UnitDetailFormatting.NamedSummary("Equipment", [], null, null, UnitDetailFormatting.EquipmentColor);
        SpecialSkillsSummaryFormatted = UnitDetailFormatting.NamedSummary("Special Skills", [], null, null, UnitDetailFormatting.SkillsColor);
        Icons = UnitHeaderIcons.None;
    }

    private void ResetStats()
    {
        _unitMovement = null;
        Icons = UnitHeaderIcons.None;
        Stats = UnitStatLine.Empty;
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
        Stats = new UnitStatLine
        {
            Mov = armyData.FormatMovement(_unitMovement),
            Cc = UnitProfile.StatText(profile.Cc),
            Bs = UnitProfile.StatText(profile.Bs),
            Ph = UnitProfile.StatText(profile.Ph),
            Wip = UnitProfile.StatText(profile.Wip),
            Arm = UnitProfile.StatText(profile.Arm),
            Bts = UnitProfile.StatText(profile.Bts),
            S = UnitProfile.StatText(profile.S),
            // The viewer shows AVA as sent (including -1) and labels structure troopers STR.
            Ava = profile.Ava switch
            {
                null => "-",
                255 => "T",
                var ava => ava.Value.ToString()
            },
            VitalityHeader = (profile.Str ? Stat.Str : Stat.Vita).Key(),
            Vitality = UnitProfile.StatText(profile.W)
        };
    }

    private void ShowTraitIcons(UnitProfileGroups profileGroups, ArmyFilterCatalog filters)
    {
        var orderTraits = UnitTraits.ReadOrderTraits(profileGroups);
        var techTraits = UnitTraits.ReadTechTraits(profileGroups, _equipmentLookup, _skillsLookup, filters.Names(ArmyFilterSection.Chars));
        Icons = new UnitHeaderIcons
        {
            Irregular = orderTraits.HasIrregular,
            Regular = !orderTraits.HasIrregular && orderTraits.HasRegular,
            Impetuous = orderTraits.HasImpetuous,
            TacticalAwareness = orderTraits.HasTacticalAwareness,
            Cube = techTraits.HasCube,
            Cube2 = techTraits.HasCube2,
            Hackable = techTraits.HasHackable
        };
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
                if (commonEquip is null || commonSkills is null)
                {
                    commonEquip = new HashSet<string>(equip, StringComparer.OrdinalIgnoreCase);
                    commonSkills = new HashSet<string>(skills, StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    commonEquip.IntersectWith(equip);
                    commonSkills.IntersectWith(skills);
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
                if (request.MercsOnly && ProfileValues.CostsSwc(swc))
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
                    // A mercenary profile costs no SWC, but one may give it ("+1").
                    SwcDisplay = request.MercsOnly && ProfileValues.SwcGrant(swc) == 0 ? string.Empty : $"SWC {swc}",
                    Cost = cost,
                    ShowProfileTacticalAwarenessIcon = !Icons.TacticalAwareness &&
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
