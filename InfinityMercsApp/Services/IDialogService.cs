namespace InfinityMercsApp.Services;

/// <summary>Alerts and confirmations, so ViewModels can ask the user without touching pages.</summary>
public interface IDialogService
{
    Task ShowAlertAsync(string title, string message, string cancel = "OK");

    /// <returns>True when the user chose <paramref name="accept"/>.</returns>
    Task<bool> ConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No");
}

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
