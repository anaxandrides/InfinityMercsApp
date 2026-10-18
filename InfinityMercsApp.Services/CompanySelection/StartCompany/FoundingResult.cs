namespace InfinityMercsApp.Services.CompanySelection.StartCompany;

/// <summary>How founding a company ended, and where its file was written when it was.</summary>
public sealed record FoundingResult(FoundingOutcome Outcome, string FilePath = "")
{
    public static FoundingResult Founded(string filePath) => new(FoundingOutcome.Founded, filePath);

    public static FoundingResult NoTroopers { get; } = new(FoundingOutcome.NoTroopers);

    public static FoundingResult CaptainCancelled { get; } = new(FoundingOutcome.CaptainCancelled);
}
