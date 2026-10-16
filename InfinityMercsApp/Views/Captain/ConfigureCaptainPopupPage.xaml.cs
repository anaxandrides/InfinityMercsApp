using InfinityMercsApp.Services.CompanySelection.Captain;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.ViewModels.Captain;

namespace InfinityMercsApp.Views.Captain;

/// <summary>Modal popup for configuring a company captain. Use <see cref="ShowAsync"/> to show it and await the result.</summary>
public partial class ConfigureCaptainPopupPage : ContentPage
{
    private readonly ConfigureCaptainViewModel _viewModel;

    private ConfigureCaptainPopupPage(ConfigureCaptainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <summary>Shows the popup modally; returns the configured captain, or null if the user backed out.</summary>
    public static async Task<SavedImprovedCaptainStats?> ShowAsync(INavigation navigation, CaptainUpgradePopupContext context)
    {
        var viewModel = new ConfigureCaptainViewModel(context);
        var page = new ConfigureCaptainPopupPage(viewModel);
        await navigation.PushModalAsync(page, false);
        var result = await viewModel.Completion;
        await DismissIfTopAsync(navigation, page);
        return result;
    }

    // The hardware back button counts as BACK, so the caller is never left waiting.
    protected override bool OnBackButtonPressed()
    {
        _viewModel.CancelCommand.Execute(null);
        return true;
    }

    // Only pop when this popup is still the top modal.
    private static async Task DismissIfTopAsync(INavigation navigation, Page page)
    {
        var modalStack = navigation.ModalStack;
        if (modalStack.Count == 0 || !ReferenceEquals(modalStack[^1], page))
        {
            return;
        }

        try
        {
            await navigation.PopModalAsync(false);
        }
        catch (ArgumentException ex) when (ex.Message.Contains("Ambiguous routes matched", StringComparison.OrdinalIgnoreCase))
        {
            // Shell routing can throw this transiently when dismissing; safe to ignore.
            Console.Error.WriteLine($"ConfigureCaptainPopupPage ignored ambiguous route pop: {ex.Message}");
        }
    }
}
