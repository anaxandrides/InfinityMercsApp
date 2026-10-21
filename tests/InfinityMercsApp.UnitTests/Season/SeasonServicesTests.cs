using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Services;
using InfinityMercsApp.Domain.Season.Troopers;

namespace InfinityMercsApp.UnitTests.Season;

public sealed class SeasonServicesTests
{
    [Fact]
    public void Cost_EveryService_FollowsTheServicesTable()
    {
        Assert.Equal([4, 8, 4, 8, 15, 11], SeasonServices.All.Select(service => service.Cost()));
        Assert.Equal("Advanced Therapy", SeasonService.AdvancedTherapy.Name());
    }

    [Fact]
    public void RemoveInjury_InjuredTrooper_WritesOffInjuries()
    {
        var trooper = new SeasonTrooper { Injuries = [Injury.Arms, Injury.BodyInjury] };

        Assert.True(SeasonServices.RemoveInjury(trooper, Injury.Arms));
        Assert.False(SeasonServices.RemoveInjury(trooper, Injury.Arms));
        SeasonServices.RemoveAllInjuries(trooper);

        Assert.Empty(trooper.Injuries);
    }

    [Fact]
    public void CalmDown_FrenzyOrImpetuous_TakesThemAndCover()
    {
        var frenzied = new SeasonTrooper();
        SeasonServices.CalmDown(frenzied, SeasonService.CalmFrenzy, keepsNoCover: false);
        Assert.Equal(["Frenzy"], frenzied.CharacteristicsLost);

        var impetuous = new SeasonTrooper();
        SeasonServices.CalmDown(impetuous, SeasonService.CalmImpetuous, keepsNoCover: false);
        Assert.Equal(["Impetuous", "Partial Cover", "No Cover"], impetuous.CharacteristicsLost);

        var aerial = new SeasonTrooper();
        SeasonServices.CalmDown(aerial, SeasonService.CalmImpetuous, keepsNoCover: true);
        Assert.DoesNotContain("No Cover", aerial.CharacteristicsLost);
    }

    [Fact]
    public void TrainRegular_IrregularTrooper_SwapsForRegularOnce()
    {
        var trooper = new SeasonTrooper();

        SeasonServices.TrainRegular(trooper);
        SeasonServices.TrainRegular(trooper);

        Assert.Equal(["Irregular"], trooper.CharacteristicsLost);
        Assert.Equal(["Regular"], trooper.CharacteristicsGained);
    }

    [Fact]
    public void Retrain_PerkOfTheSameTier_SwapsIt()
    {
        var trooper = new SeasonTrooper { Perks = ["body-track-1-tier-1", "reflex-track-2-tier-1"] };
        var terrain = new PerkPick("body-track-6-tier-1", "Terrain (Total)", "body-8-13", "Body", 6, 1);

        Assert.True(SeasonServices.Retrain(trooper, [], "reflex-track-2-tier-1", terrain));

        Assert.Equal(["body-track-1-tier-1", "body-track-6-tier-1"], trooper.Perks);
    }

    // A perk from an earlier season is saved with the company, so the season records the swap.
    [Fact]
    public void Retrain_PerkSavedWithTheCompany_RecordsTheSwap()
    {
        var trooper = new SeasonTrooper();
        IReadOnlyList<string> starting = ["reflex-track-2-tier-1"];
        var terrain = new PerkPick("body-track-6-tier-1", "Terrain (Total)", "body-8-13", "Body", 6, 1);
        var climbing = new PerkPick("body-track-1-tier-1", "Climbing Plus", "body-8-13", "Body", 1, 1);

        Assert.True(SeasonServices.Retrain(trooper, starting, "reflex-track-2-tier-1", terrain));
        Assert.True(SeasonServices.Retrain(trooper, starting, "body-track-6-tier-1", climbing));

        var swap = Assert.Single(trooper.RetrainedPerks);
        Assert.Equal(("reflex-track-2-tier-1", "body-track-1-tier-1"), (swap.Replaced, swap.Replacement));
        Assert.Equal(["body-track-1-tier-1"], TrooperProgress.Retrained(starting, trooper));
        Assert.Empty(trooper.Perks);
    }

    [Fact]
    public void Retrain_OtherTierOrPerkBuiltOn_Refuses()
    {
        var trooper = new SeasonTrooper { Perks = ["body-track-1-tier-1", "body-track-1-tier-3"] };
        var tierTwo = new PerkPick("reflex-track-8-tier-2", "+1 BS", "reflex-11-16", "Reflex", 8, 2);
        var tierOne = new PerkPick("body-track-6-tier-1", "Terrain (Total)", "body-8-13", "Body", 6, 1);

        Assert.False(SeasonServices.Retrain(trooper, [], "body-track-1-tier-3", tierTwo));
        Assert.False(SeasonServices.Retrain(trooper, [], "body-track-1-tier-1", tierOne));
        Assert.False(SeasonServices.Retrain(trooper, [], "body-track-9-tier-3", tierOne));
        Assert.False(SeasonServices.CanRetrain(trooper.Perks, "body-track-1-tier-1"));
    }
}
