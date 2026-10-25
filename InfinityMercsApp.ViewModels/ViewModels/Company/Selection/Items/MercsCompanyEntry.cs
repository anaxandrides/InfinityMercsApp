using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Services.CompanyProfiles;
using InfinityMercsApp.Services.CompanySelection.StartCompany;
using InfinityMercsApp.Services.Logos;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;
using InfinityMercsApp.Presentation.Text;

namespace InfinityMercsApp.ViewModels.Company.Selection.Items;

public partial class MercsCompanyEntry : ObservableObject, IViewerListItem, ICompanyMercsEntry
{
    public string Name { get; init; } = string.Empty;
    public string BaseUnitName { get; init; } = string.Empty;
    public RichText? NameFormatted { get; init; }
    public string CostDisplay { get; init; } = string.Empty;
    public int CostValue { get; init; }
    public string ProfileKey { get; init; } = string.Empty;
    /// <summary>The captain; a Proxy Pack's Lone Wolf is one without the Lieutenant skill.</summary>
    public bool IsLieutenant { get; set; }

    /// <inheritdoc/>
    public bool IsLoneWolf { get; set; }

    /// <summary>Recruited as a Spec Ops profile, whose captain upgrades come from the Spec-Ops chart.</summary>
    public bool IsSpecOps { get; init; }

    /// <summary>Already in the company while it recruits: counted for AVA and the company's rules, but not removed or saved again.</summary>
    public bool IsOnRoster { get; init; }

    public bool CanRemove => !IsOnRoster;

    /// <summary>
    /// The name the player gave the trooper; empty to go by its unit's name. A trooper already on a recruiting
    /// company's roster isn't saved from this screen, so it is renamed from the season's Team tab instead.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCustomName))]
    public partial string CustomName { get; set; } = string.Empty;

    public bool HasCustomName => !string.IsNullOrWhiteSpace(CustomName);

    public bool CanRename => !IsOnRoster;

    /// <inheritdoc/>
    public int? SavedEntryIndex { get; init; }

    /// <summary>A Doctor or Engineer, who can own a Servant recruited on its own.</summary>
    public bool CanOwnServant { get; init; }

    /// <summary>The Doctor or Engineer this Servant belongs to; null for every other trooper.</summary>
    public MercsCompanyEntry? ServantOwner { get; init; }

    ICompanyMercsEntry? ICompanyMercsEntry.ServantOwner => ServantOwner;

    public bool IsServant => ServantOwner is not null;

    /// <summary>Who the Servant belongs to, e.g. "Servant of Hakim".</summary>
    public string ServantOfText => ServantOwner is { } owner ? $"Servant of {owner.Name}" : string.Empty;
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

    public RichText EquipmentLineFormatted { get; init; } = RichText.Empty;
    public bool HasEquipmentLine { get; init; }
    public RichText SkillsLineFormatted { get; init; } = RichText.Empty;
    public bool HasSkillsLine { get; init; }
    public RichText RangedLineFormatted { get; init; } = RichText.Empty;
    public RichText CcLineFormatted { get; init; } = RichText.Empty;
    public RichText PeripheralEquipmentLineFormatted { get; init; } = RichText.Empty;
    public bool HasPeripheralEquipmentLine { get; init; }
    public RichText PeripheralSkillsLineFormatted { get; init; } = RichText.Empty;
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
