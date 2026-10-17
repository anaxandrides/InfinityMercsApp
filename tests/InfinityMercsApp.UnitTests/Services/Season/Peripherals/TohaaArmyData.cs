using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.Season.Peripherals;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Services.Season.Peripherals;

/// <summary>
/// A small Tohaa army: the Gorgos Squad with two Synchronized Chaksa Peripheral options, Chaksa Servants, and a
/// mercenary with a Synchronized peripheral of its own.
/// </summary>
internal static class TohaaArmyData
{
    public const int GorgosId = 1559;
    public const int PeripheralSkillId = 243;
    public const int CombiRifleId = 10;
    public const int SynchronizedId = 500;

    public static IArmyDataService Create() => AddTo(Substitute.For<IArmyDataService>());

    /// <summary>Gives <paramref name="data"/> the Tohaa army as well.</summary>
    public static IArmyDataService AddTo(IArmyDataService data)
    {
        var filters = new ArmyFilterCatalog(new Dictionary<string, IReadOnlyList<ArmyFilterItem>>
        {
            [ArmyFilterSection.Skills] = [new ArmyFilterItem(PeripheralSkillId, "Peripheral", null, null)],
            [ArmyFilterSection.Extras] = [new ArmyFilterItem(SynchronizedId, "Synchronized", null, null), new ArmyFilterItem(501, "Servant", null, null)],
            [ArmyFilterSection.Weapons] = [new ArmyFilterItem(CombiRifleId, "Combi Rifle", null, null)],
            [ArmyFilterSection.Type] = [new ArmyFilterItem(1, "LI", null, null)]
        });
        data.GetFactionSnapshot(TohaaPeripherals.TohaaFactionId, Arg.Any<CancellationToken>())
            .Returns(new Faction { FactionId = TohaaPeripherals.TohaaFactionId, Filters = filters });
        data.GetResumeByFaction(TohaaPeripherals.TohaaFactionId, Arg.Any<CancellationToken>()).Returns(
        [
            new Resume { UnitId = GorgosId, Name = "Gorgos Squad" },
            new Resume { UnitId = 656, Name = "Chaksa Servants" },
            new Resume { UnitId = 11564, Name = "Fiddler" }
        ]);
        foreach (var unit in new List<Unit>
                 {
                     Unit(GorgosId, "Gorgos Squad", SynchronizedId, [Option(1, "CHAKSA PERIPHERAL A", "7"), Option(2, "CHAKSA PERIPHERAL B", "4")]),
                     Unit(656, "Chaksa Servants", 501, [Option(1, "CHAKSA SERVANT", "3")]),
                     Unit(11564, "Fiddler", SynchronizedId, [Option(1, "BIT", "5")])
                 })
        {
            data.GetUnit(TohaaPeripherals.TohaaFactionId, unit.UnitId, Arg.Any<CancellationToken>()).Returns(unit);
        }

        return data;
    }

    private static ProfileOption Option(int id, string name, string cost) =>
        new() { Id = id, Name = name, Cost = cost, Weapons = [ProfileEntry.ForId(CombiRifleId)] };

    // The controller's group, then the peripheral's.
    private static Unit Unit(int unitId, string name, int peripheralKind, IReadOnlyList<ProfileOption> peripheralOptions) => new()
    {
        FactionId = TohaaPeripherals.TohaaFactionId,
        UnitId = unitId,
        Name = name,
        ProfileGroups = new UnitProfileGroups(
        [
            new ProfileGroup { Id = 1, Profiles = [new UnitProfile { Name = name }], Options = [new ProfileOption { Id = 1, Name = name, Cost = "60" }] },
            new ProfileGroup
            {
                Id = 2,
                Profiles =
                [
                    new UnitProfile
                    {
                        Name = "CHAKSA PERIPHERAL",
                        Type = 1,
                        Move = "15-10",
                        MoveCm = [15, 10],
                        Cc = 17,
                        Bs = 11,
                        Ph = 12,
                        Wip = 11,
                        Arm = 0,
                        Bts = 0,
                        W = 1,
                        S = 2,
                        Skills = [new ProfileEntry(PeripheralSkillId, 1, null, [peripheralKind])]
                    }
                ],
                Options = peripheralOptions
            }
        ])
    };
}
