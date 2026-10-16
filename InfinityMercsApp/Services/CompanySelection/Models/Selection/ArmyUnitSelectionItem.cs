using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.ViewModels.UnitBrowser;

namespace InfinityMercsApp.Services.CompanySelection.Models.Selection;

public partial class ArmyUnitSelectionItem : ObservableObject, IViewerListItem
{
    public int Id { get; init; }
    public int SourceFactionId { get; init; }
    public int LogoSourceFactionId { get; init; }
    public int LogoSourceUnitId { get; init; }
    public string? Slug { get; init; }

    public int? Type { get; init; }
    public bool IsCharacter { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? CachedLogoPath { get; init; }

    public string? PackagedLogoPath { get; init; }

    public string? Subtitle { get; init; }
    public bool IsSpecOps { get; init; }

    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);

    /// <summary>The TAG Company's own TAG stands out in the list.</summary>
    public bool UseBlueHalfOpacityBackground
    {
        get => _useBlueHalfOpacityBackground;
        init
        {
            _useBlueHalfOpacityBackground = value;
            UnitListBackgroundColor = BackgroundFor(IsSelected, value);
        }
    }

    private readonly bool _useBlueHalfOpacityBackground;

    [ObservableProperty]
    private string _unitListBackgroundColor = BackgroundFor(isSelected: false, highlighted: false);

    [ObservableProperty]
    private bool _isVisible = true;

    [ObservableProperty]
    private bool _isSelected;

    partial void OnIsSelectedChanged(bool value) => UnitListBackgroundColor = BackgroundFor(value, UseBlueHalfOpacityBackground);

    private static string BackgroundFor(bool isSelected, bool highlighted) =>
        isSelected ? "#334155" : highlighted ? "#800000FF" : "Transparent";
}
