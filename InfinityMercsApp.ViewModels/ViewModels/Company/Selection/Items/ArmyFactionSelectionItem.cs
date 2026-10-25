using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Services.CompanySelection.StartCompany;
using InfinityMercsApp.Services.Logos;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;

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
    public partial bool IsSelected { get; set; }

    /// <summary>A metadata faction as the picker lists it, with its downloaded and packaged logos.</summary>
    public static ArmyFactionSelectionItem From(FactionRecord faction, FactionLogoCacheService? logos) => new()
    {
        Id = faction.Id,
        ParentId = faction.ParentId,
        Name = faction.Name,
        CachedLogoPath = logos?.TryGetCachedLogoPath(faction.Id),
        PackagedLogoPath = logos?.GetPackagedFactionLogoPath(faction.Id) ?? $"SVGCache/factions/{faction.Id}.svg"
    };
}
