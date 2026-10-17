using InfinityMercsApp.Domain.Season.Troopers;

namespace InfinityMercsApp.Domain.Season.RecruitRolls;

/// <summary>
/// Puts recruit table results on a trooper. They come after every other change, armour and its limits included.
/// Dogged and NWI are taken as written: Dogged does nothing for a trooper that has either, and NWI replaces Dogged.
/// A skill with a modifier adds into the same skill held, as perks do.
/// </summary>
public static class RollResults
{
    public static RolledLine Apply(TrooperProfile profile, string? unitTypeCode, IEnumerable<string> results, GainKinds kinds)
    {
        var change = TrooperChange.None;
        var skills = new List<string>();
        var weapons = new List<string>();
        var equipment = new List<string>();
        foreach (var result in results)
        {
            var effects = RollEffects.Of(result, unitTypeCode);
            change += effects.Change;
            foreach (var name in effects.Named)
            {
                (kinds.Of(name) switch
                {
                    GainKind.Weapon => weapons,
                    GainKind.Skill => skills,
                    _ => equipment
                }).Add(name);
            }
        }

        var line = profile.With(change with { PerkSkills = skills.Where(skill => !IsWoundsSkill(skill)).ToList() });
        foreach (var woundsSkill in skills.Where(IsWoundsSkill))
        {
            line = line with { Skills = WithWoundsSkill(line.Skills, woundsSkill) };
        }

        return new RolledLine(line, weapons, equipment);
    }

    private static IReadOnlyList<string> WithWoundsSkill(IReadOnlyList<string> skills, string gained)
    {
        if (IsNwi(gained))
        {
            return [.. skills.Where(skill => !SameName(skill, TrooperProfile.Dogged) && !IsNwi(skill)), TrooperProfile.NoWoundIncapacitation];
        }

        return skills.Any(IsWoundsSkill) ? skills : [.. skills, TrooperProfile.Dogged];
    }

    private static bool IsWoundsSkill(string skill) => SameName(skill, TrooperProfile.Dogged) || IsNwi(skill);

    private static bool IsNwi(string skill) => SameName(skill, TrooperProfile.NoWoundIncapacitation) || SameName(skill, "NWI");

    private static bool SameName(string a, string b) => string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
}
