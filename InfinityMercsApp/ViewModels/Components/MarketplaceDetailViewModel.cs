using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Models.Metadata.Wiki;
using InfinityMercsApp.Infrastructure.Providers.Metadata;
using InfinityMercsApp.Presentation.Marketplaces;
using InfinityMercsApp.Services.Wiki;
using InfinityMercsApp.ViewModels.Marketplaces.Items;

namespace InfinityMercsApp.ViewModels.Components;

/// <summary>
/// The popup that shows a marketplace row (or a weapon) in full: its facts, abilities with wiki links,
/// weapon profiles, and the wiki description of an item that has one.
/// </summary>
public partial class MarketplaceDetailViewModel(
    IMetadataProvider metadataProvider,
    IWikiDescriptionService wikiDescriptionService,
    ILauncher launcher) : ObservableObject
{
    private SkillWikiLinks? _skillLinks;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOpen))]
    public partial MarketplaceDetail? Detail { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasWiki), nameof(IsWikiLoading), nameof(IsWikiEmpty), nameof(HasWikiBlocks))]
    public partial WikiStatus WikiStatus { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<WikiContentBlock> WikiBlocks { get; set; } = [];

    /// <summary>Whether weapon ranges are shown in inches rather than centimetres.</summary>
    [ObservableProperty]
    public partial bool ShowUnitsInInches { get; set; } = true;

    public bool IsOpen => Detail is not null;
    public bool HasWiki => WikiStatus != WikiStatus.None;
    public bool IsWikiLoading => WikiStatus == WikiStatus.Loading;
    public bool IsWikiEmpty => WikiStatus == WikiStatus.Empty;
    public bool HasWikiBlocks => WikiStatus == WikiStatus.Loaded;

    /// <summary>Opens the popup on a marketplace row: an item with its weapon profiles, a troop type with its skill links, or an augment.</summary>
    public Task ShowRowAsync(MarketplaceRow row) => row switch
    {
        StoreItemRow item => ShowAsync(MarketplaceDetail.For(item, metadataProvider.WeaponsFor(item.Name))),
        TroopTypeRow troopType => ShowAsync(MarketplaceDetail.For(troopType, _skillLinks ??= SkillWikiLinks.From(metadataProvider.GetSkills()))),
        AugmentRow augment => ShowAsync(MarketplaceDetail.For(augment)),
        _ => Task.CompletedTask
    };

    /// <summary>Opens the popup on a weapon's profiles.</summary>
    public Task ShowWeaponAsync(string weaponName) =>
        ShowAsync(MarketplaceDetail.ForWeapon(weaponName, metadataProvider.WeaponsFor(weaponName)));

    /// <summary>Opens the popup on <paramref name="detail"/>; one with a wiki page then loads its description.</summary>
    public async Task ShowAsync(MarketplaceDetail detail)
    {
        WikiBlocks = [];
        WikiStatus = detail.WikiUrl is null ? WikiStatus.None : WikiStatus.Loading;
        Detail = detail;
        if (detail.WikiUrl is null)
        {
            return;
        }

        var blocks = await wikiDescriptionService.FetchContentAsync(detail.WikiUrl, detail.WikiSection);

        // The popup may have been closed or moved on to another row while the page loaded.
        if (!ReferenceEquals(Detail, detail))
        {
            return;
        }

        WikiBlocks = blocks;
        WikiStatus = blocks.Count == 0 ? WikiStatus.Empty : WikiStatus.Loaded;
    }

    [RelayCommand]
    private void Close() => Detail = null;

    [RelayCommand]
    private async Task OpenLinkAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            await launcher.OpenAsync(url);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to open link '{url}': {ex.Message}");
        }
    }
}
