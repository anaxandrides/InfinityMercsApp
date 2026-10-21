using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Inducements;
using InfinityMercsApp.Services.Season.Inducements;
using InfinityMercsApp.UnitTests.Support;

namespace InfinityMercsApp.UnitTests.Services.Season.Inducements;

public sealed class HiredTroopsTests
{
    // PanOceania fields Authorized Bounty Hunters (10049): options 2 (Boarding Shotgun), 3 (Sniper Rifle),
    // 4 (Spitfire) and 5 (Submachine Gun).
    private const int BountyHunters = 10049;

    private readonly HiredTroops _hired = new(Army101Data.Create());

    private static HiredTroopOffer Row(string name, string profile) =>
        HiredTroopChart.Offers.First(offer => offer.Name == name && offer.Profile == profile);

    [Fact]
    public void ChoicesFor_SmgOrBsgRow_ListsThoseLoadoutsFromTheArmy()
    {
        var choices = _hired.ChoicesFor(Row("Authorized Bounty Hunter", "SMG or BSG"));

        Assert.Equal([2, 5], choices.Select(choice => choice.OptionId));
        Assert.All(choices, choice => Assert.Equal((Army101Data.PanOceania, BountyHunters), (choice.FactionId, choice.UnitId)));
        Assert.StartsWith("AUTHORIZED BOUNTY HUNTERS: Boarding Shotgun", choices[0].Name);
    }

    [Fact]
    public void ChoicesFor_UnitNoArmyFields_IsEmpty() => Assert.Empty(_hired.ChoicesFor(Row("Brawler", "Sniper")));

    [Fact]
    public void ProfileOf_HiredTroop_GivesItsLoadoutWithTheRowsSkills()
    {
        var profile = _hired.ProfileOf(
            new HiredTroop { FactionId = Army101Data.PanOceania, UnitId = BountyHunters, GroupId = 1, OptionId = 4, Skills = ["Immunity (POS)"] },
            showUnitsInInches: false);

        Assert.NotNull(profile);
        Assert.Contains("Spitfire", profile.RangedWeapons);
        Assert.Contains(profile.MeleeWeapons, weapon => weapon.StartsWith("PARA CC Weapon", StringComparison.Ordinal));
        Assert.Contains("Immunity (POS)", profile.Skills);
        Assert.Equal(Stat.Vita, profile.Vitality.Stat);
        Assert.False(profile.IsIrregular);
    }

    [Fact]
    public void ProfileOf_UnknownOption_IsNull() =>
        Assert.Null(_hired.ProfileOf(new HiredTroop { FactionId = Army101Data.PanOceania, UnitId = BountyHunters, GroupId = 1, OptionId = 99 }, false));
}
