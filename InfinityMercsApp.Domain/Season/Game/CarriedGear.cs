using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.Domain.Season.Game;

/// <summary>
/// What a Transmutation (X) trooper keeps in its second profile: the gear bought or rolled for it, armour aside, and
/// Neurocinetics from any source, which the rules apply to both profiles.
/// </summary>
/// <param name="Skills">Skills the gear gives, such as roles and augments.</param>
/// <param name="Equipment">Equipment items, such as Booty results.</param>
/// <param name="Overrides">Augments that set a stat outright.</param>
public sealed record CarriedGear(IReadOnlyList<string> Skills, IReadOnlyList<string> Equipment, IReadOnlyList<StatOverride> Overrides)
{
    /// <summary>Nothing carried over.</summary>
    public static CarriedGear None { get; } = new([], [], []);
}
