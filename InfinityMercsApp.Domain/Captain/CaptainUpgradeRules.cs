using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.Domain.Captain;

/// <summary>
/// The rules for improving a company captain: which stat upgrades exist, what they cost, and how much
/// experience a captain starts with.
/// </summary>
public static class CaptainUpgradeRules
{
    /// <summary>Units cost at most this many points; whatever a captain's unit costs less becomes starting experience.</summary>
    public const int MaxUnitCost = 28;

    /// <summary>
    /// The Infinity Spec-Ops chart, per stat: the bonus each tier gives over the base value, the experience each
    /// tier adds, and an optional hard cap the stat can never exceed. Index 0 is "no upgrade".
    /// </summary>
    private static readonly IReadOnlyDictionary<Stat, StatDefinition> SpecOpsChart =
        new Dictionary<Stat, StatDefinition>
        {
            [Stat.Cc] = new([0, 2, 5, 10], [0, 2, 3, 5]),
            [Stat.Bs] = new([0, 1, 2, 3], [0, 2, 3, 5]),
            [Stat.Ph] = new([0, 1, 3], [0, 2, 3], HardCap: 14),
            [Stat.Wip] = new([0, 1, 3, 6], [0, 2, 3, 5], HardCap: 15),
            [Stat.Arm] = new([0, 1, 3], [0, 5, 5]),
            [Stat.Bts] = new([0, 3, 6, 9], [0, 2, 3, 5]),
            [Stat.Vita] = new([0, 1], [0, 10], HardCap: 2),
            [Stat.Str] = new([0, 1], [0, 10], HardCap: 2)
        };

    /// <summary>
    /// The Mercenaries chart for captains that are not Spec Ops, laid out as <see cref="SpecOpsChart"/>. Its costs
    /// are totals in the rules (CC +5 for 5 XP, BS +3 for 10 XP), so each tier adds the difference; BTS is 2 XP a tier.
    /// </summary>
    private static readonly IReadOnlyDictionary<Stat, StatDefinition> MercenariesChart =
        new Dictionary<Stat, StatDefinition>
        {
            [Stat.Cc] = new([0, 2, 5, 10], [0, 2, 3, 5]),
            [Stat.Bs] = new([0, 1, 2, 3], [0, 2, 3, 5], HardCap: 15),
            [Stat.Ph] = new([0, 1, 2, 3], [0, 2, 3, 5], HardCap: 14),
            [Stat.Wip] = new([0, 1, 2, 3], [0, 2, 3, 5], HardCap: 15),
            [Stat.Arm] = new([0, 1, 3], [0, 5, 5]),
            [Stat.Bts] = new([0, 3, 6, 9], [0, 2, 2, 2], HardCap: 9),
            [Stat.Vita] = new([0, 1], [0, 10], HardCap: 2),
            [Stat.Str] = new([0, 1], [0, 10], HardCap: 2)
        };

    /// <summary>The experience a captain starts with: the override when given, else what the unit costs under <see cref="MaxUnitCost"/>.</summary>
    public static int StartingExperience(int unitCost, int? overrideExperience = null) =>
        overrideExperience ?? Math.Max(0, MaxUnitCost - unitCost);

    /// <summary>
    /// The upgrade levels of a stat for a captain with <paramref name="baseValue"/>, starting with "no upgrade".
    /// Costs accumulate across tiers. A hard cap clamps the bonus and stops further tiers once reached.
    /// A stat captains cannot improve (MOV, S, AVA, W) offers only "no upgrade".
    /// </summary>
    public static IReadOnlyList<CaptainStatOption> StatOptions(Stat stat, int baseValue, CaptainStatChart chart)
    {
        var options = new List<CaptainStatOption> { new(stat, 0, 0, 0) };
        var definitions = chart == CaptainStatChart.SpecOps ? SpecOpsChart : MercenariesChart;
        if (!definitions.TryGetValue(stat, out var definition))
        {
            return options;
        }

        var currentValue = baseValue;
        var cumulativeCost = 0;
        for (var tier = 1; tier <= definition.MaxTier; tier++)
        {
            if (definition.HardCap is int cap && currentValue >= cap)
            {
                break;
            }

            var targetValue = baseValue + definition.BonusesByTier[tier];
            if (definition.HardCap is int hardCap)
            {
                targetValue = Math.Min(targetValue, hardCap);
            }

            cumulativeCost += definition.CostsByTier[tier];
            options.Add(new CaptainStatOption(stat, tier, Math.Max(0, targetValue - baseValue), cumulativeCost));
            currentValue = targetValue;
        }

        return options;
    }

    private sealed record StatDefinition(IReadOnlyList<int> BonusesByTier, IReadOnlyList<int> CostsByTier, int? HardCap = null)
    {
        public int MaxTier => BonusesByTier.Count - 1;
    }
}
