using InfinityMercsApp.Domain.Models.Leagues;

namespace InfinityMercsApp.Presentation.Leagues;

/// <summary>How members and companies are named on a league's pages.</summary>
public static class LeagueNames
{
    public const string FormerMember = "a former member";

    public const string WithdrawnCompany = "a withdrawn company";

    /// <summary>The member's name; a member who has left is named as such.</summary>
    public static string PlayerOf(LeagueView league, string memberId) =>
        league.Members.FirstOrDefault(member => member.MemberId == memberId)?.DisplayName ?? FormerMember;

    /// <summary>The company entered as <paramref name="seasonId"/>.</summary>
    public static string CompanyOf(LeagueView league, string seasonId) =>
        league.Entries.FirstOrDefault(entry => entry.SeasonId == seasonId)?.CompanyName ?? WithdrawnCompany;
}
