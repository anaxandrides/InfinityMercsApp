using InfinityMercsApp.Api.UserDocuments;
using InfinityMercsApp.Domain.Models.Season;

namespace InfinityMercsApp.Api.UnitTests.UserDocuments;

/// <summary>The rules themselves are tested with the validator; these check the API applies them and the id.</summary>
public sealed class SeasonFileChecksTests
{
    private static readonly SeasonFileChecks Checks = new();

    private static SeasonFile Season() => new() { SeasonIdentifier = "s1", CompanyName = "Blue Team", CompanyIdentifier = "blue" };

    [Fact]
    public void ProblemsWith_CompleteSeason_IsNone() => Assert.Empty(Checks.ProblemsWith("s1", Season()));

    [Fact]
    public void ProblemsWith_SavedUnderAnotherId_SaysSo() =>
        Assert.Equal(["The season's identifier isn't the one it is saved under."], Checks.ProblemsWith("s2", Season()));

    [Fact]
    public void ProblemsWith_NoCompany_ListsTheValidatorsProblems() =>
        Assert.Contains("The season has no company name.", Checks.ProblemsWith("s1", new SeasonFile { SeasonIdentifier = "s1" }));
}
