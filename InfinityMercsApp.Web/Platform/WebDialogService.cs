using System.Globalization;
using InfinityMercsApp.Services.Dialogs;

namespace InfinityMercsApp.Web.Platform;

/// <inheritdoc/>
/// <remarks>Dialogs show one at a time in the layout's dialog host; one asked for while another shows waits its turn.</remarks>
public sealed class WebDialogService : IDialogService
{
    private readonly SemaphoreSlim _one = new(1, 1);

    /// <summary>The dialog host, once the layout has shown it.</summary>
    public IDialogPresenter? Presenter { get; set; }

    public async Task ShowAlertAsync(string title, string message, string cancel = "OK") =>
        await AskAsync(new DialogRequest { Title = title, Message = message, Buttons = [cancel], CancelButton = cancel });

    public async Task<bool> ConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No") =>
        (await AskAsync(new DialogRequest { Title = title, Message = message, Buttons = [cancel, accept], CancelButton = cancel })).Button == accept;

    public async Task<int?> AskNumberAsync(string title, string message, int minimum, int maximum)
    {
        var answer = await AskAsync(new DialogRequest
        {
            Title = title,
            Message = message,
            Buttons = ["Cancel", "OK"],
            CancelButton = "Cancel",
            Kind = DialogKind.Number,
            MaxLength = maximum.ToString(CultureInfo.InvariantCulture).Length
        });
        return answer.Button == "OK" &&
               int.TryParse(answer.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) &&
               number >= minimum && number <= maximum
            ? number
            : null;
    }

    public async Task<string?> AskTextAsync(string title, string message, string initialValue, int maxLength)
    {
        var answer = await AskAsync(new DialogRequest
        {
            Title = title,
            Message = message,
            Buttons = ["Cancel", "OK"],
            CancelButton = "Cancel",
            Kind = DialogKind.Text,
            InitialText = initialValue,
            MaxLength = maxLength
        });
        return answer.Button == "OK" ? answer.Text.Trim() : null;
    }

    public async Task<string?> ChooseAsync(string title, string cancel, IReadOnlyList<string> options)
    {
        var answer = await AskAsync(new DialogRequest { Title = title, Buttons = [.. options, cancel], CancelButton = cancel, IsList = true });
        return options.FirstOrDefault(option => string.Equals(option, answer.Button, StringComparison.Ordinal));
    }

    private async Task<DialogAnswer> AskAsync(DialogRequest request)
    {
        if (Presenter is not { } presenter)
        {
            return DialogAnswer.Dismissed;
        }

        await _one.WaitAsync();
        try
        {
            presenter.Show(request);
            return await request.Answer;
        }
        finally
        {
            _one.Release();
        }
    }
}
