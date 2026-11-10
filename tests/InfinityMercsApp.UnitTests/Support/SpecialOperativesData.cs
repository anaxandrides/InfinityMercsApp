using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Domain.Models.Army.SpecOps;
using InfinityMercsApp.Services.ArmyData;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Support;

/// <summary>
/// A Spec-Ops unit (with its SpecBot) and a Team-Ops unit shaped like Corvus Belli's October 2026 army data, with the
/// names they need added to an army's filters. Their ids sit well clear of the reference army's.
/// </summary>
internal static class SpecialOperativesData
{
    public const int SpecOpsId = 1912;
    public const int TeamOpsId = 1926;
    public const string SpecOpsName = "INDIGO SPEC-OPS";
    public const string TeamOpsName = "INDIGO TEAM-OPS";

    public const int SpecOpsSkill = 9281;
    public const int TeamOpsSkill = 9282;
    public const int Dodge = 9040;
    public const int Dogged = 9085;
    public const int CombiRifle = 9001;
    public const int FlashPulse = 9002;
    public const int Spitfire = 9003;
    public const int DeployableRepeater = 9183;
    public const int Peripheral = 9021;

    /// <summary><paramref name="filters"/> with the skills, weapons, equipment and characteristic these units use.</summary>
    public static ArmyFilterCatalog WithNames(ArmyFilterCatalog filters)
    {
        var added = new Dictionary<string, IReadOnlyList<ArmyFilterItem>>
        {
            [ArmyFilterSection.Skills] =
            [
                new(SpecOpsSkill, "Infinity Spec-Ops", null, null),
                new(TeamOpsSkill, "Infinity Team-Ops", null, null),
                new(Dodge, "Dodge", null, null),
                new(Dogged, "Dogged", null, null)
            ],
            [ArmyFilterSection.Weapons] =
            [
                new(CombiRifle, "Combi Rifle", null, null),
                new(FlashPulse, "Flash Pulse", null, null),
                new(Spitfire, "Spitfire", null, null)
            ],
            [ArmyFilterSection.Equip] = [new(DeployableRepeater, "Deployable Repeater", null, null)],
            [ArmyFilterSection.Chars] = [new(Peripheral, "Peripheral", null, null)]
        };
        var sections = filters.Sections.Concat(added.Keys).Distinct().ToDictionary(
            section => section,
            section => (IReadOnlyList<ArmyFilterItem>)[.. filters.Items(section), .. added.GetValueOrDefault(section) ?? []]);
        return new ArmyFilterCatalog(sections);
    }

    /// <summary>Gives <paramref name="factionId"/> both units, listed apart from its recruits.</summary>
    public static void AddTo(IArmyDataService armyData, int factionId)
    {
        armyData.GetUpgradeUnitResumes(factionId, Arg.Any<CancellationToken>()).Returns(
        [
            new Resume { FactionId = factionId, UnitId = SpecOpsId, Name = SpecOpsName, Slug = "indigo-spec-ops", Type = 2, Category = 4, HasUpgrades = true },
            new Resume { FactionId = factionId, UnitId = TeamOpsId, Name = TeamOpsName, Slug = "indigo-team-ops", Type = 2, Category = 4, HasUpgrades = true }
        ]);
        armyData.GetUnit(factionId, SpecOpsId, Arg.Any<CancellationToken>()).Returns(_ => SpecOps(factionId));
        armyData.GetUnit(factionId, TeamOpsId, Arg.Any<CancellationToken>()).Returns(_ => TeamOps(factionId));
    }

