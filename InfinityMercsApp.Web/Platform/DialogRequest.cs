namespace InfinityMercsApp.Web.Platform;

/// <summary>A dialog waiting to be answered. <see cref="Buttons"/> run from cancelling to confirming, as the app shows them.</summary>
public sealed class DialogRequest
{
    private readonly TaskCompletionSource<DialogAnswer> _answer = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public required string Title { get; init; }

    public string Message { get; init; } = string.Empty;

    public required IReadOnlyList<string> Buttons { get; init; }

    /// <summary>The button that dismisses the dialog: Escape and the backdrop choose it.</summary>
    public string? CancelButton { get; init; }

    public DialogKind Kind { get; init; } = DialogKind.Buttons;

    public string InitialText { get; init; } = string.Empty;

    public int MaxLength { get; init; } = 200;

    /// <summary>The choices are a list to pick from (an action sheet), rather than a row of buttons.</summary>
    public bool IsList { get; init; }

    public Task<DialogAnswer> Answer => _answer.Task;

    public void Complete(DialogAnswer answer) => _answer.TrySetResult(answer);
}
