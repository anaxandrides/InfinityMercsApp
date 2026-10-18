using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Domain.Season.RecruitRolls;
using InfinityMercsApp.Domain.Season.Troopers;
using InfinityMercsApp.Services.Logos;
using InfinityMercsApp.ViewModels.Components;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;

namespace InfinityMercsApp.ViewModels.Company.Viewer.Items;

/// <summary>A saved trooper as a tile in a company's unit strip, carrying what its detail view shows.</summary>
public sealed partial class CompanyViewerUnitListItem : ObservableObject, IViewerListItem, IUnitTile
{
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public int EntryIndex { get; init; }
    public int SourceFactionId { get; init; }
    public int VisualFactionId { get; init; }
    public int SourceUnitId { get; init; }
    public string ProfileKey { get; init; } = string.Empty;
    public bool IsPeripheralUnit { get; init; }

    /// <summary>A peripheral that is a trooper of its own (not a Servant or Ancillary one).</summary>
    public bool IsPeripheralTrooper { get; init; }

    /// <summary>A peripheral from the Peripheral (Controller) augment that no trooper controls now.</summary>
    public bool IsUncontrolledPeripheral { get; init; }

    /// <summary>What a peripheral from the Peripheral (Controller) augment sells for: half its cost, rounded up.</summary>
    public int PeripheralSellPrice { get; init; }
    public bool IsLieutenant { get; init; }

    /// <summary>For a Lone Wolf's Aspect, the Lone Wolf's entry; null for every other trooper.</summary>
    public int? AspectOf { get; init; }

    /// <summary>The founding Captain, who carries the captain upgrades even after the role passes on.</summary>
    public bool CarriesCaptainUpgrades { get; init; }
    public int Cost { get; init; }
    public string SavedEquipment { get; init; } = "-";
    public string SavedSkills { get; init; } = "-";
    public string SavedCharacteristics { get; init; } = "-";
    public string SavedRangedWeapons { get; init; } = "-";
    public string SavedCcWeapons { get; init; } = "-";
    public string SavedPeripheralNameHeading { get; init; } = string.Empty;
    public string UnitMov { get; init; } = "-";
    public string UnitCc { get; init; } = "-";
    public string UnitBs { get; init; } = "-";
    public string UnitPh { get; init; } = "-";
    public string UnitWip { get; init; } = "-";
    public string UnitArm { get; init; } = "-";
    public string UnitBts { get; init; } = "-";
    public string UnitVitality { get; init; } = "-";
    public string UnitS { get; init; } = "-";
    /// <summary>The trooper's faction accent, from the army data; the tile's outline and rail when selected.</summary>
    public Color SelectionAccentColor { get; init; } = Color.FromArgb("#93C5FD");
    public int ExperiencePoints { get; init; }

    /// <summary>The trooper's season so far: rank, perk points, injuries and perks.</summary>
    public TrooperProgress? Progress { get; init; }

    /// <summary>What a TAG company TAG bought in its Spec Ops popup, e.g. "Spec-Ops: +1 ARM".</summary>
    public IReadOnlyList<string> SpecOpsPurchases { get; init; } = [];

    /// <summary>The trooper's Booty and Metachemistry rolls, e.g. "Booty: DA CC Weapon (rolled 7)".</summary>
    public IReadOnlyList<string> RollLines { get; init; } = [];

    /// <summary>The Booty or Metachemistry rolls the trooper has yet to make.</summary>
    public IReadOnlyList<OwedRoll> OwedRolls { get; init; } = [];

    public int ExperienceLevel => CompanyUnitExperienceRanks.GetRankLevel(ExperiencePoints);
    public string ExperienceRankName => CompanyUnitExperienceRanks.GetRankName(ExperiencePoints);
    public string CaptainIconPackagedPath { get; init; } = string.Empty;
    public string ExperienceIconPackagedPath { get; init; } = string.Empty;
    public string BaseUnitName { get; init; } = string.Empty;
    public string BaseUnitDisplayName { get; init; } = string.Empty;
    public string UnitTypeCode { get; init; } = string.Empty;
    public string? CachedLogoPath { get; init; }
    public string? PackagedLogoPath { get; init; }

    /// <summary>A logo to show when neither the cached nor the packaged one exists, e.g. the TAG icon for TAG company units.</summary>
    public string? FallbackLogoPath { get; init; }

    public IReadOnlyList<string> LogoCandidates => UnitLogo.Candidates(CachedLogoPath, PackagedLogoPath, FallbackLogoPath);

    /// <summary>The logo to draw, once worked out from the paths above.</summary>
    [ObservableProperty]
    public partial string? LogoSource { get; set; }

    public string? Subtitle { get; init; }
    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);
}
