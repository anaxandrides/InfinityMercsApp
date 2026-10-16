using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Services.Logos;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;

namespace InfinityMercsApp.ViewModels.Company.Selection.Items;

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

    public IReadOnlyList<string> LogoCandidates => UnitLogo.Candidates(CachedLogoPath, PackagedLogoPath);

    public string? Subtitle { get; init; }
    public bool IsSpecOps { get; init; }

    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);

    /// <summary>The TAG Company's own TAG stands out in the list.</summary>
    public bool UseBlueHalfOpacityBackground { get; init; }

    [ObservableProperty]
    public partial bool IsVisible { get; set; } = true;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
