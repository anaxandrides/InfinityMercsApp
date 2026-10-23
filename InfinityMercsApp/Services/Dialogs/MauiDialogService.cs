namespace InfinityMercsApp.Services.Dialogs;

/// <inheritdoc/>
internal sealed class MauiDialogService : IDialogService
{
    private static Page CurrentPage =>
        Shell.Current?.CurrentPage
        ?? Application.Current?.Windows.FirstOrDefault()?.Page
        ?? throw new InvalidOperationException("No page is available to show a dialog.");

    public Task ShowAlertAsync(string title, string message, string cancel = "OK") =>
        CurrentPage.DisplayAlertAsync(title, message, cancel);

    public Task<bool> ConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No") =>
#if WINDOWS
        ConfirmOnWindowsAsync(title, message, accept, cancel);
#else
        CurrentPage.DisplayAlertAsync(title, message, accept, cancel);
#endif

    public async Task<int?> AskNumberAsync(string title, string message, int minimum, int maximum)
    {
        var answer = await CurrentPage.DisplayPromptAsync(
            title, message, "OK", "Cancel", placeholder: $"{minimum}-{maximum}", maxLength: maximum.ToString(System.Globalization.CultureInfo.InvariantCulture).Length, keyboard: Keyboard.Numeric);
        return int.TryParse(answer, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var number) &&
               number >= minimum && number <= maximum
            ? number
            : null;
    }

#if WINDOWS
    // MAUI's alert on Windows puts the confirming button first, as Windows does; this one puts it last, as Android,
    // macOS and the web do. Escape and Enter both take the cancelling button, so a stray key never confirms.
    private static async Task<bool> ConfirmOnWindowsAsync(string title, string message, string accept, string cancel)
    {
        if (CurrentPage.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement { XamlRoot: { } root })
        {
            return await CurrentPage.DisplayAlertAsync(title, message, accept, cancel);
        }

        var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = cancel,
            SecondaryButtonText = accept,
            DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Primary,
            RequestedTheme = Microsoft.UI.Xaml.ElementTheme.Dark,
            XamlRoot = root
        };
        return await dialog.ShowAsync() == Microsoft.UI.Xaml.Controls.ContentDialogResult.Secondary;
    }
#endif

    public async Task<string?> AskTextAsync(string title, string message, string initialValue, int maxLength) =>
        (await CurrentPage.DisplayPromptAsync(title, message, "OK", "Cancel", maxLength: maxLength, initialValue: initialValue))?.Trim();

    public async Task<string?> ChooseAsync(string title, string cancel, IReadOnlyList<string> options)
    {
        var choice = await CurrentPage.DisplayActionSheetAsync(title, cancel, null, [.. options]);
        return options.FirstOrDefault(option => string.Equals(option, choice, StringComparison.Ordinal));
    }
}
