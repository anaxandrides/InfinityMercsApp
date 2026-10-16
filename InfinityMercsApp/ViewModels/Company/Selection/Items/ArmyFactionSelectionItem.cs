using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Services.CompanySelection.StartCompany;
using InfinityMercsApp.Services.Logos;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;

namespace InfinityMercsApp.ViewModels.Company.Selection.Items;

public partial class ArmyFactionSelectionItem : ObservableObject, IViewerListItem, ICompanySourceFaction
{
    public int Id { get; init; }

    public int ParentId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? CachedLogoPath { get; init; }

    public string? PackagedLogoPath { get; init; }

    /// <summary>The logos to try in order, ending with the faction's packaged logo by id.</summary>
    public IReadOnlyList<string> LogoCandidates => UnitLogo.Candidates(CachedLogoPath, PackagedLogoPath, $"SVGCache/factions/{Id}.svg");

    public string? Subtitle => null;

    public bool HasSubtitle => false;

    [ObservableProperty]
    private bool _isSelected;
}
