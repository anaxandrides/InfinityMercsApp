using InfinityMercsApp.Domain.Models.Leagues;
using InfinityMercsApp.Domain.Validation.Leagues;

namespace InfinityMercsApp.UnitTests.Validation;

public sealed class LeagueValidatorsTests
{
    private static NewLeagueGame Game(params IEnumerable<LeagueGameSide> sides) =>
        new() { PlayedOn = new DateOnly(2026, 11, 1), Mission = "Supplies", Sides = sides.ToList() };

    private static LeagueGameSide Side(string seasonId, GameResult result, int op = 5) =>
        new() { SeasonId = seasonId, Result = result, ObjectivePoints = op };

    [Fact]
    public void NewLeagueValidator_NameAndDisplayName_IsValid() =>
        Assert.True(NewLeagueValidator.Instance.Validate(new NewLeague { Name = "Spring", DisplayName = "Olive", ArmySnapshotId = "2026-11-01.7" }).IsValid);

    [Theory]
    [InlineData("", "Olive")]
    [InlineData("   ", "Olive")]
    [InlineData("Spring", "")]
    public void NewLeagueValidator_MissingName_IsInvalid(string name, string displayName) =>
        Assert.False(NewLeagueValidator.Instance.Validate(new NewLeague { Name = name, DisplayName = displayName }).IsValid);

    [Fact]
    public void NewLeagueValidator_NameTooLong_IsInvalid() =>
        Assert.False(NewLeagueValidator.Instance.Validate(new NewLeague { Name = new string('a', 61), DisplayName = "Olive" }).IsValid);

    [Fact]
    public void NewLeagueValidator_SnapshotIdWithSpaces_IsInvalid() =>
        Assert.False(NewLeagueValidator.Instance.Validate(new NewLeague { Name = "Spring", DisplayName = "Olive", ArmySnapshotId = "snap 1" }).IsValid);

    [Fact]
    public void JoinLeagueValidator_NoName_IsInvalid() =>
        Assert.False(JoinLeagueValidator.Instance.Validate(new JoinLeague { DisplayName = " " }).IsValid);

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(100_001, false)]
    public void LeagueEntryUpdateValidator_Renown_MustBeInRange(int renown, bool valid) =>
        Assert.Equal(valid, LeagueEntryUpdateValidator.Instance.Validate(new LeagueEntryUpdate { Renown = renown }).IsValid);

    [Fact]
    public void LeagueArmyUpdateValidator_Current_IsValid() =>
        Assert.True(LeagueArmyUpdateValidator.Instance.Validate(new LeagueArmyUpdate()).IsValid);

    [Fact]
    public void NewLeagueGameValidator_WinAndLoss_IsValid() =>
        Assert.True(NewLeagueGameValidator.Instance.Validate(Game(Side("a", GameResult.Win), Side("b", GameResult.Loss))).IsValid);

    [Fact]
    public void NewLeagueGameValidator_Draw_IsValid() =>
        Assert.True(NewLeagueGameValidator.Instance.Validate(Game(Side("a", GameResult.Draw), Side("b", GameResult.Draw))).IsValid);

    [Fact]
    public void NewLeagueGameValidator_OneSide_IsInvalid() =>
        Assert.False(NewLeagueGameValidator.Instance.Validate(Game(Side("a", GameResult.Win))).IsValid);

    [Fact]
    public void NewLeagueGameValidator_SameCompanyTwice_IsInvalid() =>
        Assert.False(NewLeagueGameValidator.Instance.Validate(Game(Side("a", GameResult.Win), Side("a", GameResult.Loss))).IsValid);

    [Fact]
    public void NewLeagueGameValidator_TwoWinners_IsInvalid() =>
        Assert.False(NewLeagueGameValidator.Instance.Validate(Game(Side("a", GameResult.Win), Side("b", GameResult.Win))).IsValid);

    [Fact]
    public void NewLeagueGameValidator_WinAndDraw_IsInvalid() =>
        Assert.False(NewLeagueGameValidator.Instance.Validate(Game(Side("a", GameResult.Win), Side("b", GameResult.Draw))).IsValid);

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void NewLeagueGameValidator_ObjectivePointsOutOfRange_IsInvalid(int op) =>
        Assert.False(NewLeagueGameValidator.Instance.Validate(Game(Side("a", GameResult.Win, op), Side("b", GameResult.Loss))).IsValid);

    [Fact]
    public void NewLeagueGameValidator_NoDate_IsInvalid()
    {
        var game = Game(Side("a", GameResult.Win), Side("b", GameResult.Loss));
        game.PlayedOn = default;

        Assert.False(NewLeagueGameValidator.Instance.Validate(game).IsValid);
    }
}
