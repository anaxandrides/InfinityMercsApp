using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.Domain.Season.Game;

/// <summary>
/// The profile a Transmutation (X) trooper switches to once it has taken <paramref name="Threshold"/> wounds, e.g.
/// Battle-Ravaged or Inactive Symbiont Armor. It is the army profile as printed: perks, armour and other bonuses only
/// ever apply to the first profile. Its VITA is the trooper's own, since wounds carry over.
/// </summary>
public sealed record TransmutedForm(
    string Name,
    int Threshold,
    IReadOnlyList<StatValue> Statline,
    IReadOnlyList<string> Skills,
    IReadOnlyList<string> Equipment);
