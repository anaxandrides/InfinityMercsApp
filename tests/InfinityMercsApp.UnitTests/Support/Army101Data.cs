using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Infrastructure.Json;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.UnitTests.Characterization;
using NSubstitute;
using MetadataFaction = InfinityMercsApp.Domain.Models.Metadata.Faction;

namespace InfinityMercsApp.UnitTests.Support;

/// <summary>
/// An army data service backed by the checked-in PanOceania army (faction 101), plus a sectorial (102)
/// in the faction list. MOV shows as "first-second" in cm.
/// </summary>
internal static class Army101Data
{
    public const int PanOceania = 101;

    private static readonly Lazy<ArmyDefinition> Army = new(() =>
        new ArmyJsonReader().ReadArmyPayload(File.ReadAllText(RepoFiles.Resolve(ReferenceCorpus.Army101Path)))!);

    public static ArmyDefinition Definition => Army.Value;

    public static ArmyUnitDefinition UnitNamed(string name) => Definition.Units.First(u => u.Name == name);

    public static IArmyDataService Create()
    {
        var army = Definition;
        var data = Substitute.For<IArmyDataService>();

        data.GetMetadataFactions(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(
        [
            new MetadataFaction { Id = PanOceania, ParentId = PanOceania, Name = "PanOceania" },
            new MetadataFaction { Id = 102, ParentId = PanOceania, Name = "Military Orders" }
        ]);
        data.GetFactionSnapshot(PanOceania, Arg.Any<CancellationToken>()).Returns(
            new Faction { FactionId = PanOceania, Filters = army.Filters, FireteamChart = army.FireteamChart });
        data.GetResumeByFaction(PanOceania, Arg.Any<CancellationToken>()).Returns(army.Resume
            .Select(r => new Resume { FactionId = PanOceania, UnitId = r.Id, Name = r.Name, Slug = r.Slug, Type = r.Type, Category = r.Category })
            .ToList());
        data.GetUnit(PanOceania, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(call =>
            army.Units.FirstOrDefault(u => u.Id == call.ArgAt<int>(1)) is { } unit
                ? new Unit { FactionId = PanOceania, UnitId = unit.Id, Name = unit.Name, ProfileGroups = unit.ProfileGroups }
                : null);
        data.FormatMovement(Arg.Any<Movement?>()).Returns(call => Movement.Format(call.Arg<Movement?>(), inInches: false));
        data.FormatMoveValue(Arg.Any<int?>(), Arg.Any<int?>()).Returns(call =>
            Movement.Format(Movement.From(call.ArgAt<int?>(0), call.ArgAt<int?>(1)), inInches: false));
        return data;
    }
}
