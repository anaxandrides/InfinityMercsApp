using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Presentation.Company.Selection;
using InfinityMercsApp.Presentation.Themes;
using InfinityMercsApp.Presentation.Units;
using InfinityMercsApp.Services.CompanyProfiles;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.CompanySelection.Units;
using InfinityMercsApp.ViewModels.Components;

namespace InfinityMercsApp.ViewModels.Company.Selection;

/// <summary>
/// What the unit-detail display shows for the unit picked on a company selection screen: its heading,
/// statline, trait icons, shared equipment and skills, a peripheral's statline, and the faction colours.
/// It also keeps the unit's profile groups and filters, which adding a profile to the company needs.
/// </summary>
public sealed partial class CompanyUnitDetail : ObservableObject, IUnitDisplay
{
    public const string NoUnitHeading = "Select a unit";

    private static readonly Color EquipmentColor = Color.FromArgb("#B5C0CE");
    private static readonly Color SkillsColor = Color.FromArgb("#F59E0B");

    // The peripheral's MOV in centimetres, kept to re-format it when the display units change.
    private int? _peripheralMoveFirstCm;
    private int? _peripheralMoveSecondCm;

    [ObservableProperty] public partial string UnitNameHeading { get; set; } = NoUnitHeading;
    [ObservableProperty] public partial string? LogoSource { get; set; }

    [ObservableProperty] public partial UnitStatLine Stats { get; set; } = UnitStatLine.Empty;
    [ObservableProperty] public partial UnitHeaderIcons Icons { get; set; } = UnitHeaderIcons.None;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPeripheral))]
    public partial PeripheralStatBlock? Peripheral { get; set; }

    public bool HasPeripheral => Peripheral is not null;

    [ObservableProperty] public partial string EquipmentSummary { get; set; } = "Equipment: -";
    [ObservableProperty] public partial string SpecialSkillsSummary { get; set; } = "Special Skills: -";
    [ObservableProperty] public partial FormattedString EquipmentSummaryFormatted { get; set; } = new();
    [ObservableProperty] public partial FormattedString SpecialSkillsSummaryFormatted { get; set; } = new();

    [ObservableProperty] public partial bool ShowUnitsInInches { get; set; } = true;

    [ObservableProperty] public partial CompanyHeaderColors HeaderColors { get; set; } = HeaderColorsFor(null);

    /// <summary>The selected unit's MOV in centimetres, kept to re-format it when the display units change.</summary>
    public int? UnitMoveFirstCm { get; private set; }

    public int? UnitMoveSecondCm { get; private set; }

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
        Icons = UnitHeaderIcons.None;
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
    public void SetOrderTraits(bool hasRegular, bool hasIrregular, bool hasImpetuous, bool hasTacticalAwareness) =>
        Icons = Icons with
        {
            Irregular = hasIrregular,
            Regular = !hasIrregular && hasRegular,
            Impetuous = hasImpetuous,
            TacticalAwareness = hasTacticalAwareness
        };

    public void SetTechTraits(bool hasCube, bool hasCube2, bool hasHackable) =>
        Icons = Icons with { Cube = hasCube, Cube2 = hasCube2, Hackable = hasHackable };

    public void ResetStats()
    {
        UnitMoveFirstCm = UnitMoveSecondCm = null;
        Stats = UnitStatLine.Empty;
        ResetPeripheral();
    }

    /// <summary>Shows a profile's statline.</summary>
    public void ApplyStats(UnitProfile profile, Func<int?, int?, string> formatMove)
    {
        var projection = CompanyUnitDetailsShared.BuildUnitStatProjection(profile, formatMove);
        UnitMoveFirstCm = projection.MoveFirstCm;
        UnitMoveSecondCm = projection.MoveSecondCm;
        Stats = new UnitStatLine
        {
            Mov = projection.Mov,
            Cc = projection.Cc,
            Bs = projection.Bs,
            Ph = projection.Ph,
            Wip = projection.Wip,
            Arm = projection.Arm,
            Bts = projection.Bts,
            S = projection.S,
            Ava = projection.Ava,
            VitalityHeader = projection.VitalityHeader,
            Vitality = projection.Vitality
        };
    }

    /// <summary>Shows the first profile's statline, or none when the unit has no profiles.</summary>
    public void ApplyFirstProfileStats(UnitProfileGroups profileGroups, Func<int?, int?, string> formatMove) =>
        CompanyUnitDetailsShared.PopulateUnitStatsFromFirstProfile(profileGroups, ResetStats, profile => ApplyStats(profile, formatMove));

    public void ResetPeripheral()
    {
        _peripheralMoveFirstCm = _peripheralMoveSecondCm = null;
        Peripheral = null;
    }

    public void ApplyPeripheral(PeripheralMercsCompanyStats stats, Func<int?, int?, string> formatMove)
    {
        _peripheralMoveFirstCm = stats.MoveFirstCm;
        _peripheralMoveSecondCm = stats.MoveSecondCm;
        Peripheral = new PeripheralStatBlock(
            stats.NameHeading,
            new UnitStatLine
            {
                Mov = formatMove(stats.MoveFirstCm, stats.MoveSecondCm),
                Cc = stats.Cc,
                Bs = stats.Bs,
                Ph = stats.Ph,
                Wip = stats.Wip,
                Arm = stats.Arm,
                Bts = stats.Bts,
                VitalityHeader = stats.VitalityHeader,
                Vitality = stats.Vitality,
                S = stats.S,
                Ava = stats.Ava
            },
            stats.Equipment,
            stats.Skills,
            Named("Equipment", CompanyProfileText.SplitDisplayLine(stats.Equipment), EquipmentColor),
            Named("Skills", CompanyProfileText.SplitDisplayLine(stats.Skills), SkillsColor));
    }

    /// <summary>Re-formats MOV after the inches/centimetres preference changed.</summary>
    public void RefreshMoves(Func<int?, int?, string> formatMove)
    {
        Stats = Stats with { Mov = formatMove(UnitMoveFirstCm, UnitMoveSecondCm) };
        if (Peripheral is { } peripheral)
        {
            Peripheral = peripheral with
            {
                Stats = peripheral.Stats with { Mov = formatMove(_peripheralMoveFirstCm, _peripheralMoveSecondCm) }
            };
        }
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
        CompanySelectionVisualThemeWorkflow.GetHeaderColors(vanillaFactionName);

    private static FormattedString Named(string label, IEnumerable<string> values, Color color) =>
        CompanyProfileFormatting.BuildNamedSummaryFormatted(label, values, color);
}
