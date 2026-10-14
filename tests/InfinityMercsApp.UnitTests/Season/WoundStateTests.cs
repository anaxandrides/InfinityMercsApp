using InfinityMercsApp.Domain.Season;

namespace InfinityMercsApp.UnitTests.Season;

public sealed class WoundStateTests
{
    [Theory]
    [InlineData(0, false, false, WoundState.Healthy)]
    [InlineData(1, false, false, WoundState.Wounded)]
    [InlineData(2, false, false, WoundState.KnockedOut)]
    [InlineData(2, true, false, WoundState.NwiDown)]
    [InlineData(3, false, true, WoundState.KnockedOutBody2)]
    [InlineData(3, false, false, WoundState.Dead)]
    [InlineData(4, false, true, WoundState.Dead)]
    public void WoundsAgainstTwoVitality(int wounds, bool nwi, bool remotePresence, WoundState expected) =>
        Assert.Equal(expected, WoundStates.For(wounds, startingVitality: 2, nwi, remotePresence));

    [Theory]
    [InlineData(WoundState.Healthy, true)]
    [InlineData(WoundState.NwiDown, true)]
    [InlineData(WoundState.KnockedOut, false)]
    [InlineData(WoundState.KnockedOutBody2, false)]
    [InlineData(WoundState.Dead, false)]
    public void OnlyTroopersStillUpAreConscious(WoundState state, bool conscious) => Assert.Equal(conscious, state.IsConscious());
}
