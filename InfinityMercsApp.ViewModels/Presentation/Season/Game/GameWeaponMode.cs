using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Domain.Season.Game;

namespace InfinityMercsApp.Presentation.Season.Game;

/// <summary>One firing mode on a weapon card: its burst, damage and ammunition after the trooper's modifiers.</summary>
public sealed record GameWeaponMode(Weapon Weapon, WeaponModeModifiers Modifiers, string? Ammo, bool IsFirst)
{
    public string Label => string.IsNullOrWhiteSpace(Weapon.Mode) ? Weapon.Name : Weapon.Mode;

    public string? Burst => Modifiers.Burst(Weapon.Burst);

    public string? Damage => Modifiers.Damage(Weapon.Damage);

    public bool HasBurst => !WeaponModeModifiers.IsDash(Burst);

    public bool HasDamage => !WeaponModeModifiers.IsDash(Damage);

    public bool HasAmmo => !string.IsNullOrWhiteSpace(Ammo);
}
