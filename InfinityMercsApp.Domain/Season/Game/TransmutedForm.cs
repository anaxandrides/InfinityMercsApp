using System.Globalization;
using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.Domain.Season.Game;

/// <summary>
/// The profile a Transmutation (X) trooper switches to once it has taken <paramref name="Threshold"/> wounds, e.g.
/// Battle-Ravaged or Inactive Symbiont Armor. It is the army profile as printed: perks, armour and other bonuses only
/// ever apply to the first profile, though gear carries over (see <see cref="Carrying"/>). Its VITA is the trooper's
/// own, since wounds carry over.
/// </summary>
public sealed record TransmutedForm(
    string Name,
    int Threshold,
    IReadOnlyList<StatValue> Statline,
    IReadOnlyList<string> Skills,
    IReadOnlyList<string> Equipment)
{
    /// <summary>
    /// The form with the trooper's gear on it: the skills and equipment added where it lacks them, and stat augments
    /// setting their stat. VITA and STR stay the trooper's own.
    /// </summary>
    public TransmutedForm Carrying(CarriedGear gear) => this with
    {
        Statline = Statline.Select(stat => Overridden(stat, gear.Overrides)).ToList(),
        Skills = WithDistinct(Skills, gear.Skills),
        Equipment = WithDistinct(Equipment, gear.Equipment)
    };

    private static StatValue Overridden(StatValue stat, IReadOnlyList<StatOverride> overrides) =>
        stat.Stat is Stat.Vita or Stat.Str || overrides.LastOrDefault(item => item.Stat == stat.Stat) is not { } statOverride
            ? stat
            : stat with { Value = statOverride.Value.ToString(CultureInfo.InvariantCulture) };

    private static List<string> WithDistinct(IReadOnlyList<string> lines, IReadOnlyList<string> added) =>
        [.. lines, .. added.Where(line => !lines.Contains(line, StringComparer.OrdinalIgnoreCase)).Distinct(StringComparer.OrdinalIgnoreCase)];
}
