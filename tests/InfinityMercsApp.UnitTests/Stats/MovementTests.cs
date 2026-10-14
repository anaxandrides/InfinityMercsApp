using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.UnitTests.Stats;

public sealed class MovementTests
{
    [Theory]
    [InlineData("4-4", 10, 10)]
    [InlineData("10-10", 10, 10)]
    [InlineData("6\"-2\"", 15, 5)]
    [InlineData("15cm-5cm", 15, 5)]
    [InlineData(" 10 / 5 ", 10, 5)]
    public void SavedMoveIsReadInCentimetres(string text, int first, int second)
    {
        Assert.True(Movement.TryParse(text, out var movement));
        Assert.Equal(new Movement(first, second), movement);
    }

    [Theory]
    [InlineData("-")]
    [InlineData("fast")]
    [InlineData(null)]
    public void UnreadableMoveIsRejected(string? text) => Assert.False(Movement.TryParse(text, out _));

    [Theory]
    [InlineData(false, "10-5")]
    [InlineData(true, "4-2")]
    public void FormatsInTheChosenUnits(bool inInches, string expected) =>
        Assert.Equal(expected, new Movement(10, 5).Format(inInches));

    [Fact]
    public void NoMovementShowsADash()
    {
        Assert.Equal("-", Movement.Format(null, inInches: true));
        Assert.Equal("-", new Movement(0, 5).Format(inInches: false));
        Assert.Null(Movement.From(10, null));
    }

    [Fact]
    public void AProfileMovesByItsFirstTwoValues()
    {
        Assert.Equal(new Movement(15, 10), new UnitProfile { MoveCm = [15, 10, 5] }.Movement);
        Assert.Null(new UnitProfile { MoveCm = [15] }.Movement);
    }
}
