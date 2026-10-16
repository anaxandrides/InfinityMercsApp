using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.ViewModels.UnitBrowser.Items;

public partial class ViewerProfileItem : ObservableObject
{
    public string GroupName { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string ProfileKey { get; init; } = string.Empty;

    public bool IsLieutenant { get; init; }

    public FormattedString? NameFormatted { get; init; }

    public string RangedWeapons { get; init; } = "-";
    public FormattedString? RangedWeaponsFormatted { get; init; }

    public string MeleeWeapons { get; init; } = "-";
    public FormattedString? MeleeWeaponsFormatted { get; init; }

    public string UniqueEquipment { get; init; } = "-";
    public FormattedString? UniqueEquipmentFormatted { get; init; }

    public string UniqueSkills { get; init; } = "-";
    public FormattedString? UniqueSkillsFormatted { get; init; }

    public string Characteristics { get; init; } = "-";

    public string Peripherals { get; init; } = "-";
    public FormattedString? PeripheralsFormatted { get; init; }

    public bool HasPeripherals => !string.IsNullOrWhiteSpace(Peripherals) && Peripherals != "-";
    public bool HasPeripheralStatBlock { get; init; }
    public string PeripheralNameHeading { get; init; } = string.Empty;
    public string PeripheralMov { get; init; } = "-";
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
    public string PeripheralSubtitle { get; init; } = "-";
    public FormattedString PeripheralEquipmentLineFormatted { get; init; } = new();
    public bool HasPeripheralEquipmentLine { get; init; }
    public FormattedString PeripheralSkillsLineFormatted { get; init; } = new();
    public bool HasPeripheralSkillsLine { get; init; }
    public bool PeripheralGrantsFtMaster { get; init; }
    public bool PeripheralIsIrregular { get; init; }
    public string PeripheralUnitName { get; init; } = string.Empty;

    public string Swc { get; init; } = "-";

    public string SwcDisplay { get; init; } = string.Empty;

    public string Cost { get; init; } = "-";
    public bool ShowProfileTacticalAwarenessIcon { get; init; }

    [ObservableProperty]
    public partial bool IsVisible { get; set; } = true;

    [ObservableProperty]
    public partial bool IsLieutenantBlocked { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
