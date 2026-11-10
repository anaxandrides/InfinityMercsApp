using System.Globalization;
using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.SpecOps;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.SpecOps;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanyProfiles;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;

namespace InfinityMercsApp.Services.CompanySelection.SpecOps;

/// <summary>A Spec-Ops or Team-Ops unit's upgrades, named from its army's data.</summary>
public sealed class SpecUpgradeCatalog(IArmyDataService armyData)
{
    /// <summary>The unit's upgrades in table order; none for a unit without a table.</summary>
    public IReadOnlyList<SpecUpgradeEffect> For(int factionId, int unitId, bool showUnitsInInches, CancellationToken cancellationToken = default)
    {
        var unit = armyData.GetUnit(factionId, unitId, cancellationToken);
        if (unit is null || unit.Upgrades.IsEmpty)
        {
            return [];
        }

        var filters = armyData.GetFactionSnapshot(factionId, cancellationToken)?.Filters ?? ArmyFilterCatalog.Empty;
        return Describe(unit.Upgrades, filters, showUnitsInInches);
    }

    /// <summary>Which kind of unit it is, by the skill its profiles carry.</summary>
    public SpecialOperativeKind KindOf(int factionId, int unitId, CancellationToken cancellationToken = default)
    {
        var unit = armyData.GetUnit(factionId, unitId, cancellationToken);
        if (unit is null || unit.Upgrades.IsEmpty)
        {
            return SpecialOperativeKind.None;
        }

        var filters = armyData.GetFactionSnapshot(factionId, cancellationToken)?.Filters ?? ArmyFilterCatalog.Empty;
        return SpecialOperatives.KindOf(unit.ProfileGroups, filters.Names(ArmyFilterSection.Skills));
    }

    /// <summary>The faction's units of <paramref name="kind"/>: its Spec-Ops, or its Team-Ops entry.</summary>
    public IReadOnlyList<ArmyResumeRecord> ResumesOf(int factionId, SpecialOperativeKind kind, CancellationToken cancellationToken = default) =>
        armyData.GetUpgradeUnitResumes(factionId, cancellationToken)
            .Where(resume => KindOf(factionId, resume.UnitId, cancellationToken) == kind)
            .ToList();

