using InfinityMercsApp.Api.UserDocuments;
using InfinityMercsApp.Domain.Models.Company;

namespace InfinityMercsApp.Api.UnitTests.UserDocuments;

/// <summary>The rules themselves are tested with the validator; these check the API applies them and the id.</summary>
public sealed class CompanyFileChecksTests
{
    private static readonly CompanyFileChecks Checks = new();

    private static SavedCompanyFile Company(string identifier = "blue") => new()
    {
        CompanyName = "Blue Team",
        CompanyType = "Standard Company - Vanilla",
        CompanyIdentifier = identifier,
        SourceFactions = [new SavedCompanyFaction { FactionId = 101 }],
        Entries = [new SavedCompanyEntry { EntryIndex = 0, Name = "Fusilier", SourceFactionId = 101, SourceUnitId = 1, IsLieutenant = true }]
    };

    [Fact]
    public void ProblemsWith_CompleteCompany_IsNone() => Assert.Empty(Checks.ProblemsWith("blue", Company()));

    [Fact]
    public void ProblemsWith_SavedUnderAnotherId_SaysSo() =>
        Assert.Equal(["The company's identifier isn't the one it is saved under."], Checks.ProblemsWith("red", Company()));

    [Fact]
    public void ProblemsWith_FailsTheValidator_ListsItsProblems() =>
        Assert.Contains("The company's identifier must be 1 to 64 letters, digits, '-' or '_'.", Checks.ProblemsWith("../x", Company("../x")));
}
