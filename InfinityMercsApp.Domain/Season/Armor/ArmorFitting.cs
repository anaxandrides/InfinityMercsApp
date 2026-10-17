using System.Globalization;
using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Domain.Season.Troopers;

namespace InfinityMercsApp.Domain.Season.Armor;

/// <summary>
/// Puts armour on a trooper, by the Mercenaries armour rules. The troop type changes first: becoming HI gives two
/// wounds skill steps, Hackable and Immunity (Shock), and leaving HI takes them away. A trooper has one type, so
/// a REM stays a REM. The armour's ARM and BTS then replace the underlying ones, and when the type changed, ARM is
/// capped at 2 for LI and 3 for MI. The captain's bought bonuses stay on top of whatever the armour sets.
/// TAGs wear no armour.
/// </summary>
public static partial class ArmorFitting
{
    public const string Hackable = "Hackable";
    public const string ShockImmunity = "Immunity (Shock)";

    private const int HeavyInfantryWoundsSkills = 2;
    private const int LightInfantryArmCap = 2;
    private const int MediumInfantryArmCap = 3;

    /// <param name="profile">The trooper's saved line; a lieutenant's holds the captain's bonuses.</param>
    /// <param name="captainBonuses">The bonuses in a lieutenant's line, kept on top of any value the armour replaces.</param>
    public static WornArmor Wear(TrooperProfile profile, string? unitTypeCode, StoreTroopType? armor, CaptainStatBonuses captainBonuses)
    {
        var type = unitTypeCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (armor is null || UnitTypeCodes.IsTag(type))
        {
            return WornArmor.None(profile, type);
        }

        var effects = armor.Effects;
        var newType = string.IsNullOrWhiteSpace(armor.Type) || UnitTypeCodes.IsRemote(type) ? type : armor.Type.Trim().ToUpperInvariant();
        var typeChanged = !string.Equals(type, newType, StringComparison.Ordinal);

        var skills = profile.Skills.ToList();
        var characteristics = profile.Characteristics.ToList();
        var woundsSteps = effects.WoundsSkill;
        if (!UnitTypeCodes.IsHeavyInfantry(type) && UnitTypeCodes.IsHeavyInfantry(newType))
        {
            woundsSteps += HeavyInfantryWoundsSkills;
            AddDistinct(characteristics, Hackable);
            AddDistinct(skills, ShockImmunity);
        }
        else if (UnitTypeCodes.IsHeavyInfantry(type) && !UnitTypeCodes.IsHeavyInfantry(newType))
        {
            woundsSteps -= HeavyInfantryWoundsSkills;
            characteristics.RemoveAll(characteristic => SameName(characteristic, Hackable));
            skills.RemoveAll(skill => SameName(skill, ShockImmunity));
        }

        if (UnitTypeCodes.IsRemote(newType) && !UnitTypeCodes.IsRemote(type))
        {
            AddDistinct(characteristics, Hackable);
        }

        var arm = armor.Arm is { } armorArm ? armorArm : Number(profile.Arm) - captainBonuses.Arm;
        if (typeChanged && CapFor(newType) is { } cap)
        {
            arm = Math.Min(arm, cap);
        }

        var vitality = profile.Vitality;
        if (effects.Vitality is { } replacedVitality)
        {
            // Superheavy Armor's wounds replace any wounds skill, the one becoming HI gives included.
            skills.RemoveAll(skill => IsWoundsSkill(skill));
            vitality = Text(replacedVitality + captainBonuses.Vitality);
        }
        else if (woundsSteps != 0 && int.TryParse(profile.Vitality, NumberStyles.Integer, CultureInfo.InvariantCulture, out var wounds))
        {
            (skills, wounds) = TrooperProfile.StepWoundsSkill(skills, wounds, woundsSteps);
            vitality = Text(wounds);
        }

        foreach (var skill in effects.Skills)
        {
            GainSkill(skills, skill);
        }

        if (effects.MartialArtsLevels > 0)
        {
            RaiseMartialArts(skills, effects.MartialArtsLevels);
        }

        foreach (var characteristic in effects.Characteristics)
        {
            AddDistinct(characteristics, characteristic);
        }

        characteristics.RemoveAll(characteristic => effects.CharacteristicsLost.Any(lost => SameName(lost, characteristic)));

        var worn = profile with
        {
            Arm = IsNumber(profile.Arm) || armor.Arm is not null ? Text(arm + captainBonuses.Arm) : profile.Arm,
            Bts = armor.Bts is { } bts ? Text(bts + captainBonuses.Bts) : profile.Bts,
            Ph = effects.Ph is { } ph ? Text(ph + captainBonuses.Ph) : profile.Ph,
            Vitality = vitality,
            Skills = skills,
            Characteristics = characteristics
        };
        var moved = worn.With(new TrooperChange
        {
            Move = MoveOf(effects.Move),
            Silhouette = effects.Silhouette
        });

        return new WornArmor(moved, newType, effects.Equipment, effects.MaxBts);
    }

