using InfinityMercsApp.ViewModels;
using System.Windows.Input;

namespace InfinityMercsApp.Views.Controls;

public partial class CompanyViewerUnitTileView : ContentView
{
    private const int TagCompanyFactionId = 2003;
    private const string TagCompanyFallbackIconPath = "SVGCache/MercsIcons/noun-battle-mech-1731140.svg";

    private int _logoLoadVersion;
    private double _panLastTotalX;

    public static readonly BindableProperty ItemTappedCommandProperty =
        BindableProperty.Create(
            nameof(ItemTappedCommand),
            typeof(ICommand),
            typeof(CompanyViewerUnitTileView));

    public static readonly BindableProperty ItemTappedCommandParameterProperty =
        BindableProperty.Create(
            nameof(ItemTappedCommandParameter),
            typeof(object),
            typeof(CompanyViewerUnitTileView));

    public static readonly BindableProperty PanScrollCommandProperty =
        BindableProperty.Create(
            nameof(PanScrollCommand),
            typeof(ICommand),
            typeof(CompanyViewerUnitTileView));

    public CompanyViewerUnitTileView()
    {
        InitializeComponent();
    }

    public ICommand? ItemTappedCommand
    {
        get => (ICommand?)GetValue(ItemTappedCommandProperty);
        set => SetValue(ItemTappedCommandProperty, value);
    }

    public object? ItemTappedCommandParameter
    {
        get => GetValue(ItemTappedCommandParameterProperty);
        set => SetValue(ItemTappedCommandParameterProperty, value);
    }

    public ICommand? PanScrollCommand
    {
        get => (ICommand?)GetValue(PanScrollCommandProperty);
        set => SetValue(PanScrollCommandProperty, value);
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        _ = LoadLogosAsync();
    }

    private async Task LoadLogosAsync()
    {
        var loadVersion = ++_logoLoadVersion;

        if (BindingContext is not IViewerListItem item)
        {
            ApplyPaths(loadVersion, null, null, null);
            return;
        }

        var logoPath = await ResolveBestLogoPathAsync(item);

        string? captainPath = null;
        string? experiencePath = null;
        if (item is CompanyViewerUnitListItem companyItem)
        {
            captainPath = await ResolvePackagedPathAsync(companyItem.CaptainIconPackagedPath);
            experiencePath = await ResolvePackagedPathAsync(companyItem.ExperienceIconPackagedPath);
        }

        ApplyPaths(loadVersion, logoPath, captainPath, experiencePath);
    }

    // A recycled tile rebinding quickly can complete an older resolution after a newer one; the
    // version guard drops the stale set so the tile never shows the previous unit's icons.
    private void ApplyPaths(int loadVersion, string? logoPath, string? captainPath, string? experiencePath)
    {
        if (loadVersion != _logoLoadVersion)
        {
            return;
        }

        MainLogoImage.SvgPath = logoPath;
        CaptainBadgeImage.SvgPath = captainPath;
        ExperienceBadgeImage.SvgPath = experiencePath;
    }

    private static async Task<string?> ResolveBestLogoPathAsync(IViewerListItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.CachedLogoPath) && File.Exists(item.CachedLogoPath))
        {
            return item.CachedLogoPath;
        }

        if (!string.IsNullOrWhiteSpace(item.PackagedLogoPath))
        {
            foreach (var candidate in BuildPackagedCandidates(item.PackagedLogoPath))
            {
                if (await AppPackageFileExistsAsync(candidate))
                {
                    return candidate;
                }
            }
        }

        foreach (var fallback in BuildFallbackLogoCandidates(item))
        {
            if (await AppPackageFileExistsAsync(fallback))
            {
                return fallback;
            }
        }

        return null;
    }

    private static async Task<string?> ResolvePackagedPathAsync(string? packagedPath)
    {
        if (string.IsNullOrWhiteSpace(packagedPath))
        {
            return null;
        }

        foreach (var candidate in BuildPackagedCandidates(packagedPath))
        {
            if (await AppPackageFileExistsAsync(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static async Task<bool> AppPackageFileExistsAsync(string packagedPath)
    {
        try
        {
            return await FileSystem.Current.AppPackageFileExistsAsync(packagedPath);
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<string> BuildPackagedCandidates(string packagedPath)
    {
        var normalized = packagedPath.Replace('\\', '/').TrimStart('/');
        yield return normalized;
        yield return normalized.ToLowerInvariant();
    }

    private static IEnumerable<string> BuildFallbackLogoCandidates(IViewerListItem item)
    {
        if (item is not CompanyViewerUnitListItem companyItem)
        {
            yield break;
        }

        var isTagCompanyUnit = companyItem.VisualFactionId == TagCompanyFactionId ||
                               companyItem.SourceFactionId == TagCompanyFactionId ||
                               companyItem.BaseUnitName.Contains("Repurposed Mining Equipment", StringComparison.OrdinalIgnoreCase) ||
                               companyItem.BaseUnitName.Contains("Turtlemek", StringComparison.OrdinalIgnoreCase);

        if (isTagCompanyUnit)
        {
            yield return TagCompanyFallbackIconPath;
        }
    }

    private void OnTilePanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _panLastTotalX = 0d;
                break;
            case GestureStatus.Running:
                var deltaX = e.TotalX - _panLastTotalX;
                _panLastTotalX = e.TotalX;
                if (PanScrollCommand?.CanExecute(deltaX) == true)
                    PanScrollCommand.Execute(deltaX);
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _panLastTotalX = 0d;
                break;
        }
    }

    private void OnTileTapped(object? sender, TappedEventArgs e)
    {
        var parameter = ItemTappedCommandParameter ?? BindingContext;
        if (ItemTappedCommand?.CanExecute(parameter) == true)
        {
            ItemTappedCommand.Execute(parameter);
        }
    }
}
