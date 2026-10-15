using System.Globalization;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Season.Experience;

namespace InfinityMercsApp.Domain.Season.Downtime;

/// <summary>
/// How a downtime choice is resolved: who may perform it, the target they roll against, how a roll reads,
/// and what the outcome does.
/// </summary>
public static class DowntimeRules
{
    /// <summary>Who must perform <paramref name="choice"/>: the event's participant, the Captain for a Captain choice, else any merc.</summary>
    public static ParticipantKind RequiredParticipant(DowntimeEvent downtimeEvent, DowntimeChoice choice)
    {
        if (downtimeEvent.RequiredParticipant != ParticipantKind.None)
        {
            return downtimeEvent.RequiredParticipant;
        }

        return choice.Traits.Any(trait => trait.Kind == TraitKind.Captain) ? ParticipantKind.Captain : ParticipantKind.AnyMerc;
    }

    public static string ParticipantLabel(ParticipantKind kind) => kind switch
    {
        ParticipantKind.Mvp => "MVP (from last contract)",
        ParticipantKind.Captain => "the Captain",
        ParticipantKind.Renowned => "the Most Renowned merc",
        _ => "a Merc"
    };

    /// <summary>The troopers who can be <paramref name="kind"/>; for Renowned, everyone tied for the most renown.</summary>
    public static IReadOnlyList<ExperienceUnitResult> Candidates(IReadOnlyList<ExperienceUnitResult> units, ParticipantKind kind)
    {
        switch (kind)
        {
            case ParticipantKind.Mvp:
                return units.Where(unit => unit.IsMvp).ToList();
            case ParticipantKind.Captain:
                return units.Where(unit => unit.IsLieutenant).ToList();
            case ParticipantKind.Renowned:
                if (units.Count == 0)
                {
                    return [];
                }

                var topRenown = units.Max(unit => unit.Renown);
                return units.Where(unit => unit.Renown == topRenown).ToList();
            default:
                return units.ToList();
        }
    }

    /// <summary>The skills or equipment <paramref name="choice"/> requires, e.g. "Hacker".</summary>
    public static IReadOnlyList<string> Requirements(DowntimeChoice choice) => choice.Traits
        .Where(trait => trait.Kind == TraitKind.Requirement)
        .Select(trait => trait.Detail)
        .OfType<string>()
        .Where(detail => !string.IsNullOrWhiteSpace(detail))
        .ToList();

