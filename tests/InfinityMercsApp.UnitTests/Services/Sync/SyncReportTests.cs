using InfinityMercsApp.Services.Sync;

namespace InfinityMercsApp.UnitTests.Services.Sync;

public sealed class SyncReportTests
{
    private static SyncReport Synced(FileCounts online, FileCounts unclaimed) =>
        new(SyncRunStatus.Synced, 0, 0, [], online, unclaimed);

    [Fact]
    public void Summary_NothingNewToSend_SaysWhatIsOnline() =>
        Assert.Equal("Synced. 2 companies and 1 season online.", Synced(new FileCounts(2, 1), FileCounts.None).Summary);

    [Fact]
    public void Summary_NothingOnline_SaysSo() =>
        Assert.Equal("Synced. Nothing is online yet.", Synced(FileCounts.None, FileCounts.None).Summary);

    [Fact]
    public void Summary_FilesNotInTheAccount_SaysHowMany() =>
        Assert.Equal(
            "Synced. 1 company online. 2 seasons on this device aren't in your account yet.",
            Synced(new FileCounts(1, 0), new FileCounts(0, 2)).Summary);
}
