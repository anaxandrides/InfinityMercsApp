using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Domain.Models.Metadata.Wiki;

namespace InfinityMercsApp.ViewModels.Components;

/// <summary>
/// What a weapon card shows: the firing mode, Burst, Damage and ammunition on the left, saving on the
/// right, and the special rules with their wiki links.
/// </summary>
public sealed partial record WeaponCard(
    string Mode,
    IReadOnlyList<WeaponCardStat> LeftStats,
    IReadOnlyList<WeaponCardStat> RightStats,
    IReadOnlyList<WeaponCardRuleRow> RuleRows)
{
    public bool HasMode => Mode.Length > 0;

    public bool HasStats => LeftStats.Count > 0 || RightStats.Count > 0;

    public bool HasSpecialRules => RuleRows.Count > 0;

    /// <summary>
    /// The card for <paramref name="weapon"/>. A Burst bonus is added to a numeric Burst; Damage reduction
    /// lowers a numeric Damage to no less than 1. Placeholder rules such as "[*]" are left out.
    /// </summary>
    public static WeaponCard From(Weapon weapon, int damageReduction = 0, int burstBonus = 0, string? ammo = null)
    {
        var left = new List<WeaponCardStat>();
        if (!IsDash(weapon.Burst))
        {
            left.Add(new WeaponCardStat("Burst", Adjust(weapon.Burst, value => value + Math.Max(0, burstBonus))));
        }

        if (!IsDash(weapon.Damage))
        {
            left.Add(new WeaponCardStat("Damage", damageReduction > 0 ? Adjust(weapon.Damage, value => Math.Max(1, value - damageReduction)) : weapon.Damage));
        }

        if (!string.IsNullOrWhiteSpace(ammo))
        {
            left.Add(new WeaponCardStat("Ammo", ammo));
        }

        var right = new List<WeaponCardStat>();
        if (!IsDash(weapon.Saving))
        {
            right.Add(new WeaponCardStat("Saving", weapon.Saving));
        }

        if (!IsDash(weapon.SavingNum))
        {
            right.Add(new WeaponCardStat("Saving Rolls", weapon.SavingNum));
        }

        var rules = weapon.Properties
            .Where(rule => !Placeholder().IsMatch(rule.Trim()))
            .Select(rule => new LinkedText(rule, WikiLinks.ForName(rule)))
            .ToList();
        var rows = rules.Chunk(2).Select(pair => new WeaponCardRuleRow(pair[0], pair.Length > 1 ? pair[1] : null)).ToList();

        return new WeaponCard(weapon.Mode?.Trim() ?? string.Empty, left, right, rows);
    }

    // Applies the change to a whole-number value; anything else ("2+1", "PH") is shown as it is.
    private static string Adjust(string value, Func<int, int> change) =>
        int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            ? change(number).ToString(CultureInfo.InvariantCulture)
            : value;

    private static bool IsDash([NotNullWhen(false)] string? value) =>
        string.IsNullOrWhiteSpace(value) || value.Trim() == "-";

    [GeneratedRegex(@"^\[\*+\]$")]
    private static partial Regex Placeholder();
}
