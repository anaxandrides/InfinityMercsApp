namespace InfinityMercsApp.Services.Feedback;

public sealed class FeedbackSubmission
{
    public string Category { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string UserEmail { get; init; } = string.Empty;
}
