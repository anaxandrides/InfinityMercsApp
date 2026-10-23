using System.Collections.ObjectModel;
using InfinityMercsApp.ViewModels.Company.Selection;
using InfinityMercsApp.ViewModels.Company.Selection.Items;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Selection;

public sealed class CoreFireteamTrackerTests
{
    private readonly ObservableCollection<ArmyTeamListItem> _teams = [];
    private readonly ObservableCollection<MercsCompanyEntry> _entries = [];

    private CoreFireteamTracker Tracker()
    {
        var tracker = new CoreFireteamTracker(_teams, _entries);
        _teams.Add(Team("Fusilier Core", "Fusilier", 1, tracker));
        _teams.Add(Team("Bulleteer Core", "Bulleteer", 2, tracker));
        return tracker;
    }

    private static ArmyTeamListItem Team(string name, string unitName, int unitId, IFireteamTracking? tracking = null) => new()
    {
        Tracking = tracking,
        Name = name,
        AllowedProfiles = [new ArmyTeamUnitLimitItem { Name = unitName, ResolvedUnitId = unitId, ResolvedSourceFactionId = 101, Max = "5" }]
    };

    [Fact]
    public void RestoreAfterReload_NothingTracked_TracksTheFirstFireteam()
    {
        var tracker = Tracker();

        tracker.RestoreAfterReload();

        Assert.Equal("Fusilier Core", tracker.Name);
        Assert.True(_teams[0].IsTrackedTeam);
    }

    [Fact]
    public void IsTrackedTeam_Ticked_TracksThatFireteam()
    {
        var tracker = Tracker();
        tracker.RestoreAfterReload();

        _teams[1].IsTrackedTeam = true;

        Assert.Equal("Bulleteer Core", tracker.Name);
        Assert.False(_teams[0].IsTrackedTeam);
    }

    [Fact]
    public void IsTrackedTeam_TickedWhileFixed_KeepsTheSavedFireteam()
    {
        var tracker = Tracker();
        tracker.Fix("Fusilier Core");
        tracker.RestoreAfterReload();

        _teams[1].IsTrackedTeam = true;

        Assert.Equal("Fusilier Core", tracker.Name);
    }

    [Fact]
    public void Reevaluate_TrooperOutsideTheFireteam_IsIrregular()
    {
        var tracker = Tracker();
        _entries.Add(new MercsCompanyEntry { Name = "Fusilier", BaseUnitName = "Fusilier", SourceUnitId = 1, SourceFactionId = 101 });
        _entries.Add(new MercsCompanyEntry { Name = "Bulleteer", BaseUnitName = "Bulleteer", SourceUnitId = 2, SourceFactionId = 101 });

        tracker.Track("Fusilier Core");

        Assert.Equal([false, true], _entries.Select(entry => entry.IsIrregular));
        Assert.True(tracker.CanJoin(new ArmyUnitSelectionItem { Id = 1, SourceFactionId = 101, Name = "Fusilier" }));
        Assert.False(tracker.CanJoin(new ArmyUnitSelectionItem { Id = 2, SourceFactionId = 101, Name = "Bulleteer" }));
    }

    [Fact]
    public void Track_Nothing_ReadsAsSelectFireteam()
    {
        var tracker = Tracker();

        tracker.Track(null);

        Assert.Equal("Select fireteam", tracker.NameDisplay);
        Assert.Null(tracker.Slots());
        Assert.Equal(0, tracker.Level);
    }
}
