using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Services.CompanySelection.Teams;
using InfinityMercsApp.Services.Logos;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;

namespace InfinityMercsApp.ViewModels.Company.Selection.Items;

public partial class ArmyTeamUnitLimitItem : ObservableObject, IViewerListItem, ITeamSlot
{
    public string Name { get; init; } = string.Empty;
    public string Min { get; init; } = "0";
    public string Max { get; init; } = "0";
    public string? Slug { get; init; }
    public bool IsCharacter { get; init; }
    public int? ResolvedUnitId { get; init; }
    public int? ResolvedSourceFactionId { get; init; }
    public string? CachedLogoPath { get; init; }
    public string? PackagedLogoPath { get; init; }

    public IReadOnlyList<string> LogoCandidates => UnitLogo.Candidates(CachedLogoPath, PackagedLogoPath);
    public string? Subtitle { get; init; }
    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);

    [ObservableProperty]
    public partial bool IsVisible { get; set; } = true;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>A listed fireteam slot, showing <paramref name="min"/> as its minimum and the matched unit's logo.</summary>
    public static ArmyTeamUnitLimitItem For(TeamSlotListing<ArmyUnitSelectionItem> slot, string min) => new()
    {
        Name = slot.Name,
        Min = min,
        Max = slot.Max,
        Slug = slot.Slug,
        IsCharacter = slot.Unit?.IsCharacter ?? false,
        CachedLogoPath = slot.Unit?.CachedLogoPath,
        PackagedLogoPath = slot.Unit?.PackagedLogoPath,
        Subtitle = slot.Unit?.Subtitle,
        ResolvedUnitId = slot.Unit?.Id,
        ResolvedSourceFactionId = slot.Unit?.SourceFactionId
    };
}
