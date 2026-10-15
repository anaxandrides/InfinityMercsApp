using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.ViewModels.UnitBrowser;

namespace InfinityMercsApp.ViewModels.Company.Viewer;

/// <summary>A saved trooper as a tile in a company's unit strip, carrying what its detail view shows.</summary>
public sealed partial class CompanyViewerUnitListItem : ObservableObject, IViewerListItem
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TileStroke), nameof(TileStrokeThickness))]
    private bool _isSelected;

    public int EntryIndex { get; init; }
    public int SourceFactionId { get; init; }
    public int VisualFactionId { get; init; }
    public int SourceUnitId { get; init; }
    public string ProfileKey { get; init; } = string.Empty;
    public bool IsPeripheralUnit { get; init; }
    public bool IsLieutenant { get; init; }
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
    public Color SelectionAccentColor { get; init; } = Color.FromArgb("#93C5FD");
    public Color TileStroke => IsSelected ? SelectionAccentColor : Color.FromArgb("#3A4554");
    public double TileStrokeThickness => IsSelected ? 2d : 1d;
    public int ExperiencePoints { get; init; }
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

    /// <summary>The logo to draw, once worked out from the paths above.</summary>
    [ObservableProperty]
    private string? _logoSource;

    public string? Subtitle { get; init; }
    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);
}
