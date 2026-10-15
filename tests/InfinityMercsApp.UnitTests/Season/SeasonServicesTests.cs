using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Services;
using InfinityMercsApp.Domain.Season.Troopers;

namespace InfinityMercsApp.UnitTests.Season;

public sealed class SeasonServicesTests
{
    [Fact]
    public void PricesFollowTheServicesTable()
    {
        Assert.Equal([4, 8, 4, 8, 15, 11], SeasonServices.All.Select(service => service.Cost()));
        Assert.Equal("Advanced Therapy", SeasonService.AdvancedTherapy.Name());
    }

    [Fact]
    public void TherapyWritesOffInjuries()
    {
        var trooper = new SeasonTrooper { Injuries = [Injury.Arms, Injury.BodyInjury] };

        Assert.True(SeasonServices.RemoveInjury(trooper, Injury.Arms));
        Assert.False(SeasonServices.RemoveInjury(trooper, Injury.Arms));
        SeasonServices.RemoveAllInjuries(trooper);

        Assert.Empty(trooper.Injuries);
    }

    [Fact]
    public void AngerManagementTakesFrenzyOrImpetuousAndCover()
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
    public void RegularTrainingSwapsIrregularForRegular()
    {
        var trooper = new SeasonTrooper();

        SeasonServices.TrainRegular(trooper);
        SeasonServices.TrainRegular(trooper);

        Assert.Equal(["Irregular"], trooper.CharacteristicsLost);
        Assert.Equal(["Regular"], trooper.CharacteristicsGained);
    }

    [Fact]
    public void RetrainingSwapsAPerkForOneOfTheSameTier()
    {
        var trooper = new SeasonTrooper { Perks = ["body-track-1-tier-1", "reflex-track-2-tier-1"] };
        var terrain = new PerkPick("body-track-6-tier-1", "Terrain (Total)", "body-8-13", "Body", 6, 1);

        Assert.True(SeasonServices.Retrain(trooper, "reflex-track-2-tier-1", terrain));

        Assert.Equal(["body-track-1-tier-1", "body-track-6-tier-1"], trooper.Perks);
    }

    [Fact]
    public void RetrainingRefusesAnotherTierOrAPerkOthersBuildOn()
    {
        var trooper = new SeasonTrooper { Perks = ["body-track-1-tier-1", "body-track-1-tier-3"] };
        var tierTwo = new PerkPick("reflex-track-8-tier-2", "+1 BS", "reflex-11-16", "Reflex", 8, 2);
        var tierOne = new PerkPick("body-track-6-tier-1", "Terrain (Total)", "body-8-13", "Body", 6, 1);

        Assert.False(SeasonServices.Retrain(trooper, "body-track-1-tier-3", tierTwo));
        Assert.False(SeasonServices.Retrain(trooper, "body-track-1-tier-1", tierOne));
        Assert.False(SeasonServices.Retrain(trooper, "body-track-9-tier-3", tierOne));
        Assert.False(SeasonServices.CanRetrain(trooper.Perks, "body-track-1-tier-1"));
    }
}