    /// <summary>The requirements of <paramref name="choice"/> that <paramref name="unit"/>'s skills and equipment don't mention.</summary>
    public static IReadOnlyList<string> MissingRequirements(DowntimeChoice choice, ExperienceUnitResult unit)
    {
        var haystack = $"{unit.Skills} {unit.Equipment}";
        return Requirements(choice)
            .Where(requirement => !haystack.Contains(requirement, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public static bool HasP2P(DowntimeChoice choice) => choice.Traits.Any(trait => trait.Kind == TraitKind.P2P);

    /// <summary>
    /// The number <paramref name="unit"/> must roll under: PS 10, PS 10 plus ARM, or a stat plus its modifier.
    /// Null for a stat test without a participant or with a stat that isn't a number.
    /// </summary>
    public static int? TestTarget(DowntimeTest test, ExperienceUnitResult? unit)
    {
        switch (test.Kind)
        {
            case TestKind.FixedPs10:
                return 10;
            case TestKind.FixedPs10PlusArm:
                return unit is null ? 10 : 10 + ParseOrZero(unit.UnitArm);
            case TestKind.Stat:
                if (unit is null)
                {
                    return null;
                }

                var raw = test.Stat switch
                {
                    Stat.Ph => unit.UnitPh,
                    Stat.Bs => unit.UnitBs,
                    Stat.Cc => unit.UnitCc,
                    Stat.Wip => unit.UnitWip,
                    _ => "-"
                };
                return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                    ? value + test.Modifier
                    : null;
            default:
                return null;
        }
    }

    /// <summary>
    /// A d20 against <paramref name="target"/>: equal is a critical, under passes. Above 20 every roll passes, and
    /// 20 or anything up to the excess over 20 is a critical.
    /// </summary>
    public static PassFailOutcome Classify(int roll, int target)
    {
        if (target <= 20)
        {
            return roll == target ? PassFailOutcome.CritPass
                : roll < target ? PassFailOutcome.Pass
                : PassFailOutcome.Fail;
        }

        return roll == 20 || roll <= target - 20 ? PassFailOutcome.CritPass : PassFailOutcome.Pass;
    }

    /// <summary>The target, with the extra critical range spelled out when it is over 20.</summary>
    public static string TargetText(int target) =>
        target > 20 ? $"{target}  (crit on 20 and 1–{target - 20})" : target.ToString(CultureInfo.InvariantCulture);

    public static string OutcomeLabel(PassFailOutcome outcome) => outcome switch
    {
        PassFailOutcome.Fail => "FAIL",
        PassFailOutcome.Pass => "PASS",
        PassFailOutcome.CritPass => "CRITICAL PASS",
        _ => "—"
    };

    /// <summary>Whether <paramref name="choice"/> wins a pistol on <paramref name="outcome"/>.</summary>
    public static bool WinsPistol(DowntimeChoice choice, PassFailOutcome outcome) =>
        IsPass(outcome) && ActiveTraits(choice, outcome).Any(trait => trait.Kind == TraitKind.Weapon);

    /// <summary>The skills the participant gains from <paramref name="choice"/> on <paramref name="outcome"/>.</summary>
    public static IReadOnlyList<string> SkillsAwarded(DowntimeChoice choice, PassFailOutcome outcome) =>
        IsPass(outcome)
            ? ActiveTraits(choice, outcome)
                .Where(trait => trait.Kind == TraitKind.Skill && !string.IsNullOrWhiteSpace(trait.Detail))
                .Select(trait => trait.Detail ?? string.Empty)
                .ToList()
            : [];

    /// <summary>Whether <paramref name="choice"/> injures the participant on <paramref name="outcome"/> (an Attack failed).</summary>
    public static bool InjuresParticipant(DowntimeChoice choice, PassFailOutcome outcome) =>
        outcome == PassFailOutcome.Fail && ActiveTraits(choice, outcome).Any(trait => trait.Kind == TraitKind.Attack);

    /// <summary>
    /// What happens, one line per effect. <paramref name="pistol"/> is the pistol rolled for a Weapon trait.
    /// </summary>
    public static IReadOnlyList<string> ConsequenceLines(
        DowntimeChoice choice,
        PassFailOutcome outcome,
        string? participantName,
        PistolRoll? pistol)
    {
        var name = string.IsNullOrWhiteSpace(participantName) ? "selected merc" : participantName;
        var lines = new List<string>();

        foreach (var trait in ActiveTraits(choice, outcome))
        {
            switch (trait.Kind)
            {
                case TraitKind.Chaotic:
                    lines.Add(outcome switch
                    {
                        PassFailOutcome.Fail => "Lose 1 Notoriety",
                        PassFailOutcome.Pass => "Gain 1 Notoriety",
                        PassFailOutcome.CritPass => "Gain 2 Notoriety",
                        _ => ""
                    });
                    break;
                case TraitKind.Lawful:
                    lines.Add(outcome switch
                    {
                        PassFailOutcome.Fail => "Gain 1 Notoriety",
                        PassFailOutcome.Pass => "Lose 1 Notoriety",
                        PassFailOutcome.CritPass => "Lose 2 Notoriety",
                        _ => ""
                    });
                    break;
                case TraitKind.Attack:
                    if (outcome == PassFailOutcome.Fail)
                    {
                        lines.Add($"Injury inflicted on {name}");
                    }

                    break;
                case TraitKind.Cr:
                    lines.Add(outcome switch
                    {
                        PassFailOutcome.Fail => "Lose 5 CR",
                        PassFailOutcome.Pass => "Gain 5 CR",
                        PassFailOutcome.CritPass => "Gain 6 CR",
                        _ => ""
                    });
                    break;
                case TraitKind.Xp:
                    lines.Add(outcome switch
                    {
                        PassFailOutcome.Pass => $"{name} gains 2 XP",
                        PassFailOutcome.CritPass => $"{name} gains 3 XP",
                        _ => ""
                    });
                    break;
                case TraitKind.Weapon:
                    if (IsPass(outcome) && pistol is not null)
                    {
                        var prefix = outcome == PassFailOutcome.CritPass ? "Roll a random pistol (+1 SD)" : "Roll a random pistol";
                        lines.Add($"{prefix}: {pistol.Display}");
                    }

                    break;
                case TraitKind.Swc:
                    if (IsPass(outcome))
                    {
                        lines.Add("Gain 0.5 SWC");
                    }

                    break;
                case TraitKind.P2P:
                    lines.Add(outcome switch
                    {
                        PassFailOutcome.Fail => "Spent CR are lost; SR not increased",
                        PassFailOutcome.Pass => "Spent CR are lost; SR increased by 1 per CR spent",
                        PassFailOutcome.CritPass => "Spent CR are kept; SR increased by 1 per CR spent",
                        _ => ""
                    });
                    break;
                case TraitKind.Skill:
                    if (IsPass(outcome))
                    {
                        lines.Add($"{name} gains skill: {trait.Detail ?? "(see event)"}");
                    }

                    break;
                case TraitKind.Lt:
                    if (IsPass(outcome))
                    {
                        lines.Add("You may select any member of your team to be the new Captain");
                    }

                    break;
                case TraitKind.Captain:
                    lines.Add("Captain participation required (see event)");
                    break;
                case TraitKind.Requirement:
                    lines.Add($"Requirement: {trait.Detail ?? "(see event)"}");
                    break;
                case TraitKind.Note:
                    lines.Add(trait.Detail ?? string.Empty);
                    break;
            }
        }

        return lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
    }

    /// <summary>The gains and losses to record; CR spent on a P2P test is kept on a critical pass.</summary>
    public static DowntimeTotals Totals(DowntimeChoice choice, PassFailOutcome outcome, int crSpent)
    {
        var crGain = 0;
        var notorietyGain = 0;
        var xpGain = 0;
        var spentCr = 0;
        var swcGain = 0.0;
        var other = new List<string>();

        foreach (var trait in ActiveTraits(choice, outcome))
        {
            switch (trait.Kind)
            {
                case TraitKind.Chaotic:
                    notorietyGain += outcome switch
                    {
                        PassFailOutcome.Fail => -1,
                        PassFailOutcome.Pass => 1,
                        PassFailOutcome.CritPass => 2,
                        _ => 0
                    };
                    break;
                case TraitKind.Lawful:
                    notorietyGain += outcome switch
                    {
                        PassFailOutcome.Fail => 1,
                        PassFailOutcome.Pass => -1,
                        PassFailOutcome.CritPass => -2,
                        _ => 0
                    };
                    break;
                case TraitKind.Cr:
                    crGain += outcome switch
                    {
                        PassFailOutcome.Fail => -5,
                        PassFailOutcome.Pass => 5,
                        PassFailOutcome.CritPass => 6,
                        _ => 0
                    };
                    break;
                case TraitKind.Xp:
                    xpGain += XpFor(outcome);
                    break;
                case TraitKind.P2P:
                    if (outcome != PassFailOutcome.CritPass)
                    {
                        spentCr += crSpent;
                    }

                    break;
                case TraitKind.Weapon:
                    if (IsPass(outcome))
                    {
                        other.Add(outcome == PassFailOutcome.CritPass ? "Random pistol (+1 SD)" : "Random pistol");
                    }

                    break;
                case TraitKind.Skill:
                    if (IsPass(outcome))
                    {
                        other.Add($"Skill awarded: {trait.Detail ?? "(see event)"}");
                    }

                    break;
                case TraitKind.Lt:
                    if (IsPass(outcome))
                    {
                        other.Add("New Captain may be selected");
                    }

                    break;
                case TraitKind.Attack:
                    if (outcome == PassFailOutcome.Fail)
                    {
                        other.Add("Participant injured");
                    }

                    break;
                case TraitKind.Swc:
                    if (IsPass(outcome))
                    {
                        swcGain += 0.5;
                    }

                    break;
                case TraitKind.Note:
                    if (!string.IsNullOrWhiteSpace(trait.Detail))
                    {
                        other.Add(trait.Detail);
                    }

                    break;
            }
        }

        return new DowntimeTotals(crGain, notorietyGain, xpGain, spentCr, swcGain, other);
    }

    /// <summary>Gives <paramref name="participant"/> the XP and injury the outcome brings.</summary>
    public static void ApplyToParticipant(DowntimeChoice choice, PassFailOutcome outcome, ExperienceUnitResult participant)
    {
        foreach (var trait in ActiveTraits(choice, outcome))
        {
            if (trait.Kind == TraitKind.Xp)
            {
                participant.XpData.DowntimeBonusXp += XpFor(outcome);
            }
            else if (trait.Kind == TraitKind.Attack && outcome == PassFailOutcome.Fail)
            {
                participant.GainedInjury = true;
            }
        }
    }

    private static int ParseOrZero(string value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;

    private static bool IsPass(PassFailOutcome outcome) => outcome is PassFailOutcome.Pass or PassFailOutcome.CritPass;

    private static int XpFor(PassFailOutcome outcome) => outcome switch
    {
        PassFailOutcome.Pass => 2,
        PassFailOutcome.CritPass => 3,
        _ => 0
    };

    // A negated trait only counts when the test fails.
    private static IEnumerable<DowntimeTrait> ActiveTraits(DowntimeChoice choice, PassFailOutcome outcome) =>
        choice.Traits.Where(trait => !trait.Negated || outcome == PassFailOutcome.Fail);
}
