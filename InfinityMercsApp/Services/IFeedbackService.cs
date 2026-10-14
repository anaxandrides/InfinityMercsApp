namespace InfinityMercsApp.Services;

public interface IFeedbackService
{
    Task<FeedbackSubmitResult> SubmitAsync(FeedbackSubmission submission, CancellationToken cancellationToken = default);
}
