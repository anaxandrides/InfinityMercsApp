using InfinityMercsApp.Web.Platform;

namespace InfinityMercsApp.Web.UnitTests.Platform;

/// <summary>Shows dialogs by answering each with the next answer given, or leaves it waiting when none is left.</summary>
internal sealed class AnsweringPresenter(params IEnumerable<DialogAnswer> answers) : IDialogPresenter
{
    private readonly Queue<DialogAnswer> _answers = new(answers);

    public List<DialogRequest> Shown { get; } = [];

    public void Show(DialogRequest request)
    {
        Shown.Add(request);
        if (_answers.TryDequeue(out var answer))
        {
            request.Complete(answer);
        }
    }
}
