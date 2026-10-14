namespace InfinityMercsApp.Services;

public sealed class FeedbackSubmitResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? IssueUrl { get; init; }

    public static FeedbackSubmitResult Ok(string? issueUrl)
    {
        var message = string.IsNullOrWhiteSpace(issueUrl)
            ? "Feedback email sent successfully."
            : $"Feedback email sent successfully: {issueUrl}";

        return new FeedbackSubmitResult
        {
            Success = true,
            Message = message,
            IssueUrl = issueUrl
        };
    }

    public static FeedbackSubmitResult Fail(string message)
    {
        return new FeedbackSubmitResult
        {
            Success = false,
            Message = message
        };
    }
}
