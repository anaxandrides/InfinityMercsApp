using InfinityMercsApp.ViewModels;

namespace InfinityMercsApp.Views;

public sealed class CompanyViewerUnitListItem : BaseViewModel, IViewerListItem
{
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
    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set
        {
            if (_name == value)
            {
                return;
            }

            _name = value;
            OnPropertyChanged();
        }
    }
    public string BaseUnitName { get; init; } = string.Empty;
    public string BaseUnitDisplayName { get; init; } = string.Empty;
    public string UnitTypeCode { get; init; } = string.Empty;
    public string? CachedLogoPath { get; init; }
    public string? PackagedLogoPath { get; init; }
    public string? Subtitle { get; init; }
    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TileStroke));
            OnPropertyChanged(nameof(TileStrokeThickness));
        }
    }
}
