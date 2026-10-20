using InfinityMercsApp.Api.UserDocuments;
using InfinityMercsApp.Domain.Models.Company;

namespace InfinityMercsApp.Api.UnitTests.UserDocuments;

public sealed class CompanyFileChecksTests
{
    private static readonly CompanyFileChecks Checks = new();

    private static SavedCompanyFile Company(string name = "Blue Team", string identifier = "blue", int factions = 1, params IEnumerable<SavedCompanyEntry> entries) => new()
    {
        CompanyName = name,
        CompanyIdentifier = identifier,
        SourceFactions = Enumerable.Range(0, factions).Select(index => new SavedCompanyFaction { FactionId = 101 + index }).ToList(),
        Entries = entries.ToList()
    };

    private static SavedCompanyEntry Trooper(int index, bool captain = false) => new() { EntryIndex = index, IsLieutenant = captain };

    [Fact]
    public void ProblemsWith_CompleteCompany_IsNone() =>
        Assert.Empty(Checks.ProblemsWith(Company(entries: [Trooper(0, captain: true), Trooper(1)])));

    [Fact]
    public void ProblemsWith_NoNameIdentifierOrFactions_NamesEach()
    {
        var problems = Checks.ProblemsWith(Company(" ", "", 0, Trooper(0, captain: true)));

        Assert.Equal(["The company has no name.", "The company has no identifier.", "The company has no factions."], problems);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void ProblemsWith_NotOneCaptain_SaysSo(int captains)
    {
        var problems = Checks.ProblemsWith(Company(entries: Enumerable.Range(0, 3).Select(index => Trooper(index, captain: index < captains))));

        Assert.Equal(["The company must have exactly one Captain."], problems);
    }

    [Fact]
    public void ProblemsWith_SharedEntryIndex_SaysSo() =>
        Assert.Equal(["Two troopers share an entry index."], Checks.ProblemsWith(Company(entries: [Trooper(0, captain: true), Trooper(0)])));
}