    private static int? CapFor(string type) => type switch
    {
        UnitTypeCodes.LightInfantry => LightInfantryArmCap,
        UnitTypeCodes.MediumInfantry => MediumInfantryArmCap,
        _ => null
    };

    // Dogged and NWI are taken as written: Dogged does nothing for a trooper that has either, and NWI replaces Dogged.
    private static void GainSkill(List<string> skills, ArmorSkill skill)
    {
        if (SameName(skill.Name, TrooperProfile.Dogged))
        {
            if (!skills.Any(IsWoundsSkill))
            {
                skills.Add(TrooperProfile.Dogged);
            }

            return;
        }

        if (IsNwi(skill.Name))
        {
            skills.RemoveAll(held => SameName(held, TrooperProfile.Dogged));
            AddDistinct(skills, TrooperProfile.NoWoundIncapacitation);
            return;
        }

        var held = skills.FindIndex(existing => SameName(BaseName(existing), BaseName(skill.Name)));
        if (held >= 0 && skill.WhenHeld is { } whenHeld)
        {
            skills[held] = whenHeld;
        }
        else
        {
            AddDistinct(skills, skill.Name);
        }
    }

    private static void RaiseMartialArts(List<string> skills, int levels)
    {
        var held = skills.FindIndex(skill => MartialArtsLevel().IsMatch(skill));
        if (held < 0)
        {
            skills.Add($"Martial Arts L{levels}");
            return;
        }

        var level = int.Parse(MartialArtsLevel().Match(skills[held]).Groups["level"].Value, CultureInfo.InvariantCulture);
        skills[held] = $"Martial Arts L{level + levels}";
    }

    private static MoveInches? MoveOf(string? move)
    {
        IReadOnlyList<string> parts = move?.Split('-', StringSplitOptions.TrimEntries) ?? [];
        return parts.Count == 2 &&
               int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var first) &&
               int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var second)
            ? new MoveInches(first, second)
            : null;
    }

    private static void AddDistinct(List<string> list, string addition)
    {
        if (!list.Any(existing => SameName(existing, addition)))
        {
            list.Add(addition);
        }
    }

    private static bool IsWoundsSkill(string skill) => SameName(skill, TrooperProfile.Dogged) || IsNwi(skill);

    private static bool IsNwi(string skill) => SameName(skill, TrooperProfile.NoWoundIncapacitation) || SameName(skill, "NWI");

    // "Combat Jump (-3)" and "Combat Jump" are the same skill.
    private static string BaseName(string skill)
    {
        var bracket = skill.IndexOf('(', StringComparison.Ordinal);
        return bracket < 0 ? skill : skill[..bracket];
    }

    private static bool IsNumber(string value) => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);

    private static int Number(string value) => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? number : 0;

    private static string Text(int value) => value.ToString(CultureInfo.InvariantCulture);

    // The army data writes some names with non-breaking spaces ("Hidden Deployment").
    private static bool SameName(string a, string b) =>
        string.Equals(a.Replace(' ', ' ').Trim(), b.Replace(' ', ' ').Trim(), StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(@"^\s*Martial\s+Arts\s+L(?<level>\d+)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex MartialArtsLevel();
}
