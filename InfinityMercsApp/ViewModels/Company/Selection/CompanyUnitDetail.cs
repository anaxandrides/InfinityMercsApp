using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Services.CompanyProfiles;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.CompanySelection.Units;
using InfinityMercsApp.Services.CompanySelection.Visuals;
using InfinityMercsApp.ViewModels.Company.Selection.Workflows;
using InfinityMercsApp.Views.Controls;

namespace InfinityMercsApp.ViewModels.Company.Selection;

/// <summary>
/// What the unit-detail display shows for the unit picked on a company selection screen: its heading,
/// statline, trait icons, shared equipment and skills, a peripheral's statline, and the faction colours.
/// It also keeps the unit's profile groups and filters, which adding a profile to the company needs.
/// </summary>
public sealed partial class CompanyUnitDetail : ObservableObject
{
    public const string NoUnitHeading = "Select a unit";

    private static readonly Color EquipmentColor = Color.FromArgb("#B5C0CE");
    private static readonly Color SkillsColor = Color.FromArgb("#F59E0B");

    [ObservableProperty] private string _unitNameHeading = NoUnitHeading;
    [ObservableProperty] private string? _logoSource;

    [ObservableProperty] private int? _unitMoveFirstCm;
    [ObservableProperty] private int? _unitMoveSecondCm;
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

    [ObservableProperty] private bool _hasPeripheralStatBlock;
    [ObservableProperty] private string _peripheralNameHeading = string.Empty;
    [ObservableProperty] private int? _peripheralMoveFirstCm;
    [ObservableProperty] private int? _peripheralMoveSecondCm;
    [ObservableProperty] private string _peripheralMov = "-";
    [ObservableProperty] private string _peripheralCc = "-";
    [ObservableProperty] private string _peripheralBs = "-";
    [ObservableProperty] private string _peripheralPh = "-";
    [ObservableProperty] private string _peripheralWip = "-";
    [ObservableProperty] private string _peripheralArm = "-";
    [ObservableProperty] private string _peripheralBts = "-";
    [ObservableProperty] private string _peripheralVitalityHeader = Stat.Vita.Key();
    [ObservableProperty] private string _peripheralVitality = "-";
    [ObservableProperty] private string _peripheralS = "-";
    [ObservableProperty] private string _peripheralAva = "-";
    [ObservableProperty] private string _peripheralEquipment = "-";
    [ObservableProperty] private string _peripheralSkills = "-";
    [ObservableProperty] private FormattedString _peripheralEquipmentFormatted = Named("Equipment", [], EquipmentColor);
    [ObservableProperty] private FormattedString _peripheralSkillsFormatted = Named("Skills", [], SkillsColor);

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

    [ObservableProperty] private bool _showUnitsInInches = true;

    [ObservableProperty] private CompanyHeaderColors _headerColors = HeaderColorsFor(null);

    /// <summary>The selected unit's profile groups; empty when nothing is selected.</summary>
    public UnitProfileGroups ProfileGroups { get; private set; } = UnitProfileGroups.Empty;

    /// <summary>The army filters the selected unit's names come from.</summary>
    public ArmyFilterCatalog Filters { get; private set; } = ArmyFilterCatalog.Empty;

    /// <summary>Equipment every visible profile shares; a roster entry starts from it.</summary>
    public IReadOnlyList<string> CommonEquipment { get; private set; } = [];

    /// <summary>Skills every visible profile shares; a roster entry starts from it.</summary>
    public IReadOnlyList<string> CommonSkills { get; private set; } = [];

    /// <summary>Whether lieutenant entries in the skills summary are highlighted (Spec Ops captains).</summary>
    public bool HighlightLieutenant { get; private set; }

    /// <summary>Back to "Select a unit", optionally keeping the logo and faction colours.</summary>
    public void Reset(bool clearLogo = true, bool resetHeaderColors = true)
    {
        UnitNameHeading = NoUnitHeading;
        if (resetHeaderColors)
        {
            HeaderColors = HeaderColorsFor(null);
        }

        if (clearLogo)
        {
            LogoSource = null;
        }

        SetProfileData(UnitProfileGroups.Empty, ArmyFilterCatalog.Empty);
        ResetStats();
        EquipmentSummary = "Equipment: -";
        SpecialSkillsSummary = "Special Skills: -";
        SetCommonEquipmentAndSkills([], [], highlightLieutenant: false);
        ShowRegularOrderIcon = ShowIrregularOrderIcon = ShowImpetuousIcon = ShowTacticalAwarenessIcon = false;
        ShowCubeIcon = ShowCube2Icon = ShowHackableIcon = false;
    }

    public void SetProfileData(UnitProfileGroups profileGroups, ArmyFilterCatalog filters)
    {
        ProfileGroups = profileGroups;
        Filters = filters;
    }

    public void SetCommonEquipmentAndSkills(IReadOnlyList<string> equipment, IReadOnlyList<string> skills, bool highlightLieutenant)
    {
        CommonEquipment = equipment;
        CommonSkills = skills;
        HighlightLieutenant = highlightLieutenant;
        RefreshSummaries();
    }

    /// <summary>Order icons: irregular wins over regular.</summary>
    public void SetOrderTraits(bool hasRegular, bool hasIrregular, bool hasImpetuous, bool hasTacticalAwareness)
    {
        ShowIrregularOrderIcon = hasIrregular;
        ShowRegularOrderIcon = !hasIrregular && hasRegular;
        ShowImpetuousIcon = hasImpetuous;
        ShowTacticalAwarenessIcon = hasTacticalAwareness;
    }

