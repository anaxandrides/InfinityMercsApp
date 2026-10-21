using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Validation.Season;

namespace InfinityMercsApp.UnitTests.Validation;

public sealed class SeasonFileValidatorTests
{
    private static SeasonFile Season() => new() { SeasonIdentifier = "s1", CompanyName = "Blue Lions", CompanyIdentifier = "b1" };

    private static IReadOnlyList<string> Problems(SeasonFile season) =>
        SeasonFileValidator.Instance.Validate(season).Errors.Select(error => error.ErrorMessage).ToList();

    [Fact]
    public void Validate_NewSeason_HasNoProblems() => Assert.Empty(Problems(Season()));

    [Fact]
    public void Validate_NoIdentifiersOrName_NamesEach() =>
        Assert.Equal(
            [
                "The season's identifier must be 1 to 64 letters, digits, '-' or '_'.",
                "The season has no company name.",
                "The season's company identifier must be 1 to 64 letters, digits, '-' or '_'."
            ],
            Problems(new SeasonFile()));

    [Fact]
    public void Validate_TwoContractsWithOneNumber_SaysSo()
    {
        var season = Season();
        season.Rounds = [new SeasonRound { RoundIndex = 1 }, new SeasonRound { RoundIndex = 1 }];

        Assert.Equal(["Two of the season's contracts share a number."], Problems(season));
    }

    [Fact]
    public void Validate_NegativeXp_NamesTheTrooper()
    {
        var season = Season();
        season.Rounds = [new SeasonRound { RoundIndex = 1, MissionResults = new SeasonMissionResult { UnitResults = [new SeasonMissionUnitResult { UnitName = "Kestrel", XpGained = -3 }] } }];

        Assert.Equal(["Kestrel gained negative XP."], Problems(season));
    }

    [Fact]
    public void Validate_RetiredAfterAnUnplayedContract_NamesTheTrooper()
    {
        var season = Season();
        season.Retired = [new RetiredTrooper { EntryIndex = 2, Name = "Kestrel", AfterRound = 1 }];

        Assert.Equal(["Kestrel retired after a contract the season hasn't played."], Problems(season));
    }
}
