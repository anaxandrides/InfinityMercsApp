using System.Globalization;
using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.Domain.Season.Troopers;

/// <summary>
/// A trooper's stat line, skills and characteristics as text, as saved for a company entry. <see cref="With"/>
/// gives the line after a <see cref="TrooperChange"/>; values that are not numbers ("-") are left alone.
/// </summary>
public sealed partial record TrooperProfile(
    string Move,
    string Cc,
    string Bs,
    string Ph,
    string Wip,
    string Arm,
    string Bts,
    string Vitality,
    string Silhouette,
    IReadOnlyList<string> Skills,
    IReadOnlyList<string> Characteristics)
{
    public const string Dogged = "Dogged";
    public const string NoWoundIncapacitation = "No Wound Incapacitation";

    // A game inch is 2.5 cm.
    private const double CmPerGameInch = 2.5;

    public TrooperProfile With(TrooperChange change)
    {
        var skills = Skills.ToList();
        var vitality = Vitality;
        if (change.WoundsSkillSteps != 0 && int.TryParse(Vitality, NumberStyles.Integer, CultureInfo.InvariantCulture, out var wounds))
        {
            (skills, wounds) = StepWoundsSkill(skills, wounds, change.WoundsSkillSteps);
            vitality = wounds.ToString(CultureInfo.InvariantCulture);
        }

        AddDistinct(skills, change.SkillsGained);
        foreach (var perkSkill in change.PerkSkills)
        {
            if (!ImproveHeld(skills, perkSkill) && change.ListsPerkSkills)
            {
                AddDistinct(skills, [perkSkill]);
            }
        }
        var characteristics = Characteristics
            .Where(characteristic => !change.CharacteristicsLost.Any(lost => SameName(lost, characteristic)))
            .ToList();
        AddDistinct(characteristics, change.CharacteristicsGained.Where(gained => !change.CharacteristicsLost.Any(lost => SameName(lost, gained))));

        return this with
        {
            Move = MoveAfter(change),
            Cc = Add(Cc, change.Cc),
            Bs = Add(Bs, change.Bs),
            Ph = Add(Ph, change.Ph),
            Wip = Add(Wip, change.Wip),
            Arm = Add(Arm, change.Arm),
            Bts = Add(Bts, change.Bts),
            Vitality = vitality,
            Silhouette = change.Silhouette?.ToString(CultureInfo.InvariantCulture) ?? Silhouette,
            Skills = skills,
            Characteristics = characteristics
        };
    }

    /// <summary>
    /// Wounds skill steps go Dogged → NWI → +1 VITA/STR and back down, never below 1 VITA/STR. A level counts
    /// three per wound plus 1 for Dogged or 2 for NWI.
    /// </summary>
    public static (List<string> Skills, int Vitality) StepWoundsSkill(IReadOnlyList<string> skills, int vitality, int steps)
    {
        var hasNwi = skills.Any(skill => IsNwi(skill));
        var hasDogged = skills.Any(skill => SameName(skill, Dogged));
        var level = Math.Max(3, (vitality * 3) + (hasNwi ? 2 : hasDogged ? 1 : 0) + steps);

        var result = skills.Where(skill => !IsNwi(skill) && !SameName(skill, Dogged)).ToList();
        switch (level % 3)
        {
            case 1:
                result.Add(Dogged);
                break;
            case 2:
                result.Add(NoWoundIncapacitation);
                break;
        }

        return (result, level / 3);
    }

    private string MoveAfter(TrooperChange change)
    {
        if (change.Move is null && change.FirstMoveInches == 0 && change.SecondMoveInches == 0)
        {
            return Move;
        }

        double first, second;
        if (change.Move is { } replaced)
        {
            (first, second) = (replaced.First, replaced.Second);
        }
        else if (Movement.TryParse(Move, out var movement))
        {
            (first, second) = (movement.FirstCm / CmPerGameInch, movement.SecondCm / CmPerGameInch);
        }
        else
        {
            return Move;
        }

        first = Math.Max(0, first + change.FirstMoveInches);
        second = Math.Max(0, second + change.SecondMoveInches);
        return $"{Cm(first)}cm-{Cm(second)}cm";
    }

    private static int Cm(double inches) => (int)Math.Round(inches * CmPerGameInch, MidpointRounding.AwayFromZero);

    private static string Add(string value, int delta) =>
        delta != 0 && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            ? (number + delta).ToString(CultureInfo.InvariantCulture)
            : value;

    // A skill with a modifier adds into the same skill held: Mimetism (-3) on Mimetism (-3) makes Mimetism (-6).
    private static bool ImproveHeld(List<string> skills, string skill)
    {
        if (SkillModifier().Match(skill) is not { Success: true } gained)
        {
            return false;
        }

        for (var i = 0; i < skills.Count; i++)
        {
            if (SkillModifier().Match(skills[i]) is { Success: true } held &&
                SameName(held.Groups["name"].Value, gained.Groups["name"].Value))
            {
                var total = int.Parse(held.Groups["value"].Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture) +
                            int.Parse(gained.Groups["value"].Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
                skills[i] = $"{held.Groups["name"].Value.Trim()} ({total.ToString("+0;-0;0", CultureInfo.InvariantCulture)})";
                return true;
            }
        }

        return false;
    }

    private static void AddDistinct(List<string> list, IEnumerable<string> additions)
    {
        foreach (var addition in additions)
        {
            if (!list.Any(existing => SameName(existing, addition)))
            {
                list.Add(addition);
            }
        }
    }

    private static bool IsNwi(string skill) => SameName(skill, NoWoundIncapacitation) || SameName(skill, "NWI");

    private static bool SameName(string a, string b) => string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);

    // "Mimetism (-3)": a name, then a signed number in brackets.
    [GeneratedRegex(@"^\s*(?<name>[^()]+?)\s*\((?<value>[+-]\d+)\)\s*$")]
    private static partial Regex SkillModifier();
}
