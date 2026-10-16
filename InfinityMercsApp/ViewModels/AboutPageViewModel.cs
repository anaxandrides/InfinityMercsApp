using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.ViewModels;

/// <summary>Opens the author contact and attribution links shown on the About page.</summary>
public partial class AboutPageViewModel(
    INavigationService navigationService,
    ILauncher launcher) : ViewModelBase(navigationService)
{
    public const string AuthorEmailUri = "mailto:jeremiahpatrick@protonmail.com";
    public const string DiscordAppUri = "discord://";
    public const string DiscordWebUri = "https://discord.com/app";

    // Page padding (24 each side) and the gap between the two columns.
    private const double PagePadding = 48;
    private const double ColumnGap = 16;

    /// <summary>Each attribution entry's width: half the page on screens wider than 500, else -1 for its natural width.</summary>
    [ObservableProperty]
    public partial double AttributionEntryWidth { get; set; } = -1;

    [RelayCommand]
    private void FitAttributionColumns(double pageWidth) =>
        AttributionEntryWidth = pageWidth > 500 ? Math.Max(0, (pageWidth - PagePadding - ColumnGap) / 2) : -1;

    [RelayCommand]
    private Task OpenEmailAsync() => launcher.OpenAsync(AuthorEmailUri);

    [RelayCommand]
    private async Task OpenDiscordAsync()
    {
        try
        {
            await launcher.OpenAsync(DiscordAppUri);
        }
        catch
        {
            await launcher.OpenAsync(DiscordWebUri);
        }
    }

    [RelayCommand]
    private async Task OpenLinkAsync(string? uri)
    {
        if (!string.IsNullOrWhiteSpace(uri))
        {
            await launcher.OpenAsync(uri);
        }
    }
}
