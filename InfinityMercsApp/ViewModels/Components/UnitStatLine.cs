using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.ViewModels.Components;

/// <summary>A statline as the unit display shows it; every value is display text, "-" when unknown.</summary>
public sealed record UnitStatLine
{
    public string Mov { get; init; } = "-";
    public string Cc { get; init; } = "-";
    public string Bs { get; init; } = "-";
    public string Ph { get; init; } = "-";
    public string Wip { get; init; } = "-";
    public string Arm { get; init; } = "-";
    public string Bts { get; init; } = "-";

    /// <summary>VITA, or STR for structure troopers.</summary>
    public string VitalityHeader { get; init; } = Stat.Vita.Key();

    public string Vitality { get; init; } = "-";
    public string S { get; init; } = "-";
    public string Ava { get; init; } = "-";

    /// <summary>No unit: every value "-".</summary>
    public static UnitStatLine Empty { get; } = new();
}
