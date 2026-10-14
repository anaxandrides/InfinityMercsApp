using System.Text.RegularExpressions;

namespace InfinityMercsApp.Domain.Fireteams;

/// <summary>One allowed-profile slot of a tracked fireteam: its name, the unit it resolved to (if any) and its Max.</summary>
public sealed record FireteamSlot(string? Name, int? UnitId, int? FactionId, string? Max);

/// <summary>One trooper of the company, as matched against fireteam slots.</summary>
public sealed record FireteamMember(string? BaseUnitName, string? Name, int UnitId, int FactionId);

/// <summary>
/// Fireteam level and irregular-trooper rules for a cohesive company: members claim the slots of the
/// tracked fireteam, the number that fit is the level, and those that don't fit are irregular.
/// </summary>
public static class FireteamLevels
{
    public const int MaxLevel = 6;

    // Unbounded slots ("*" or blank) are a large finite number so decrementing never overflows.
    private const int UnboundedSlots = int.MaxValue / 2;

    // Bonuses gained at each level; level 1 (formed) gives none, levels 2-6 add one each.
    private static readonly IReadOnlyList<string?> LevelBonuses =
    [
        null,
        null,
        "BS Attack (+1 SD)",
        "+3 Discover and +1 Dodge",
        "+1 BS",
        "Sixth Sense",
        "+1 WIP",
    ];

    /// <summary>All bonuses up to and including the level, comma separated; null when the level has none.</summary>
    public static string? BonusText(int level)
    {
        if (level <= 0 || level >= LevelBonuses.Count)
        {
            return null;
        }

        var bonuses = LevelBonuses.Skip(1).Take(level).Where(b => b is not null).ToList();
        return bonuses.Count > 0 ? string.Join(", ", bonuses) : null;
    }

    /// <summary>
    /// The fireteam level: how many members claim a slot, clamped to <see cref="MaxLevel"/>.
    /// </summary>
    public static int Level(IReadOnlyList<FireteamMember> members, IReadOnlyList<FireteamSlot> slots) =>
        Math.Clamp(ClaimSlots(members, slots).Count(claimed => claimed), 0, MaxLevel);

    /// <summary>
    /// Whether each member (in order) fits the fireteam. Members are taken in order and each claims the
    /// first matching slot that still has room; a member is irregular when it matches no slot, or every
    /// slot it matches is full.
    /// </summary>
    public static IReadOnlyList<bool> ClaimSlots(IReadOnlyList<FireteamMember> members, IReadOnlyList<FireteamSlot> slots)
    {
        var matchers = slots
            .Select(slot => (Keys: SlotNameKeys(slot.Name), Slot: slot))
            .Where(x => x.Keys.Count > 0 || (x.Slot.UnitId.HasValue && x.Slot.FactionId.HasValue))
            .ToList();
        var remaining = matchers.Select(x => MaxSlots(x.Slot.Max)).ToList();

        var claimed = Enumerable.Repeat(false, members.Count).ToList();
        for (var m = 0; m < members.Count; m++)
        {
            var member = members[m];
            var memberKeys = MemberNameKeys(member.BaseUnitName, member.Name);
            for (var i = 0; i < matchers.Count; i++)
            {
                if (remaining[i] > 0 && Matches(matchers[i].Slot, matchers[i].Keys, member, memberKeys))
                {
                    remaining[i]--;
                    claimed[m] = true;
                    break;
                }
            }
        }

        return claimed;
    }

    /// <summary>A slot's Max as a count: "*" or blank is unbounded, non-positive or unreadable is 0.</summary>
    public static int MaxSlots(string? max)
    {
        if (string.IsNullOrWhiteSpace(max) || max.Trim() == "*")
        {
            return UnboundedSlots;
        }

        return int.TryParse(max.Trim(), out var n) && n > 0 ? n : 0;
    }

    /// <summary>
    /// The name keys a slot answers to: its name without parentheses, plus each alias listed inside
    /// parentheses (split on , / ; |), each with a simple singular/plural variant.
    /// </summary>
    public static HashSet<string> SlotNameKeys(string? slotName)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        AddNameKey(keys, Regex.Replace(slotName ?? string.Empty, @"\([^)]*\)", " "));

        foreach (Match match in Regex.Matches(slotName ?? string.Empty, @"\(([^)]*)\)"))
        {
            var aliases = match.Groups[1].Value.Split(
                [',', '/', ';', '|'],
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            foreach (var alias in aliases)
            {
                AddNameKey(keys, alias);
            }
        }

        return keys;
    }

    // A member matches by resolved unit and faction id when the slot has them, else by any shared name key.
    private static bool Matches(FireteamSlot slot, HashSet<string> slotKeys, FireteamMember member, HashSet<string> memberKeys)
    {
        if (slot.UnitId == member.UnitId && slot.FactionId == member.FactionId)
        {
            return true;
        }

        return memberKeys.Overlaps(slotKeys);
    }

    private static HashSet<string> MemberNameKeys(string? baseUnitName, string? name)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        AddNameKey(keys, baseUnitName);
        AddNameKey(keys, name);
        return keys;
    }

    private static void AddNameKey(HashSet<string> keys, string? value)
    {
        var normalized = FireteamMatching.NormalizeTeamUnitName(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        keys.Add(normalized);
        if (normalized.Length > 1)
        {
            keys.Add(normalized.EndsWith('S') ? normalized[..^1] : $"{normalized}S");
        }
    }
}
