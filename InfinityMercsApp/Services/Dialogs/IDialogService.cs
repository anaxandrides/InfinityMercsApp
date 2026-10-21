namespace InfinityMercsApp.Services.Dialogs;

/// <summary>Alerts and confirmations, so ViewModels can ask the user without touching pages.</summary>
public interface IDialogService
{
    Task ShowAlertAsync(string title, string message, string cancel = "OK");

    /// <returns>True when the user chose <paramref name="accept"/>.</returns>
    Task<bool> ConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No");

    /// <summary>Asks for a whole number from <paramref name="minimum"/> to <paramref name="maximum"/>.</summary>
    /// <returns>The number, or null when the user cancels or enters something outside the range.</returns>
    Task<int?> AskNumberAsync(string title, string message, int minimum, int maximum);

    /// <summary>Asks which of <paramref name="options"/> to take.</summary>
    /// <returns>The option chosen, or null when the user picks <paramref name="cancel"/>.</returns>
    Task<string?> ChooseAsync(string title, string cancel, IReadOnlyList<string> options);
}
