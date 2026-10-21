using InfinityMercsApp.Api.UserDocuments;
using InfinityMercsApp.Domain.Models.Season;

namespace InfinityMercsApp.Api.UnitTests.UserDocuments;

public sealed class SeasonFileChecksTests
{
    private static readonly SeasonFileChecks Checks = new();

    [Fact]
    public void ProblemsWith_CompanyNamed_IsNone() =>
        Assert.Empty(Checks.ProblemsWith(new SeasonFile { CompanyName = "Blue Team", CompanyIdentifier = "blue" }));

    [Fact]
    public void ProblemsWith_NoCompany_NamesBoth() =>
        Assert.Equal(
            ["The season has no company name.", "The season has no company identifier."],
            Checks.ProblemsWith(new SeasonFile()));
}
