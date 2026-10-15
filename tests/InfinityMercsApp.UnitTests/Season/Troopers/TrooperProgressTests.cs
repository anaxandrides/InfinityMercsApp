using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Troopers;

namespace InfinityMercsApp.UnitTests.Season.Troopers;

public sealed class TrooperProgressTests
{
    private static SeasonFile SeasonWith(int xp, SeasonTrooper? trooper = null)
    {
        var season = new SeasonFile
        {
            Rounds = [new SeasonRound { MissionResults = new SeasonMissionResult { UnitResults = [new() { EntryIndex = 1, UnitName = "Bolt", XpGained = xp }] } }]
        };
        if (trooper is not null)
        {
            season.Troopers[1] = trooper;
        }

        return season;
    }

    private static TrooperProgress Bolt(SeasonFile season, int startingXp = 0, bool isTag = false, IReadOnlyList<string>? startingPerks = null) =>
        TrooperProgress.For(1, "Bolt", startingXp, startingPerks ?? [], isTag, season);

    [Fact]
    public void ExperienceAddsTheSeasonToTheSavedValue()
    {
        var progress = Bolt(SeasonWith(12), startingXp: 4);

        Assert.Equal(16, progress.TotalXp);
        Assert.Equal((2, "Hired Gun"), (progress.RankLevel, progress.RankName));
    }

    [Fact]
    public void HiredGunNeedsFifteen()
    {
        Assert.Equal(1, Bolt(SeasonWith(14)).RankLevel);
        Assert.Equal(2, Bolt(SeasonWith(15)).RankLevel);
    }

    [Fact]
    public void EachRankOwesAPerkPoint()
    {
        var progress = Bolt(SeasonWith(31), startingPerks: ["body-track-1-tier-1"]);

        Assert.Equal(2, progress.PerkPointsOwed);
        Assert.Equal(PerkPointKind.Standard, progress.NextPerkPoint);
        Assert.Equal(0, progress.TagPerkPointsOwed);
    }

    [Fact]
    public void ATagAlsoOwesATagPerk()
    {
        var progress = Bolt(SeasonWith(5, new SeasonTrooper { Perks = ["body-track-1-tier-1"] }), isTag: true);

        Assert.Equal((0, 1), (progress.PerkPointsOwed, progress.TagPerkPointsOwed));
        Assert.Equal(PerkPointKind.Tag, progress.NextPerkPoint);
    }

    [Fact]
    public void NothingOwedWhenUnranked()
    {
        var progress = Bolt(SeasonWith(4));

        Assert.False(progress.HasPerkPointsToSpend);
        Assert.Null(progress.NextPerkPoint);
    }

    [Fact]
    public void TheChangeAddsInjuriesPerksRankSkillsAndServices()
    {
        var trooper = new SeasonTrooper
        {
            Injuries = [Injury.Arms],
            Perks = ["reflex-track-8-tier-2", "body-track-1-tier-1"],
            SkillsGained = ["Natural Born Warrior"],
            CharacteristicsLost = ["Irregular"]
        };

        var change = Bolt(SeasonWith(30, trooper)).Change;

        Assert.Equal(0, change.Bs);
        Assert.Equal(["Climbing Plus", "Courage", "Fireteam: Mercs", "Natural Born Warrior"], change.SkillsGained);
        Assert.Equal(["Irregular"], change.CharacteristicsLost);
    }

    [Fact]
    public void ATrooperWithoutARecordHasAnEmptyOne()
    {
        Assert.Empty(Bolt(SeasonWith(0)).Record.Injuries);
        Assert.Equal(0, TrooperProgress.For(1, "Bolt", -5, [], false, null).TotalXp);
    }
}
