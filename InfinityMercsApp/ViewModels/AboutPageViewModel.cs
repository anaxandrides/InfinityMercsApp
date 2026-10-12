using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Services;
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
