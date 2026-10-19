namespace InfinityMercsApp.Domain.Season.Inducements;

/// <summary>A trooper the Troops for Hire table offers.</summary>
/// <param name="Name">The table's name for it, e.g. "Authorized Bounty Hunter".</param>
/// <param name="UnitName">The start of the army unit's name, e.g. "AUTHORIZED BOUNTY HUNTER".</param>
/// <param name="Profile">The table's profile column, e.g. "SMG or BSG".</param>
/// <param name="Weapons">The loadouts allowed, by a weapon each must carry; none means any loadout.</param>
/// <param name="Skills">Skills the hired trooper gains, e.g. the Anaconda's Immunity (POS).</param>
public sealed record HiredTroopOffer(
    string Name,
    string UnitName,
    string Profile,
    IReadOnlyList<string> Weapons,
    int Cost,
    IReadOnlyList<string> Skills)
{
    /// <summary>"Authorized Bounty Hunter (SMG or BSG)", or just the name when any loadout goes.</summary>
    public string Display => Weapons.Count == 0 ? Name : $"{Name} ({Profile})";

    /// <summary>Whether a loadout with these weapons can be hired under this row.</summary>
    public bool Allows(IEnumerable<string> weapons) =>
        Weapons.Count == 0 ||
        weapons.Any(weapon => Weapons.Any(allowed => weapon.Contains(allowed, StringComparison.OrdinalIgnoreCase)));
}