    /// <summary>The Spec-Ops: its Initial and Enhanced profiles with two loadouts (one costing SWC), then its SpecBot.</summary>
    public static Unit SpecOps(int factionId) => new()
    {
        FactionId = factionId,
        UnitId = SpecOpsId,
        Name = SpecOpsName,
        ProfileGroups = new UnitProfileGroups(
        [
            new ProfileGroup
            {
                Id = 1,
                Isc = SpecOpsName,
                Profiles =
                [
                    Trooper("INITIAL PROFILE", type: 2, ava: 1, SpecOpsSkill),
                    Trooper("ENHANCED PROFILE", type: 3, ava: -1, SpecOpsSkill)
                ],
                Options = [Loadout(1, "INDIGO", 20, "0", CombiRifle), Loadout(2, "INDIGO", 25, "1", CombiRifle)]
            },
            new ProfileGroup
            {
                Id = 2,
                Isc = "BLUEBERRY SPECBOTS",
                Profiles = [SpecBot(ava: -1)],
                Options = [Loadout(1, "BLUEBERRY SPECBOT_1", 6, "-", FlashPulse)]
            }
        ]),
        Upgrades = new SpecUpgradeTable(
        [
            new([SpecUpgradePart.ForStat("bts", 3)]),
            new([SpecUpgradePart.ForStat("arm", 1), SpecUpgradePart.ForItem(SpecUpgradePartKind.Skill, Dogged)]),
            new([SpecUpgradePart.ForItem(SpecUpgradePartKind.Weapon, Spitfire)]),
            new([SpecUpgradePart.ForItem(SpecUpgradePartKind.Equipment, DeployableRepeater)])
        ])
    };

    /// <summary>The Team-Ops entry: its MI, its HI and its SpecBot, each a group of its own.</summary>
    public static Unit TeamOps(int factionId) => new()
    {
        FactionId = factionId,
        UnitId = TeamOpsId,
        Name = TeamOpsName,
        ProfileGroups = new UnitProfileGroups(
        [
            new ProfileGroup
            {
                Id = 1,
                Isc = "Indigo-1 Spec-Ops",
                Profiles = [Trooper("INDIGO-1 SPEC-OPS", type: 2, ava: 1, TeamOpsSkill)],
                Options = [Loadout(1, "INDIGO-1", 18, "0", CombiRifle)]
            },
            new ProfileGroup
            {
                Id = 2,
                Isc = "Indigo Heavy Spec-Ops",
                Profiles = [Trooper("INDIGO HEAVY SPEC-OPS", type: 3, ava: 1, TeamOpsSkill)],
                Options = [Loadout(1, "INDIGO HEAVY", 34, "0", Spitfire)]
            },
            new ProfileGroup
            {
                Id = 3,
                Isc = "Blueberry SpecBots",
                Profiles = [SpecBot(ava: 1, TeamOpsSkill)],
                Options = [Loadout(1, "BLUEBERRY SPECBOT_1", 6, "0", FlashPulse)]
            }
        ]),
        Upgrades = new SpecUpgradeTable(
        [
            new([SpecUpgradePart.ForStat("move0", 15), SpecUpgradePart.ForStat("move1", 10), SpecUpgradePart.ForItem(SpecUpgradePartKind.Skill, Dodge)]),
            new([SpecUpgradePart.ForStat("wip", 1)])
        ])
    };

    private static UnitProfile Trooper(string name, int type, int ava, params IEnumerable<int> skills) => new()
    {
        Id = 1,
        Name = name,
        Type = type,
        Move = "10-10",
        MoveCm = [10, 10],
        Cc = 13,
        Bs = 13,
        Ph = 11,
        Wip = 12,
        Arm = 2,
        Bts = 3,
        W = 1,
        S = 2,
        Ava = ava,
        HasStatFields = true,
        Skills = skills.Select(ProfileEntry.ForId).ToList()
    };

    private static UnitProfile SpecBot(int ava, params IEnumerable<int> skills) => new()
    {
        Id = 1,
        Name = "BLUEBERRY SPECBOT",
        Type = 5,
        Str = true,
        Move = "15-10",
        MoveCm = [15, 10],
        Cc = 13,
        Bs = 10,
        Ph = 10,
        Wip = 11,
        Arm = 0,
        Bts = 3,
        W = 1,
        S = 3,
        Ava = ava,
        HasStatFields = true,
        Chars = [ProfileEntry.ForId(Peripheral)],
        Skills = skills.Select(ProfileEntry.ForId).ToList()
    };

    private static ProfileOption Loadout(int id, string name, int cost, string swc, params IEnumerable<int> weapons) => new()
    {
        Id = id,
        Name = name,
        Cost = cost.ToString(System.Globalization.CultureInfo.InvariantCulture),
        Swc = swc,
        Minis = 1,
        Orders = [new OrderEntry("REGULAR", 1, 1)],
        Weapons = weapons.Select(ProfileEntry.ForId).ToList()
    };
}
