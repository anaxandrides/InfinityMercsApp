using InfinityMercsApp.Domain.Season.Troopers;

namespace InfinityMercsApp.Domain.Season.Injuries;

/// <summary>A row of the injury table: a d20 range and the injury it gives.</summary>
public sealed record InjuryTableEntry(int Min, int Max, Injury Injury)
{
    public string Label => Injury.Label();

    public override string ToString() => $"{Min}-{Max} | {Label}";
}