    public void SetTechTraits(bool hasCube, bool hasCube2, bool hasHackable)
    {
        ShowCubeIcon = hasCube;
        ShowCube2Icon = hasCube2;
        ShowHackableIcon = hasHackable;
    }

    public void ResetStats()
    {
        UnitMoveFirstCm = UnitMoveSecondCm = null;
        UnitMov = UnitCc = UnitBs = UnitPh = UnitWip = UnitArm = UnitBts = UnitVitality = UnitS = UnitAva = "-";
        UnitVitalityHeader = Stat.Vita.Key();
        ResetPeripheral();
    }

    /// <summary>Shows a profile's statline.</summary>
    public void ApplyStats(UnitProfile profile, Func<int?, int?, string> formatMove)
    {
        var projection = CompanyUnitDetailsShared.BuildUnitStatProjection(profile, formatMove);
        UnitMoveFirstCm = projection.MoveFirstCm;
        UnitMoveSecondCm = projection.MoveSecondCm;
        UnitMov = projection.Mov;
        UnitCc = projection.Cc;
        UnitBs = projection.Bs;
        UnitPh = projection.Ph;
        UnitWip = projection.Wip;
        UnitArm = projection.Arm;
        UnitBts = projection.Bts;
        UnitS = projection.S;
        UnitAva = projection.Ava;
        UnitVitalityHeader = projection.VitalityHeader;
        UnitVitality = projection.Vitality;
    }

    /// <summary>Shows the first profile's statline, or none when the unit has no profiles.</summary>
    public void ApplyFirstProfileStats(UnitProfileGroups profileGroups, Func<int?, int?, string> formatMove) =>
        CompanyUnitDetailsShared.PopulateUnitStatsFromFirstProfile(profileGroups, ResetStats, profile => ApplyStats(profile, formatMove));

    public void ResetPeripheral()
    {
        PeripheralMoveFirstCm = PeripheralMoveSecondCm = null;
        HasPeripheralStatBlock = false;
        PeripheralNameHeading = string.Empty;
        PeripheralMov = PeripheralCc = PeripheralBs = PeripheralPh = PeripheralWip = PeripheralArm = PeripheralBts = "-";
        PeripheralVitality = PeripheralS = PeripheralAva = PeripheralEquipment = PeripheralSkills = "-";
        PeripheralVitalityHeader = Stat.Vita.Key();
        PeripheralEquipmentFormatted = Named("Equipment", [], EquipmentColor);
        PeripheralSkillsFormatted = Named("Skills", [], SkillsColor);
    }

    public void ApplyPeripheral(PeripheralMercsCompanyStats stats, Func<int?, int?, string> formatMove)
    {
        PeripheralMoveFirstCm = stats.MoveFirstCm;
        PeripheralMoveSecondCm = stats.MoveSecondCm;
        PeripheralMov = formatMove(stats.MoveFirstCm, stats.MoveSecondCm);
        PeripheralNameHeading = stats.NameHeading;
        PeripheralCc = stats.Cc;
        PeripheralBs = stats.Bs;
        PeripheralPh = stats.Ph;
        PeripheralWip = stats.Wip;
        PeripheralArm = stats.Arm;
        PeripheralBts = stats.Bts;
        PeripheralVitalityHeader = stats.VitalityHeader;
        PeripheralVitality = stats.Vitality;
        PeripheralS = stats.S;
        PeripheralAva = stats.Ava;
        PeripheralEquipment = stats.Equipment;
        PeripheralSkills = stats.Skills;
        PeripheralEquipmentFormatted = Named("Equipment", CompanyProfileText.SplitDisplayLine(stats.Equipment), EquipmentColor);
        PeripheralSkillsFormatted = Named("Skills", CompanyProfileText.SplitDisplayLine(stats.Skills), SkillsColor);
        HasPeripheralStatBlock = true;
    }

    /// <summary>Re-formats MOV after the inches/centimetres preference changed.</summary>
    public void RefreshMoves(Func<int?, int?, string> formatMove)
    {
        UnitMov = formatMove(UnitMoveFirstCm, UnitMoveSecondCm);
        PeripheralMov = formatMove(PeripheralMoveFirstCm, PeripheralMoveSecondCm);
    }

    /// <summary>The faction's header colours; the summaries are re-coloured to read on the new background.</summary>
    public void ApplyHeaderColors(string? vanillaFactionName)
    {
        HeaderColors = HeaderColorsFor(vanillaFactionName);
        RefreshSummaries();
    }

    private void RefreshSummaries()
    {
        var accents = CompanySelectionVisualThemeWorkflow.GetSummaryAccentColorsForSecondaryBackground(HeaderColors.Secondary);
        EquipmentSummaryFormatted = Named("Equipment", CommonEquipment, accents.Equipment);
        SpecialSkillsSummaryFormatted = CompanyProfileFormatting.BuildNamedSummaryFormatted(
            "Special Skills", CommonSkills, accents.Skills, highlightLieutenantPurple: HighlightLieutenant);
    }

    private static CompanyHeaderColors HeaderColorsFor(string? vanillaFactionName) =>
        CompanySelectionVisualThemeWorkflow.GetHeaderColors(
            vanillaFactionName,
            UnitDisplayConfigurationsView.DefaultHeaderPrimaryColor,
            UnitDisplayConfigurationsView.DefaultHeaderSecondaryColor);

    private static FormattedString Named(string label, IEnumerable<string> values, Color color) =>
        CompanyProfileFormatting.BuildNamedSummaryFormatted(label, values, color);
}
