using InfinityMercsApp.Web.Platform;

namespace InfinityMercsApp.Web.UnitTests.Platform;

public sealed class SeasonStepsTests
{
    [Theory]
    [InlineData("RankUpPage", SeasonStep.RankUp)]
    [InlineData("ByeRoundPage", SeasonStep.Bye)]
    [InlineData("LootBoxPage", SeasonStep.Loot)]
    [InlineData("PlayModePage", SeasonStep.Play)]
    [InlineData("GameModePage", SeasonStep.Game)]
    [InlineData("InjuriesPage", SeasonStep.Injuries)]
    [InlineData("MissionOutcomePage", SeasonStep.Outcome)]
    [InlineData("ExperiencePage", SeasonStep.Experience)]
    [InlineData("//DowntimePage", SeasonStep.Downtime)]
    public void ForRoute_SeasonStepsPage_IsThatStep(string route, SeasonStep expected) => Assert.Equal(expected, SeasonSteps.ForRoute(route));

    [Fact]
    public void ForRoute_OtherPage_IsNull() => Assert.Null(SeasonSteps.ForRoute("SeasonPage"));

    [Theory]
    [InlineData(SeasonStep.RankUp, true)]
    [InlineData(SeasonStep.Bye, true)]
    [InlineData(SeasonStep.Loot, true)]
    [InlineData(SeasonStep.Play, true)]
    [InlineData(SeasonStep.Game, false)]
    [InlineData(SeasonStep.Injuries, false)]
    [InlineData(SeasonStep.Outcome, false)]
    [InlineData(SeasonStep.Experience, false)]
    [InlineData(SeasonStep.Downtime, false)]
    public void OpensFromAddress_Step_OnlyBeforeTheGameStarts(SeasonStep step, bool expected) => Assert.Equal(expected, step.OpensFromAddress());

    [Fact]
    public void Address_SeasonIdWithSpaces_IsEscapedUnderTheSeason() =>
        Assert.Equal("season/my%20season/rank-up", SeasonStep.RankUp.Address("my season"));
}
