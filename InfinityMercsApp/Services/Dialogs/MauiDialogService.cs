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
}
