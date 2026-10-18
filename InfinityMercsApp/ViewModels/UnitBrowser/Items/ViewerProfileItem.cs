using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Presentation.Company.Selection;
using InfinityMercsApp.Services.CompanyProfiles;
using InfinityMercsApp.Services.CompanySelection.Profiles;
using InfinityMercsApp.Services.CompanySelection.Units;

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

    /// <summary>
    /// A selection screen's configuration as a listed profile, its names coloured by kind. The profile's own
    /// Tactical Awareness icon shows only when the unit's header doesn't already show it.
    /// </summary>
    public static ViewerProfileItem From(
        ProfileConfiguration configuration, Func<int?, int?, string> formatMove, bool unitShowsTacticalAwareness)
    {
        var peripheral = configuration.Peripheral;
        var equipment = configuration.PeripheralEquipment;
        var skills = configuration.PeripheralSkills;
        return new ViewerProfileItem
        {
            GroupName = configuration.GroupName,
            Name = configuration.Name,
            ProfileKey = configuration.ProfileKey,
            IsLieutenant = configuration.IsLieutenant,
            NameFormatted = CompanyProfileFormatting.BuildNameFormatted(configuration.Name),
            RangedWeapons = CompanyProfileText.JoinOrDash(configuration.RangedWeapons),
            RangedWeaponsFormatted = CompanyProfileFormatting.BuildListFormattedString(configuration.RangedWeapons, Color.FromArgb("#EF4444")),
            MeleeWeapons = CompanyProfileText.JoinOrDash(configuration.MeleeWeapons),
            MeleeWeaponsFormatted = CompanyProfileFormatting.BuildListFormattedString(configuration.MeleeWeapons, Color.FromArgb("#22C55E")),
            UniqueEquipment = CompanyProfileText.JoinOrDash(configuration.UniqueEquipment),
            UniqueEquipmentFormatted = CompanyProfileFormatting.BuildListFormattedString(configuration.UniqueEquipment, Color.FromArgb("#06B6D4")),
            UniqueSkills = CompanyProfileText.JoinOrDash(configuration.UniqueSkills),
            UniqueSkillsFormatted = CompanyProfileFormatting.BuildListFormattedString(
                configuration.UniqueSkills,
                Color.FromArgb("#F59E0B"),
                highlightLieutenantPurple: configuration.ForcedLieutenant),
            Characteristics = CompanyProfileText.JoinOrDash(configuration.Characteristics),
            Peripherals = CompanyProfileText.JoinOrDash(configuration.Peripherals),
            PeripheralsFormatted = CompanyProfileFormatting.BuildListFormattedString(configuration.Peripherals, Color.FromArgb("#FACC15")),
            HasPeripheralStatBlock = peripheral is not null,
            PeripheralNameHeading = peripheral?.NameHeading ?? string.Empty,
            PeripheralMov = peripheral is not null ? formatMove(peripheral.MoveFirstCm, peripheral.MoveSecondCm) : "-",
            PeripheralCc = peripheral?.Cc ?? "-",
            PeripheralBs = peripheral?.Bs ?? "-",
            PeripheralPh = peripheral?.Ph ?? "-",
            PeripheralWip = peripheral?.Wip ?? "-",
            PeripheralArm = peripheral?.Arm ?? "-",
            PeripheralBts = peripheral?.Bts ?? "-",
            PeripheralVitalityHeader = peripheral?.VitalityHeader ?? Stat.Vita.Key(),
            PeripheralVitality = peripheral?.Vitality ?? "-",
            PeripheralS = peripheral?.S ?? "-",
            PeripheralAva = peripheral?.Ava ?? "-",
            PeripheralSubtitle = CompanyUnitDetailsShared.BuildPeripheralSubtitle(peripheral),
            PeripheralEquipmentLineFormatted = CompanyProfileFormatting.BuildMercsCompanyLineFormatted("Equipment", equipment, Color.FromArgb("#06B6D4")),
            HasPeripheralEquipmentLine = peripheral is not null && !string.IsNullOrWhiteSpace(equipment) && equipment != "-",
            PeripheralSkillsLineFormatted = CompanyProfileFormatting.BuildMercsCompanyLineFormatted("Skills", skills, Color.FromArgb("#F59E0B")),
            HasPeripheralSkillsLine = peripheral is not null && !string.IsNullOrWhiteSpace(skills) && skills != "-",
            PeripheralGrantsFtMaster = configuration.PeripheralIsControlled && !string.IsNullOrWhiteSpace(configuration.PeripheralUnitName),
            PeripheralIsIrregular = configuration.PeripheralIsControlled,
            PeripheralUnitName = configuration.PeripheralUnitName,
            Swc = configuration.Swc,
            SwcDisplay = $"SWC {configuration.Swc}",
            Cost = configuration.Cost,
            ShowProfileTacticalAwarenessIcon = !unitShowsTacticalAwareness && configuration.HasTacticalAwareness
        };
    }
}
