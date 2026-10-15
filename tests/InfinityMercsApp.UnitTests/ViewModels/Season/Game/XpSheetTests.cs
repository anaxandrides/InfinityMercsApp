using InfinityMercsApp.Domain.Season.Experience;
using InfinityMercsApp.ViewModels.Season.Game;

namespace InfinityMercsApp.UnitTests.ViewModels.Season.Game;

public sealed class XpSheetTests
{
    [Fact]
    public void NumberedBoxesTickInOrder()
    {
        var sheet = new XpSheet("Alpha", new UnitXpData());

        Assert.Equal([true, false, false], sheet.InflictState.Select(check => check.IsEnabled));

        sheet.InflictState[0].IsChecked = true;
        Assert.Equal([true, true, false], sheet.InflictState.Select(check => check.IsEnabled));

        sheet.InflictState[1].IsChecked = true;
        Assert.Equal([false, true, true], sheet.InflictState.Select(check => check.IsEnabled));
    }

    [Fact]
    public void TicksAreWrittenToTheXpAndTotalled()
    {
        var xp = new UnitXpData();
        var sheet = new XpSheet("Alpha", xp);
        var changed = new List<string?>();
        sheet.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        sheet.Assist[0].IsChecked = true;
        sheet.TagAndBag.IsChecked = true;

        Assert.True(xp.Assist[0]);
        Assert.True(xp.TagAndBag);
        Assert.Equal(xp.TotalXp, sheet.TotalXp);
        Assert.Equal($"Total XP Earned: {xp.TotalXp}", sheet.TotalText);
        Assert.Contains(nameof(XpSheet.TotalText), changed);
    }

    [Fact]
    public void SucceedingNeedsAnAttempt()
    {
        var xp = new UnitXpData();
        var sheet = new XpSheet("Alpha", xp);

        sheet.Succeeded.IsChecked = true;
        Assert.True(sheet.Attempted.IsChecked);
        Assert.True(xp.AttemptButton && xp.SucceedButton);

        sheet.Attempted.IsChecked = false;
        Assert.False(sheet.Succeeded.IsChecked);
        Assert.False(xp.AttemptButton || xp.SucceedButton);
    }

    [Fact]
    public void ScanningWithAForwardObserverNeedsAScan()
    {
        var xp = new UnitXpData();
        var sheet = new XpSheet("Alpha", xp);

        sheet.ScannedWithFo.IsChecked = true;
        Assert.True(sheet.Scanned.IsChecked);

        sheet.Scanned.IsChecked = false;
        Assert.False(sheet.ScannedWithFo.IsChecked);
        Assert.False(xp.ScanEnemy || xp.ScanEnemyFo);
    }

    [Fact]
    public void TheSheetStartsFromTheSavedXp()
    {
        var xp = new UnitXpData { AttemptButton = true };
        xp.Assist[0] = true;

        var sheet = new XpSheet("Alpha", xp);

        Assert.True(sheet.Attempted.IsChecked);
        Assert.True(sheet.Assist[0].IsChecked);
        Assert.Equal([true, true], sheet.Assist.Select(check => check.IsEnabled));
    }
}
