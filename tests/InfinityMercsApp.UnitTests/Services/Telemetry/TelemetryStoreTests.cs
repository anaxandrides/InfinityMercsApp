using InfinityMercsApp.Services.Telemetry;

namespace InfinityMercsApp.UnitTests.Services.Telemetry;

public sealed class TelemetryStoreTests : IDisposable
{
    private readonly TempTelemetry _folder = new();

    public void Dispose() => _folder.Dispose();

    [Fact]
    public void Constructor_FirstLaunch_MakesAnInstallIdWithTelemetryOn()
    {
        var store = _folder.NewStore();

        Assert.True(Guid.TryParse(store.InstallId, out _));
        Assert.True(store.IsEnabled);
    }

    [Fact]
    public void Constructor_LaterLaunch_KeepsTheInstallIdAndSetting()
    {
        var first = _folder.NewStore();
        first.SetEnabled(false);

        var second = _folder.NewStore();

        Assert.Equal(first.InstallId, second.InstallId);
        Assert.False(second.IsEnabled);
    }

    [Fact]
    public void Record_TwoEvents_AreReadBackInOrder()
    {
        var store = _folder.NewStore();
        store.Record(TempTelemetry.Page("SettingsPage"));
        store.Record(TempTelemetry.Page("AboutPage"));

        var file = Assert.Single(store.TakePending());

        Assert.Equal(["SettingsPage", "AboutPage"], TelemetryStore.Read(file).Select(e => e.Name));
    }

    [Fact]
    public void Record_TelemetryOff_WritesNothing()
    {
        var store = _folder.NewStore();
        store.SetEnabled(false);

        store.Record(TempTelemetry.Page("SettingsPage"));

        Assert.Empty(store.TakePending());
    }

    [Fact]
    public void SetEnabled_Off_ThrowsAwayWhatIsWaiting()
    {
        var store = _folder.NewStore();
        store.Record(TempTelemetry.Page("SettingsPage"));

        store.SetEnabled(false);

        Assert.Empty(store.TakePending());
    }

    [Fact]
    public void Record_AfterTakePending_GoesToANewFile()
    {
        var store = _folder.NewStore();
        store.Record(TempTelemetry.Page("SettingsPage"));
        var taken = Assert.Single(store.TakePending());

        store.Record(TempTelemetry.Page("AboutPage"));

        Assert.Single(TelemetryStore.Read(taken));
        Assert.Equal(2, store.TakePending().Count);
    }

    [Fact]
    public void Read_LineCutShort_SkipsIt()
    {
        var store = _folder.NewStore();
        store.Record(TempTelemetry.Page("SettingsPage"));
        var file = Assert.Single(store.TakePending());
        File.AppendAllText(file, "{\"kind\":\"PageSh");

        Assert.Single(TelemetryStore.Read(file));
    }

    [Fact]
    public void Keep_SomeUnsent_LeavesOnlyThose()
    {
        var store = _folder.NewStore();
        store.Record(TempTelemetry.Page("SettingsPage"));
        store.Record(TempTelemetry.Page("AboutPage"));
        var file = Assert.Single(store.TakePending());

        store.Keep(file, [TempTelemetry.Page("AboutPage")]);

        Assert.Equal(["AboutPage"], TelemetryStore.Read(file).Select(e => e.Name));
    }

    [Fact]
    public void Keep_NoneUnsent_DeletesTheFile()
    {
        var store = _folder.NewStore();
        store.Record(TempTelemetry.Page("SettingsPage"));
        var file = Assert.Single(store.TakePending());

        store.Keep(file, []);

        Assert.False(File.Exists(file));
    }

    [Fact]
    public void Record_OutboxOverItsCap_DropsTheOldestFiles()
    {
        var store = _folder.NewStore();
        var page = new string('p', 100);
        var eventsPerFile = (int)(TelemetryStore.MaxFileBytes / 150);
        for (var i = 0; i < eventsPerFile * 12; i++)
        {
            store.Record(TempTelemetry.Page(page));
        }

        var total = store.TakePending().Sum(file => new FileInfo(file).Length);

        Assert.InRange(total, 1, TelemetryStore.MaxOutboxBytes);
    }
}
