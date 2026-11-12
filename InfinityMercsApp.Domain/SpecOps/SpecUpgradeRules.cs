using InfinityMercsApp.Domain.Captain;

namespace InfinityMercsApp.Domain.SpecOps;

/// <summary>
/// The Mercenaries rules for Spec-Ops and Team-Ops troopers. A Spec-Ops is the captain of a Standard, Inspiring
/// Leader or TAG company and picks two different upgrades from its army's table, in place of spending XP. A Team-Ops
/// trooper picks one. Either gains the Lieutenant skill as captain.
/// </summary>
public static class SpecUpgradeRules
{
    /// <summary>How many different upgrades a Spec-Ops picks.</summary>
    public const int SpecOpsUpgrades = 2;

    /// <summary>How many upgrades each Team-Ops trooper picks.</summary>
    public const int TeamOpsUpgrades = 1;

    /// <summary>The skill every Spec-Ops profile carries in the army data.</summary>
    public const string SpecOpsSkill = "Infinity Spec-Ops";

    /// <summary>The skill every Team-Ops profile carries in the army data.</summary>
    public const string TeamOpsSkill = "Infinity Team-Ops";

    /// <summary>Where a Spec-Ops or Team-Ops upgrade comes from, as its purchase line reads.</summary>
    public const string Source = "Spec-Ops";

    /// <summary>
    /// The upgrades together: bonuses added up, the last MOV set, and every weapon, skill and piece of equipment once.
    /// </summary>
    public static TrooperUpgrade Combine(IReadOnlyList<TrooperUpgrade> upgrades)
    {
        var bonuses = upgrades.Aggregate(CaptainStatBonuses.None, (total, upgrade) => new CaptainStatBonuses(
            total.Cc + upgrade.Bonuses.Cc,
            total.Bs + upgrade.Bonuses.Bs,
            total.Ph + upgrade.Bonuses.Ph,
            total.Wip + upgrade.Bonuses.Wip,
            total.Arm + upgrade.Bonuses.Arm,
            total.Bts + upgrade.Bonuses.Bts,
            total.Vitality + upgrade.Bonuses.Vitality));
        var move = upgrades.LastOrDefault(upgrade => upgrade.SetsMove);

        static List<string> Distinct(IEnumerable<string> names) => names.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        return new TrooperUpgrade(
            string.Join("; ", upgrades.Select(upgrade => upgrade.Label)),
            bonuses,
            move?.MoveFirstCm,
            move?.MoveSecondCm,
            Distinct(upgrades.SelectMany(upgrade => upgrade.Weapons)),
            Distinct(upgrades.SelectMany(upgrade => upgrade.Skills)),
            Distinct(upgrades.SelectMany(upgrade => upgrade.Equipment)));
    }
}
