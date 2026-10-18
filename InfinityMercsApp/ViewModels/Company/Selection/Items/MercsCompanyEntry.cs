using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Services.CompanyProfiles;
using InfinityMercsApp.Services.CompanySelection.StartCompany;
using InfinityMercsApp.Services.Logos;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;

namespace InfinityMercsApp.ViewModels.Company.Selection.Items;

public partial class MercsCompanyEntry : ObservableObject, IViewerListItem, ICompanyMercsEntry
{
    public string Name { get; init; } = string.Empty;
    public string BaseUnitName { get; init; } = string.Empty;
    public FormattedString? NameFormatted { get; init; }
    public string CostDisplay { get; init; } = string.Empty;
    public int CostValue { get; init; }
    public string ProfileKey { get; init; } = string.Empty;
    public bool IsLieutenant { get; init; }

    /// <summary>Recruited as a Spec Ops profile, whose captain upgrades come from the Spec-Ops chart.</summary>
    public bool IsSpecOps { get; init; }

    /// <summary>Already in the company while it recruits: counted for AVA and the company's rules, but not removed or saved again.</summary>
    public bool IsOnRoster { get; init; }

    public bool CanRemove => !IsOnRoster;
    public int SourceUnitId { get; init; }
    public int SourceFactionId { get; init; }
    public int LogoSourceFactionId { get; init; }
    public int LogoSourceUnitId { get; init; }

    public string? CachedLogoPath { get; init; }

    public string? PackagedLogoPath { get; init; }

    public IReadOnlyList<string> LogoCandidates => UnitLogo.Candidates(CachedLogoPath, PackagedLogoPath);

    /// <summary>The statline, e.g. "MOV 10-5 | CC 13 | …".</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSubtitle))]
    public partial string? Subtitle { get; set; }

    public string UnitTypeCode { get; init; } = string.Empty;
    public string SavedEquipment { get; init; } = "-";
    public string SavedSkills { get; init; } = "-";
    public string SavedCharacteristics { get; init; } = "-";
    public string SavedRangedWeapons { get; init; } = "-";
    public string SavedCcWeapons { get; init; } = "-";
    public string SavedEquipmentDisplay => ToCommaSeparatedDisplay(SavedEquipment);
    public string SavedSkillsDisplay => ToCommaSeparatedDisplay(SavedSkills);
    public string SavedRangedWeaponsDisplay => ToCommaSeparatedDisplay(SavedRangedWeapons);
    public string SavedCcWeaponsDisplay => ToCommaSeparatedDisplay(SavedCcWeapons);
    public int? UnitMoveFirstCm { get; init; }
    public int? UnitMoveSecondCm { get; init; }
    public string UnitMoveDisplay { get; set; } = "-";
    public bool HasPeripheralStatBlock { get; init; }
    public string PeripheralNameHeading { get; init; } = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PeripheralSubtitle))]
    public partial string PeripheralMov { get; set; } = "-";

    public string PeripheralCc { get; init; } = "-";
    public string PeripheralBs { get; init; } = "-";
    public string PeripheralPh { get; init; } = "-";
    public string PeripheralWip { get; init; } = "-";
    public string PeripheralArm { get; init; } = "-";
    public string PeripheralBts { get; init; } = "-";
    public string PeripheralVitalityHeader { get; init; } = Stat.Vita.Key();
    public string PeripheralVitality { get; init; } = "-";
    public string PeripheralS { get; init; } = "-";
    public string PeripheralAva { get; init; } = "-";
    public int? PeripheralMoveFirstCm { get; init; }
    public int? PeripheralMoveSecondCm { get; init; }
    public string SavedPeripheralEquipment { get; init; } = "-";
    public string SavedPeripheralSkills { get; init; } = "-";
    public string SavedPeripheralCharacteristics { get; init; } = "-";
    public string SavedPeripheralRangedWeapons { get; init; } = "-";
    public string SavedPeripheralCcWeapons { get; init; } = "-";
    public string PeripheralSubtitle => $"MOV {PeripheralMov} | CC {PeripheralCc} | BS {PeripheralBs} | PH {PeripheralPh} | WIP {PeripheralWip} | ARM {PeripheralArm} | BTS {PeripheralBts} | {PeripheralVitalityHeader} {PeripheralVitality} | S {PeripheralS} | AVA {PeripheralAva}";

    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);

    public FormattedString EquipmentLineFormatted { get; init; } = new();
    public bool HasEquipmentLine { get; init; }
    public FormattedString SkillsLineFormatted { get; init; } = new();
    public bool HasSkillsLine { get; init; }
    public FormattedString RangedLineFormatted { get; init; } = new();
    public FormattedString CcLineFormatted { get; init; } = new();
    public FormattedString PeripheralEquipmentLineFormatted { get; init; } = new();
    public bool HasPeripheralEquipmentLine { get; init; }
    public FormattedString PeripheralSkillsLineFormatted { get; init; } = new();
    public bool HasPeripheralSkillsLine { get; init; }
    /// <summary>The trooper's XP; never below 0.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExperienceRankName))]
    public partial int ExperiencePoints { get; set; }

    partial void OnExperiencePointsChanged(int value)
    {
        if (value < 0)
        {
            ExperiencePoints = 0;
        }
    }

    public string ExperienceRankName => CompanyUnitExperienceRanks.GetRankName(ExperiencePoints);
    public List<CompanyTrooperPerkState> Perks { get; init; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> SpecOpsPurchases { get; init; } = [];

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    public partial bool IsDetailsExpanded { get; set; }

    /// <summary>Irregular in this company (a Cohesive company's trooper outside its tracked fireteam).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowRegularModifier))]
    public partial bool IsIrregular { get; set; }

    /// <summary>Irregular by profile, so being regular here is worth a badge.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowRegularModifier))]
    public partial bool NormallyIrregular { get; set; }

    public bool ShowRegularModifier => NormallyIrregular && !IsIrregular;

    private static string ToCommaSeparatedDisplay(string? value)
    {
        var parts = CompanyProfileText.SplitDisplayLine(value);
        return parts.Count == 0 ? "-" : string.Join(", ", parts);
    }

    /// <summary>Re-formats the trooper's and its peripheral's MOV after the inches/centimetres preference changed.</summary>
    public void RefreshMoves(bool inInches)
    {
        UnitMoveDisplay = Movement.Format(Movement.From(UnitMoveFirstCm, UnitMoveSecondCm), inInches);
        Subtitle = Statline.WithMove(Subtitle, UnitMoveDisplay);
        if (HasPeripheralStatBlock)
        {
            PeripheralMov = Movement.Format(Movement.From(PeripheralMoveFirstCm, PeripheralMoveSecondCm), inInches);
        }
    }
}
