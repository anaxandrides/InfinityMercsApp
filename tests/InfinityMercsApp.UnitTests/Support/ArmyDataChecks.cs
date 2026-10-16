using InfinityMercsApp.Domain.Models.Army;
using MetadataFaction = InfinityMercsApp.Domain.Models.Metadata.Faction;
using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Services.Import;

namespace InfinityMercsApp.UnitTests.Support;

/// <summary>Army data checks for the screens that act on them, with PanOceania as the downloaded army.</summary>
internal static class ArmyDataChecks
{
    public static ArmyDataCheck Failed() => ArmyDataCheck.Failed("offline");

    public static ArmyDataCheck UpToDate() =>
        ArmyDataCheck.Downloaded(Metadata(), metadataChanged: false, [Army("7.1", storedVersion: "7.1", isUpdate: false)], []);

    public static ArmyDataCheck WithUpdate() =>
        ArmyDataCheck.Downloaded(Metadata(), metadataChanged: false, [Army("7.2", storedVersion: "7.1", isUpdate: true)], []);

    public static ArmyDataCheck WithUnavailable(params IEnumerable<string> factions) =>
        ArmyDataCheck.Downloaded(Metadata(), metadataChanged: false, [Army("7.1", storedVersion: "7.1", isUpdate: false)], factions.ToList());

    public static DownloadedArmy Army(string version, string? storedVersion, bool isUpdate, int factionId = 101, string name = "PanOceania") =>
        new(factionId, name, new ArmyDefinition { Version = version }, storedVersion, isUpdate);

    public static MetadataDocument Metadata() => new()
    {
        Fingerprint = "metadata-v2",
        Factions = [new MetadataFaction { Id = 101, ParentId = 101, Name = "PanOceania" }]
    };
}
