using InfinityMercsApp.Domain.Leagues;

namespace InfinityMercsApp.UnitTests.Leagues;

public sealed class LeagueLinksTests
{
    [Fact]
    public void Join_Code_IsTheSitesJoinPage() => Assert.Equal("https://www.infinitythemercs.com/join/ABC234", LeagueLinks.Join("ABC234"));
}
