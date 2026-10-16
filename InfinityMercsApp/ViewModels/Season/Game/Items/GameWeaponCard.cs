using InfinityMercsApp.Domain.Models.Metadata.Ranges;

namespace InfinityMercsApp.ViewModels.Season.Game.Items;

/// <summary>
/// A weapon card: its modes and the range band of the first mode that has one. A weapon missing from the
/// metadata gets a card with just its name.
/// </summary>
public sealed record GameWeaponCard(string Title, IReadOnlyList<GameWeaponMode> Modes, bool HasXVisor)
{
    public bool IsMatched => Modes.Count > 0;

    public WeaponDistance? Range => Modes.Select(mode => mode.Weapon.Distance).FirstOrDefault(distance => distance is not null);

    public bool HasRange => Range is not null;

    public static GameWeaponCard Unmatched(string title) => new(title, [], false);
}
