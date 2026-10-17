using System.Globalization;
using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Models.Perks.Catalog;

namespace InfinityMercsApp.Domain.Season.Troopers;

/// <summary>
/// What a perk does to a trooper, read from its name: "+3 CC" and the like change attributes, "+1 Wounds Skill"
/// steps up the wounds path, "MOV 6-4" and "S7" replace MOV and silhouette, and anything else is a perk skill.
/// </summary>
public static partial class PerkEffects
{
    public static TrooperChange For(string perkId) =>
        CompanyPerkCatalog.FindById(perkId) is { } perk ? FromName(perk.Description) : TrooperChange.None;

    public static TrooperChange FromName(string perkName)
    {
        var change = TrooperChange.None;
        foreach (var part in Parts(perkName))
        {
            change += PartChange(part);
        }

        return change;
    }

    private static TrooperChange PartChange(string part)
    {
        var attribute = AttributeBonus().Match(part);
        if (attribute.Success)
        {
            var value = int.Parse(attribute.Groups["value"].Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            return attribute.Groups["stat"].Value.ToUpperInvariant() switch
            {
                "CC" => new() { Cc = value },
                "BS" => new() { Bs = value },
                "PH" => new() { Ph = value },
                "WIP" => new() { Wip = value },
                "ARM" => new() { Arm = value },
                _ => new() { Bts = value }
            };
        }

        var wounds = WoundsSkillBonus().Match(part);
        if (wounds.Success)
        {
            return new() { WoundsSkillSteps = int.Parse(wounds.Groups["value"].Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture) };
        }

        var move = MoveValue().Match(part);
        if (move.Success)
        {
            return new() { Move = new MoveInches(int.Parse(move.Groups["first"].Value, CultureInfo.InvariantCulture), int.Parse(move.Groups["second"].Value, CultureInfo.InvariantCulture)) };
        }

        var silhouette = SilhouetteValue().Match(part);
        if (silhouette.Success)
        {
            return new() { Silhouette = int.Parse(silhouette.Groups["value"].Value, CultureInfo.InvariantCulture) };
        }

        return new() { PerkSkills = [part] };
    }

    // "+2 PH + CC Attack (SR-2)" and "S7, +1 ARM, SR-1" split into their parts; commas inside brackets stay.
    private static IEnumerable<string> Parts(string perkName)
    {
        var depth = 0;
        var start = 0;
        for (var i = 0; i < perkName.Length; i++)
        {
            switch (perkName[i])
            {
                case '(':
                    depth++;
                    break;
                case ')':
                    depth = Math.Max(0, depth - 1);
                    break;
                case ',' when depth == 0:
                    yield return perkName[start..i].Trim();
                    start = i + 1;
                    break;
                case '+' when depth == 0 && i > 0 && perkName[i - 1] == ' ' && i + 1 < perkName.Length && perkName[i + 1] == ' ':
                    yield return perkName[start..i].Trim();
                    start = i + 1;
                    break;
            }
        }

        var last = perkName[start..].Trim();
        if (last.Length > 0)
        {
            yield return last;
        }
    }

    [GeneratedRegex(@"^(?<value>[+-]\d+)\s*(?<stat>CC|BS|PH|WIP|ARM|BTS)$", RegexOptions.IgnoreCase)]
    private static partial Regex AttributeBonus();

    [GeneratedRegex(@"^(?<value>[+-]\d+)\s*Wounds?\s+Skill$", RegexOptions.IgnoreCase)]
    private static partial Regex WoundsSkillBonus();

    [GeneratedRegex(@"^(?:MOV|Move)\s+(?<first>\d+)-(?<second>\d+)$", RegexOptions.IgnoreCase)]
    private static partial Regex MoveValue();

    [GeneratedRegex(@"^S(?<value>\d+)$", RegexOptions.IgnoreCase)]
    private static partial Regex SilhouetteValue();
}
