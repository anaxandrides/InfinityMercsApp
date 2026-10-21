using System.Globalization;
using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Season.Troopers;

namespace InfinityMercsApp.Domain.Season.RecruitRolls;

/// <summary>
/// What a recruit table result does, read from its text: stat bonuses ("+1 ARM"), a MOV that replaces the
/// trooper's ("MOV: 8-4"), and named weapons, skills or equipment. Results joined with " + " give each part, and a
/// "TAG: … / Other Troop Types: …" result gives the side for the trooper's type.
/// </summary>
public sealed partial record RollEffects(TrooperChange Change, IReadOnlyList<string> Named)
{
    /// <summary>The part of <paramref name="result"/> for a trooper of <paramref name="unitTypeCode"/>.</summary>
    public static string Side(string result, string? unitTypeCode)
    {
        if (TagOrOther().Match(result) is not { Success: true } sides)
        {
            return result.Trim();
        }

        return sides.Groups[UnitTypeCodes.IsTag(unitTypeCode) ? "tag" : "other"].Value.Trim();
    }

    public static RollEffects Of(string result, string? unitTypeCode)
    {
        var change = TrooperChange.None;
        var named = new List<string>();
        foreach (var part in Side(result, unitTypeCode).Split(" + ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (StatBonus().Match(part) is { Success: true } bonus)
            {
                change += BonusOf(bonus.Groups["stat"].Value, int.Parse(bonus.Groups["value"].Value, CultureInfo.InvariantCulture));
            }
            else if (Move().Match(part) is { Success: true } move)
            {
                change += new TrooperChange
                {
                    Move = new MoveInches(
                        int.Parse(move.Groups["first"].Value, CultureInfo.InvariantCulture),
                        int.Parse(move.Groups["second"].Value, CultureInfo.InvariantCulture))
                };
            }
            else
            {
                named.Add(part);
            }
        }

        return new RollEffects(change, named);
    }

    private static TrooperChange BonusOf(string stat, int value) => stat.ToUpperInvariant() switch
    {
        "CC" => new TrooperChange { Cc = value },
        "BS" => new TrooperChange { Bs = value },
        "PH" => new TrooperChange { Ph = value },
        "WIP" => new TrooperChange { Wip = value },
        "ARM" => new TrooperChange { Arm = value },
        _ => new TrooperChange { Bts = value }
    };

    [GeneratedRegex(@"^\s*TAG:\s*(?<tag>.+?)\s*/\s*Other\s+Troop\s+Types:\s*(?<other>.+?)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex TagOrOther();

    [GeneratedRegex(@"^\+\s*(?<value>\d+)\s*(?<stat>CC|BS|PH|WIP|ARM|BTS)$", RegexOptions.IgnoreCase)]
    private static partial Regex StatBonus();

    [GeneratedRegex(@"^MOV:?\s*(?<first>\d+)\s*-\s*(?<second>\d+)$", RegexOptions.IgnoreCase)]
    private static partial Regex Move();
}
