using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Adaptive;

namespace InfinityMercsApp.Views.Season;

public partial class MercsSeasonPage : AdaptiveContentPage
{
    public MercsSeasonPage(MercsSeasonPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

#if WINDOWS
        // The window/Shell are fully live by the time the home page appears, so this is a
        // reliable point to wire up the Ctrl+Shift+Q/W/E/R resize shortcuts (idempotent).
        InfinityMercsApp.WinUI.App.EnsureWindowResizeShortcuts();
#endif

        // Resets the page to its main two-button state.
        await InitializeViewModelAsync();
    }
}
