using InfinityMercsApp.Domain.Season.Troopers;

namespace InfinityMercsApp.Domain.Season.Byes;

/// <summary>The one injury a bye round writes off, and the trooper it comes off.</summary>
public sealed record InjuryWriteOff(int EntryIndex, Injury Injury);