    /// <summary>
    /// The SpecBots a Spec-Ops can bring: each option without SWC of the unit's groups after the first (the Spec-Ops
    /// itself), with the option's weapons, skills and equipment on the SpecBot's profile.
    /// </summary>
    public IReadOnlyList<SpecBotOption> SpecBotsFor(int factionId, int unitId, bool showUnitsInInches, CancellationToken cancellationToken = default)
    {
        var unit = armyData.GetUnit(factionId, unitId, cancellationToken);
        if (unit is null)
        {
            return [];
        }

        var filters = armyData.GetFactionSnapshot(factionId, cancellationToken)?.Filters ?? ArmyFilterCatalog.Empty;
        var options = new List<SpecBotOption>();
        foreach (var group in unit.ProfileGroups.Groups.Skip(1))
        {
            if (group.Profiles.FirstOrDefault() is not { } profile)
            {
                continue;
            }

            var name = string.IsNullOrWhiteSpace(profile.Name) ? group.Isc ?? "SpecBot" : profile.Name.Trim();
            foreach (var option in group.Options.Where(option => !option.CostsSwc))
            {
                var equipped = profile.WithAva(profile.Ava);
                equipped.Weapons = [.. profile.Weapons, .. option.Weapons];
                equipped.Skills = [.. profile.Skills, .. option.Skills];
                equipped.Equip = [.. profile.Equip, .. option.Equip];
                if (PeripheralMercsCompanyStats.From(name, equipped, filters, showUnitsInInches) is not { } stats)
                {
                    continue;
                }

                var loadout = CompanyProfileText.SplitDisplayLine(stats.RangedWeapons)
                    .Concat(CompanyProfileText.SplitDisplayLine(stats.CcWeapons))
                    .ToList();
                var label = $"{name} ({option.CostValue} pts)" + (loadout.Count == 0 ? string.Empty : $": {string.Join(", ", loadout)}");
                options.Add(new SpecBotOption(label, option.CostValue, stats));
            }
        }

        return options.DistinctBy(option => option.Label, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Each upgrade of <paramref name="table"/> with its parts named from <paramref name="filters"/>.</summary>
    public static IReadOnlyList<SpecUpgradeEffect> Describe(SpecUpgradeTable table, ArmyFilterCatalog filters, bool showUnitsInInches)
    {
        var names = new ProfileEntryNames(filters.Extras, showUnitsInInches);
        var weapons = filters.Names(ArmyFilterSection.Weapons);
        var skills = filters.Names(ArmyFilterSection.Skills);
        var equipment = filters.Names(ArmyFilterSection.Equip);
        return table.Upgrades.Select(upgrade => Describe(upgrade, names, weapons, skills, equipment, showUnitsInInches)).ToList();
    }

    private static SpecUpgradeEffect Describe(
        SpecUpgrade upgrade,
        ProfileEntryNames names,
        IReadOnlyDictionary<int, string> weaponNames,
        IReadOnlyDictionary<int, string> skillNames,
        IReadOnlyDictionary<int, string> equipmentNames,
        bool showUnitsInInches)
    {
        var bonuses = CaptainStatBonuses.None;
        int? moveFirst = null;
        int? moveSecond = null;
        var weapons = new List<string>();
        var skills = new List<string>();
        var equipment = new List<string>();
        var labels = new List<string>();

        string? Named(SpecUpgradePart part, IReadOnlyDictionary<int, string> lookup) =>
            part.Id is int id ? names.DisplayName(new ProfileEntry(id, 1, null, part.Extras), lookup) : null;

        foreach (var part in upgrade.Parts)
        {
            switch (part.Kind)
            {
                case SpecUpgradePartKind.Stat when MoveIndex(part.StatKey) is int index:
                    if (index == 0)
                    {
                        moveFirst = part.Quantity;
                    }
                    else
                    {
                        moveSecond = part.Quantity;
                    }

                    break;
                case SpecUpgradePartKind.Stat when BonusStat(part.StatKey) is Stat stat:
                    bonuses = WithBonus(bonuses, stat, part.Quantity);
                    labels.Add($"{part.Quantity.ToString("+0;-0", CultureInfo.InvariantCulture)} {stat.Key()}");
                    break;
                case SpecUpgradePartKind.Weapon when Named(part, weaponNames) is { } weapon:
                    weapons.Add(weapon);
                    labels.Add(weapon);
                    break;
                case SpecUpgradePartKind.Skill when Named(part, skillNames) is { } skill:
                    skills.Add(skill);
                    labels.Add(skill);
                    break;
                case SpecUpgradePartKind.Equipment when Named(part, equipmentNames) is { } item:
                    equipment.Add(item);
                    labels.Add(item);
                    break;
            }
        }

        if (moveFirst is not null || moveSecond is not null)
        {
            labels.Insert(0, $"MOV {Movement.Format(Movement.From(moveFirst, moveSecond), showUnitsInInches)}");
        }

        return new SpecUpgradeEffect(string.Join(", ", labels), bonuses, moveFirst, moveSecond, weapons, skills, equipment);
    }

    // "move0" and "move1" set the first and second MOV values.
    private static int? MoveIndex(string? statKey) => statKey switch
    {
        "move0" => 0,
        "move1" => 1,
        _ => null
    };

    private static Stat? BonusStat(string? statKey) => statKey switch
    {
        "cc" => Stat.Cc,
        "bs" => Stat.Bs,
        "ph" => Stat.Ph,
        "wip" => Stat.Wip,
        "arm" => Stat.Arm,
        "bts" => Stat.Bts,
        "w" or "vita" or "str" => Stat.Vita,
        _ => null
    };

    private static CaptainStatBonuses WithBonus(CaptainStatBonuses bonuses, Stat stat, int quantity) => stat switch
    {
        Stat.Cc => bonuses with { Cc = bonuses.Cc + quantity },
        Stat.Bs => bonuses with { Bs = bonuses.Bs + quantity },
        Stat.Ph => bonuses with { Ph = bonuses.Ph + quantity },
        Stat.Wip => bonuses with { Wip = bonuses.Wip + quantity },
        Stat.Arm => bonuses with { Arm = bonuses.Arm + quantity },
        Stat.Bts => bonuses with { Bts = bonuses.Bts + quantity },
        _ => bonuses with { Vitality = bonuses.Vitality + quantity }
    };
}
