using InfinityMercsApp.Domain.Season.Troopers;

namespace InfinityMercsApp.ViewModels.Season.Bye;

/// <summary>An injury a bye round can write off, and the trooper carrying it.</summary>
public sealed record ByeInjuryOption(int EntryIndex, string TrooperName, Injury Injury)
{
    public string Label => $"{TrooperName}: {Injury.Label()}";

    public override string ToString() => Label;
}
