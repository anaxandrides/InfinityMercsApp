namespace InfinityMercsApp.Domain.Season.Game;

/// <summary>
/// What a Transmutation (X) trooper keeps in its second profile: the gear bought or rolled for it, and Neurocinetics
/// from any source, which the rules apply to both profiles. Anything that changes a stat (stat augments, armour,
/// perks, captain bonuses) stays with the first profile.
/// </summary>
/// <param name="Skills">Skills the gear gives, such as roles and augments.</param>
/// <param name="Equipment">Equipment items, such as Booty results.</param>
public sealed record CarriedGear(IReadOnlyList<string> Skills, IReadOnlyList<string> Equipment)
{
    /// <summary>Nothing carried over.</summary>
    public static CarriedGear None { get; } = new([], []);
}
