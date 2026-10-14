namespace InfinityMercsApp.Domain.Models.Perks;

/// <summary>A trooper rank: the experience needed to reach it, its name, and its level (1 for Newbie).</summary>
public sealed record ExperienceRank(int MinXp, string Name, int Level);

public static class CompanyUnitExperienceRanks
{
    public const string UnrankedName = "Unranked";

    /// <summary>The ranks from lowest to highest.</summary>
    public static IReadOnlyList<ExperienceRank> Ranks { get; } =
    [
        new(5, "Newbie", 1),
        new(10, "Hired Gun", 2),
        new(30, "Hitman", 3),
        new(50, "Operative", 4),
        new(75, "Veteran", 5),
        new(105, "Officer", 6),
        new(140, "Legend", 7)
    ];

    /// <summary>The highest rank the experience reaches; null below the first rank.</summary>
    public static ExperienceRank? RankFor(int experiencePoints) =>
        Ranks.LastOrDefault(rank => Math.Max(0, experiencePoints) >= rank.MinXp);

    public static string GetRankName(int experiencePoints) => RankFor(experiencePoints)?.Name ?? UnrankedName;

    /// <summary>The rank's level, or 0 when unranked.</summary>
    public static int GetRankLevel(int experiencePoints) => RankFor(experiencePoints)?.Level ?? 0;
}
