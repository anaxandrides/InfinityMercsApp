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
        CurrentPage.DisplayAlertAsync(title, message, accept, cancel);

    public async Task<int?> AskNumberAsync(string title, string message, int minimum, int maximum)
    {
        var answer = await CurrentPage.DisplayPromptAsync(
            title, message, "OK", "Cancel", placeholder: $"{minimum}-{maximum}", maxLength: maximum.ToString(System.Globalization.CultureInfo.InvariantCulture).Length, keyboard: Keyboard.Numeric);
        return int.TryParse(answer, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var number) &&
               number >= minimum && number <= maximum
            ? number
            : null;
    }
}
