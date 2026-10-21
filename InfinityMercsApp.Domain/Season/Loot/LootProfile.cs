using System.Globalization;
using InfinityMercsApp.Domain.Models.Metadata;

namespace InfinityMercsApp.Domain.Season.Loot;

/// <summary>
/// A loot item's weapon profile: the sheet's row with its buffs applied in the order they were rolled. Ammunition
/// buffs replace N or join what is there (Shock+T2); the saving roll starts as ARM.
/// </summary>
internal static class LootProfile
{
    private const string Dash = "-";
    private const string Normal = "N";
    private const string Arm = "ARM";
    private const string Bts = "BTS";

    public static IReadOnlyList<LootWeaponMode> Build(LootItem item)
    {
        var row = item.Profile;
        var ammo = new List<string> { BaseAmmo(item.Weapon) };
        foreach (var buffAmmo in item.Buffs.Select(buff => buff.Ammo).OfType<string>())
        {
            AddAmmo(ammo, buffAmmo);
        }

        var burstBonus = item.Buffs.Sum(buff => buff.BurstBonus);
        var traits = item.Weapon.Traits
            .Concat(item.TurretWeapon?.Traits ?? [])
            .Concat(item.Buffs.SelectMany(buff => buff.Traits))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var saving = Saving(item.Buffs);
        var savingNum = SavingNum(item.Buffs);
        var damage = Damage(row.Ps, item.Buffs);
        var distance = row.Distance();

        Weapon Mode(string? mode, string burst, IReadOnlyList<string> modeTraits) => new()
        {
            Name = item.Name,
            Mode = mode,
            Burst = Burst(burst, burstBonus),
            Damage = damage,
            Saving = saving,
            SavingNum = savingNum.ToString(CultureInfo.InvariantCulture),
            Properties = modeTraits,
            Distance = distance
        };

        if (item.Buffs.All(buff => !buff.IsMulti))
        {
            return [new LootWeaponMode(Mode(null, row.Burst, traits), Joined(ammo))];
        }

        // Multi: the weapon's burst with AP or Shock, and B1 with DA and Anti-material.
        List<string> antiMaterialTraits = [.. traits, LootBuffChart.AntiMaterial];
        return
        [
            new LootWeaponMode(Mode("AP Mode", row.Burst, traits), Joined(WithAmmo(ammo, "AP"))),
            new LootWeaponMode(Mode("Shock Mode", row.Burst, traits), Joined(WithAmmo(ammo, "Shock"))),
            new LootWeaponMode(Mode("Anti-Material Mode", "1", antiMaterialTraits.Distinct(StringComparer.OrdinalIgnoreCase).ToList()), Joined(WithAmmo(ammo, "DA")))
        ];
    }

    // An Upgrade row names its ammunition among its traits ("DA Ammo"); a weapon fires N.
    private static string BaseAmmo(LootWeaponRow row) =>
        row.Traits.FirstOrDefault(trait => trait.EndsWith(" Ammo", StringComparison.OrdinalIgnoreCase)) is { } named
            ? named[..^" Ammo".Length].Trim()
            : Normal;

    private static void AddAmmo(List<string> ammo, string added)
    {
        if (ammo is [Normal])
        {
            ammo.Clear();
        }

        if (!ammo.Contains(added, StringComparer.OrdinalIgnoreCase))
        {
            ammo.Add(added);
        }
    }

    private static List<string> WithAmmo(IReadOnlyList<string> ammo, string added)
    {
        var copy = ammo.ToList();
        AddAmmo(copy, added);
        return copy;
    }

    private static string Joined(IReadOnlyList<string> ammo) => string.Join("+", ammo);

    private static string Burst(string burst, int bonus) =>
        int.TryParse(burst, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? (value + bonus).ToString(CultureInfo.InvariantCulture)
            : Dash;

    // High Caliber lowers PS by 1 and K1 sets it to 7, in the order rolled.
    private static string Damage(string ps, IReadOnlyList<LootBuff> buffs)
    {
        if (!int.TryParse(ps, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return Dash;
        }

        foreach (var buff in buffs)
        {
            value = buff.PsSet ?? value + buff.PsChange;
        }

        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string Saving(IReadOnlyList<LootBuff> buffs)
    {
        var attribute = Arm;
        var halved = false;
        var zeroed = false;
        List<string> replaced = [];
        foreach (var buff in buffs)
        {
            switch (buff.Saving)
            {
                case LootSaving.Opposite:
                    attribute = attribute == Arm ? Bts : attribute == Bts ? Arm : attribute;
                    break;
                case LootSaving.Halved:
                    halved = true;
                    break;
                case LootSaving.ArmAndBts:
                    attribute = $"{Arm}+{Bts}";
                    break;
                case LootSaving.Zeroed:
                    zeroed = true;
                    break;
                case LootSaving.PhMinusSix:
                    replaced.Add("PH-6");
                    break;
                case LootSaving.BtsHalved:
                    replaced.Add($"{Bts}/2");
                    break;
            }
        }

        if (replaced.Count > 0)
        {
            return string.Join(" + ", replaced);
        }

        return zeroed ? $"{attribute}=0" : halved ? $"{attribute}/2" : attribute;
    }

    // Paralyzing with Electromagnetic forces 3 saves, as the sheet says.
    private static int SavingNum(IReadOnlyList<LootBuff> buffs)
    {
        var paralyzing = buffs.Any(buff => buff.Saving == LootSaving.PhMinusSix);
        var electromagnetic = buffs.Any(buff => buff.Saving == LootSaving.BtsHalved);
        return paralyzing && electromagnetic ? 3 : buffs.Select(buff => buff.SavingNum).Prepend(1).Max();
    }
}
