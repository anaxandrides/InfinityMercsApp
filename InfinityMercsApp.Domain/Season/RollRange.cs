namespace InfinityMercsApp.Domain.Season;

/// <summary>A result of a d20 table and the rolls (Min to Max, inclusive) that give it.</summary>
public sealed record RollRange(int Min, int Max, string Result)
{
    public bool Contains(int roll) => roll >= Min && roll <= Max;
}
