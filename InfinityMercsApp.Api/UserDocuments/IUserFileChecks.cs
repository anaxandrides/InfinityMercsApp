namespace InfinityMercsApp.Api.UserDocuments;

/// <summary>What makes a saved file of one kind unusable, checked before it is kept.</summary>
public interface IUserFileChecks<in TContent>
    where TContent : class
{
    /// <summary>Each problem with <paramref name="content"/>, as a sentence; empty when it can be kept.</summary>
    IReadOnlyList<string> ProblemsWith(TContent content);
}
