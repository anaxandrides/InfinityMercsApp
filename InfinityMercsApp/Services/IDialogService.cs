namespace InfinityMercsApp.Services;

/// <summary>Alerts and confirmations, so ViewModels can ask the user without touching pages.</summary>
public interface IDialogService
{
    Task ShowAlertAsync(string title, string message, string cancel = "OK");

    /// <returns>True when the user chose <paramref name="accept"/>.</returns>
    Task<bool> ConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No");
}
