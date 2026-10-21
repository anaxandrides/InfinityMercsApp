namespace InfinityMercsApp.Domain.Captain;

/// <summary>A captain as configured: the experience it starts with, and the stat upgrades and choices bought with it.</summary>
public sealed record CaptainBuild(int StartingExperience, IReadOnlyList<CaptainStatPick> Stats, IReadOnlyList<CaptainChoicePicks> Choices)
{
    public int SpentExperience => Stats.Sum(stat => stat.Option?.Cost ?? 0) + Choices.Sum(group => group.Cost);

    public int ExperienceRemaining => StartingExperience - SpentExperience;
}
