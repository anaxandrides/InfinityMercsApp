using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Models.Metadata;

namespace InfinityMercsApp.Domain.Season.Game;

/// <summary>
/// The attack modifiers one firing mode actually gets. Modes that are deployed or roll WIP get none; the
/// other modes of a weapon that can be deployed get only the burst bonus; Suppressive Fire gets only the
/// damage reduction.
/// </summary>
public sealed partial record WeaponModeModifiers(int DamageReduction, string? AmmoModifier, int BurstBonus)
{
    public const string SuppressiveFireMode = "Suppressive Fire Mode";

    public static WeaponModeModifiers None { get; } = new(0, null, 0);

    /// <summary>
    /// What <paramref name="weapon"/> gets of <paramref name="card"/>'s modifiers, on a card titled
    /// <paramref name="cardTitle"/> that shows <paramref name="cardModes"/>.
    /// </summary>
    public static WeaponModeModifiers For(Weapon weapon, IReadOnlyList<Weapon> cardModes, string cardTitle, WeaponModeModifiers card)
    {
        var isSuppressiveFire = string.Equals(cardTitle, SuppressiveFireMode, StringComparison.OrdinalIgnoreCase);
        if (IsDeployable(weapon) || RollsWip(weapon))
        {
            return None;
        }

        var burstBonus = isSuppressiveFire ? 0 : card.BurstBonus;
        if (cardModes.Any(IsDeployable))
        {
            return new WeaponModeModifiers(0, null, burstBonus);
        }

        return new WeaponModeModifiers(card.DamageReduction, isSuppressiveFire ? null : card.AmmoModifier, burstBonus);
    }

    /// <summary>The burst with the bonus added; "-" or a non-number is left alone.</summary>
    public string? Burst(string? burst) =>
        BurstBonus > 0 && TryNumber(burst, out var value) ? (value + BurstBonus).ToString(CultureInfo.InvariantCulture) : burst;

    /// <summary>The damage less the reduction, never below 1; "-" or a non-number is left alone.</summary>
    public string? Damage(string? damage) =>
        DamageReduction > 0 && TryNumber(damage, out var value)
            ? Math.Max(1, value - DamageReduction).ToString(CultureInfo.InvariantCulture)
            : damage;

    /// <summary>
    /// The weapon's ammunition with the modifier added: plain N is replaced, an ammo already including it is kept,
    /// otherwise "base+modifier".
    /// </summary>
    public string? Ammo(string? baseAmmo)
    {
        if (string.IsNullOrWhiteSpace(AmmoModifier))
        {
            return baseAmmo;
        }

        if (string.IsNullOrWhiteSpace(baseAmmo) || baseAmmo.Equals("N", StringComparison.OrdinalIgnoreCase))
        {
            return AmmoModifier;
        }

        return baseAmmo.Contains(AmmoModifier, StringComparison.OrdinalIgnoreCase) ? baseAmmo : $"{baseAmmo}+{AmmoModifier}";
    }

    public static bool IsDash([NotNullWhen(false)] string? value) => string.IsNullOrWhiteSpace(value) || value.Trim() == "-";

    private static bool IsDeployable(Weapon weapon) =>
        weapon.Mode?.Contains("Deployable", StringComparison.OrdinalIgnoreCase) ?? false;

    // "BS Weapon (WIP)" rolls WIP rather than BS, so BS Attack modifiers don't apply.
    private static bool RollsWip(Weapon weapon) => weapon.Properties.Any(property => BsWeaponWip().IsMatch(property));

    private static bool TryNumber(string? text, out int value)
    {
        value = 0;
        return !IsDash(text) && int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    [GeneratedRegex(@"\bBS\s+Weapon\s*\(\s*WIP\s*\)", RegexOptions.IgnoreCase)]
    private static partial Regex BsWeaponWip();
}
