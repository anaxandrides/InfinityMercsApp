using InfinityMercsApp.Api.Leagues;
using InfinityMercsApp.Api.Leagues.Changes;
using InfinityMercsApp.Domain.Leagues;
using static InfinityMercsApp.Api.UnitTests.Leagues.Leagues;

namespace InfinityMercsApp.Api.UnitTests.Leagues.Changes;

public sealed class JoinChangeTests
{
    [Fact]
    public void Apply_NewUser_AddsThemAsAMember()
    {
        var result = new JoinChange("  Nova ", "m-n", DateTimeOffset.UnixEpoch).Apply(Sample(), Outsider);

        var member = Assert.Single(result.League?.Members ?? [], m => m.UserId == Outsider);
        Assert.Equal("m-n", member.MemberId);
        Assert.Equal("Nova", member.DisplayName);
        Assert.Contains(Outsider, result.League?.MemberUserIds ?? []);
    }

    [Fact]
    public void Apply_AlreadyAMember_LeavesTheLeagueAsItWas()
    {
        var league = Sample();

        var result = new JoinChange("Mo again", "m-x", DateTimeOffset.UnixEpoch).Apply(league, Member);

        Assert.Same(league, result.League);
    }

    [Fact]
    public void Apply_LeagueFull_IsRejected()
    {
        var league = Sample();
        league = league.WithMembers(Enumerable.Range(0, LeagueLimits.Members)
            .Select(i => new LeagueMember($"m{i}", $"u{i}", $"P{i}", DateTimeOffset.UnixEpoch)).ToList());

        var result = new JoinChange("Nova", "m-n", DateTimeOffset.UnixEpoch).Apply(league, Outsider);

        Assert.Equal(LeagueChangeStatus.Rejected, result.Status);
    }
}
