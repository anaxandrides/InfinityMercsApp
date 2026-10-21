using InfinityMercsApp.Services.Import;
using InfinityMercsApp.UnitTests.Support;

namespace InfinityMercsApp.UnitTests.Services.Import;

/// <summary>What a check tells the user: which data changed, and from which version to which.</summary>
public sealed class ArmyDataCheckTests
{
    [Fact]
    public void UpdateSummary_Updates_ListsMetadataThenEachArmyByName()
    {
        var check = ArmyDataCheck.Downloaded(
            ArmyDataChecks.Metadata(),
            metadataChanged: true,
            [
                ArmyDataChecks.Army("7.2", storedVersion: "7.1", isUpdate: true, factionId: 201, name: "Yu Jing"),
                ArmyDataChecks.Army("7.1", storedVersion: null, isUpdate: true, factionId: 301, name: "Ariadna"),
                ArmyDataChecks.Army("7.1", storedVersion: "7.1", isUpdate: false)
            ],
            []);

        Assert.Equal(
            "• Factions, weapons, skills and equipment\n• Ariadna: new (7.1)\n• Yu Jing: 7.1 → 7.2",
            check.UpdateSummary.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void UpdateSummary_LongList_ShortensIt()
    {
        var armies = Enumerable.Range(1, 10)
            .Select(index => ArmyDataChecks.Army("2.0", storedVersion: "1.0", isUpdate: true, factionId: index, name: $"Army {index:00}"))
            .ToList();

        var check = ArmyDataCheck.Downloaded(ArmyDataChecks.Metadata(), metadataChanged: false, armies, []);

        var lines = check.UpdateSummary.ReplaceLineEndings("\n").Split('\n');
        Assert.Equal(9, lines.Length);
        Assert.Equal("• Army 08: 1.0 → 2.0", lines[7]);
        Assert.Equal("• …and 2 more armies", lines[8]);
    }

    [Fact]
    public void UpToDate_NothingNew_HasNoUpdates()
    {
        var check = ArmyDataChecks.UpToDate();

        Assert.True(check.Succeeded);
        Assert.False(check.HasUpdates);
        Assert.Empty(check.Updates);
        Assert.Equal(string.Empty, check.UpdateSummary);
    }

    [Fact]
    public void Failed_Reason_KeepsIt()
    {
        var check = ArmyDataCheck.Failed("offline");

        Assert.False(check.Succeeded);
        Assert.Equal("offline", check.FailureMessage);
        Assert.False(check.HasUpdates);
    }
}
