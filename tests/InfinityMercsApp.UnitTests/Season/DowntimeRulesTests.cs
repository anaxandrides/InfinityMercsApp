using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Season.Downtime;
using InfinityMercsApp.Domain.Season.Experience;

namespace InfinityMercsApp.UnitTests.Season;

public sealed class DowntimeRulesTests
{
    private static ExperienceUnitResult Unit(string name, int renown = 0, bool isMvp = false, bool isLieutenant = false, string skills = "") => new()
    {
        Name = name,
        Renown = renown,
        IsMvp = isMvp,
        IsLieutenant = isLieutenant,
        Skills = skills,
        UnitCc = "14",
        UnitBs = "11",
        UnitPh = "10",
        UnitWip = "13",
        UnitArm = "2"
    };

    private static DowntimeChoice Choice(DowntimeTest? test, params IReadOnlyList<DowntimeTrait> traits) =>
        new() { Test = test, Description = "test choice", Traits = traits };

    [Theory]
    [InlineData(5, 10, PassFailOutcome.Pass)]
    [InlineData(10, 10, PassFailOutcome.CritPass)]
    [InlineData(11, 10, PassFailOutcome.Fail)]
    [InlineData(20, 20, PassFailOutcome.CritPass)]
    [InlineData(19, 23, PassFailOutcome.Pass)]
    [InlineData(3, 23, PassFailOutcome.CritPass)]
    [InlineData(4, 23, PassFailOutcome.Pass)]
    [InlineData(20, 23, PassFailOutcome.CritPass)]
    public void ClassifyReadsARollAgainstItsTarget(int roll, int target, PassFailOutcome expected) =>
        Assert.Equal(expected, DowntimeRules.Classify(roll, target));

    [Theory]
    [InlineData(13, "13")]
    [InlineData(22, "22  (crit on 20 and 1–2)")]
    public void TargetTextSpellsOutTheExtraCriticalRange(int target, string expected) =>
        Assert.Equal(expected, DowntimeRules.TargetText(target));

    [Fact]
    public void TestTargetsComeFromTheParticipantsStats()
    {
        var unit = Unit("Alpha");

        Assert.Equal(10, DowntimeRules.TestTarget(DowntimeTest.Ps10, null));
        Assert.Equal(10, DowntimeRules.TestTarget(DowntimeTest.Ps10PlusArm, null));
        Assert.Equal(12, DowntimeRules.TestTarget(DowntimeTest.Ps10PlusArm, unit));
        Assert.Equal(4, DowntimeRules.TestTarget(DowntimeTest.For(Stat.Cc, -10), unit));
        Assert.Equal(13, DowntimeRules.TestTarget(DowntimeTest.For(Stat.Wip), unit));
        Assert.Null(DowntimeRules.TestTarget(DowntimeTest.For(Stat.Wip), null));
        Assert.Null(DowntimeRules.TestTarget(DowntimeTest.For(Stat.Bts), unit));
    }

    [Fact]
    public void ANonNumericStatHasNoTarget() =>
        Assert.Null(DowntimeRules.TestTarget(DowntimeTest.For(Stat.Ph), new ExperienceUnitResult { UnitPh = "-" }));

    [Fact]
    public void ANonNumericArmCountsAsZero() =>
        Assert.Equal(10, DowntimeRules.TestTarget(DowntimeTest.Ps10PlusArm, new ExperienceUnitResult { UnitArm = "-" }));

    [Fact]
    public void TheEventsParticipantWinsOverTheChoices()
    {
        var captainChoice = Choice(null, new DowntimeTrait(TraitKind.Captain));

        Assert.Equal(ParticipantKind.Renowned, DowntimeRules.RequiredParticipant(new DowntimeEvent { RequiredParticipant = ParticipantKind.Renowned }, captainChoice));
        Assert.Equal(ParticipantKind.Captain, DowntimeRules.RequiredParticipant(new DowntimeEvent(), captainChoice));
        Assert.Equal(ParticipantKind.AnyMerc, DowntimeRules.RequiredParticipant(new DowntimeEvent(), Choice(null)));
    }

    [Fact]
    public void CandidatesAreFilteredByWhoMustTakePart()
    {
        IReadOnlyList<ExperienceUnitResult> units =
        [
            Unit("Alpha", renown: 30, isLieutenant: true),
            Unit("Bravo", renown: 30, isMvp: true),
            Unit("Charlie", renown: 10)
        ];

        Assert.Equal(["Alpha", "Bravo", "Charlie"], DowntimeRules.Candidates(units, ParticipantKind.AnyMerc).Select(u => u.Name));
        Assert.Equal(["Alpha"], DowntimeRules.Candidates(units, ParticipantKind.Captain).Select(u => u.Name));
        Assert.Equal(["Bravo"], DowntimeRules.Candidates(units, ParticipantKind.Mvp).Select(u => u.Name));
        Assert.Equal(["Alpha", "Bravo"], DowntimeRules.Candidates(units, ParticipantKind.Renowned).Select(u => u.Name));
        Assert.Empty(DowntimeRules.Candidates([], ParticipantKind.Renowned));
    }

    [Fact]
    public void MissingRequirementsAreThoseNotInSkillsOrEquipment()
    {
        var choice = Choice(null, new DowntimeTrait(TraitKind.Requirement, "Hacker"), new DowntimeTrait(TraitKind.Requirement, "Trinity Program"));
        var hacker = Unit("Alpha", skills: "Hacker (Killer)");

        Assert.Equal(["Trinity Program"], DowntimeRules.MissingRequirements(choice, hacker));
        Assert.Equal(["Hacker", "Trinity Program"], DowntimeRules.MissingRequirements(choice, Unit("Bravo")));
    }

    [Fact]
    public void ConsequencesDescribeEachTraitForTheOutcome()
    {
        var choice = Choice(
            DowntimeTest.Ps10,
            new DowntimeTrait(TraitKind.Chaotic),
            new DowntimeTrait(TraitKind.Xp),
            new DowntimeTrait(TraitKind.Attack),
            new DowntimeTrait(TraitKind.Weapon),
            new DowntimeTrait(TraitKind.Note, "Read the fine print"));
        var pistol = new PistolRoll("Breaker Pistol", 7, 1);

        Assert.Equal(
            ["Gain 2 Notoriety", "Alpha gains 3 XP", "Roll a random pistol (+1 SD): Breaker Pistol (+1 SD) (rolled 7)", "Read the fine print"],
            DowntimeRules.ConsequenceLines(choice, PassFailOutcome.CritPass, "Alpha", pistol));
        Assert.Equal(
            ["Lose 1 Notoriety", "Injury inflicted on selected merc", "Read the fine print"],
            DowntimeRules.ConsequenceLines(choice, PassFailOutcome.Fail, null, null));
    }

    [Fact]
    public void ANegatedTraitOnlyCountsOnAFail()
    {
        var choice = Choice(DowntimeTest.Ps10, new DowntimeTrait(TraitKind.Xp), new DowntimeTrait(TraitKind.Cr, Negated: true));

        Assert.Equal(["Alpha gains 2 XP"], DowntimeRules.ConsequenceLines(choice, PassFailOutcome.Pass, "Alpha", null));
        Assert.Equal(["Lose 5 CR"], DowntimeRules.ConsequenceLines(choice, PassFailOutcome.Fail, "Alpha", null));
        Assert.Equal(0, DowntimeRules.Totals(choice, PassFailOutcome.Pass, 0).CrGain);
        Assert.Equal(-5, DowntimeRules.Totals(choice, PassFailOutcome.Fail, 0).CrGain);
    }

    [Theory]
    [InlineData(PassFailOutcome.Fail, 4)]
    [InlineData(PassFailOutcome.Pass, 4)]
    [InlineData(PassFailOutcome.CritPass, 0)]
    public void CrSpentOnAP2PTestIsKeptOnlyOnACritical(PassFailOutcome outcome, int spent) =>
        Assert.Equal(spent, DowntimeRules.Totals(Choice(DowntimeTest.Ps10, new DowntimeTrait(TraitKind.P2P)), outcome, 4).SpentCr);

    [Fact]
    public void TotalsAddUpEveryTrait()
    {
        var choice = Choice(
            DowntimeTest.Ps10,
            new DowntimeTrait(TraitKind.Lawful),
            new DowntimeTrait(TraitKind.Cr),
            new DowntimeTrait(TraitKind.Xp),
            new DowntimeTrait(TraitKind.Swc),
            new DowntimeTrait(TraitKind.Weapon),
            new DowntimeTrait(TraitKind.Skill, "Stealth"),
            new DowntimeTrait(TraitKind.Lt));

        var totals = DowntimeRules.Totals(choice, PassFailOutcome.Pass, 0);

        Assert.Equal(new DowntimeTotals(5, -1, 2, 0, 0.5, totals.OtherEffects), totals);
        Assert.Equal(["Random pistol", "Skill awarded: Stealth", "New Captain may be selected"], totals.OtherEffects);
    }

    [Fact]
    public void ThePistolIsOnlyWonOnAPass()
    {
        var choice = Choice(DowntimeTest.Ps10, new DowntimeTrait(TraitKind.Weapon));

        Assert.True(DowntimeRules.WinsPistol(choice, PassFailOutcome.Pass));
        Assert.True(DowntimeRules.WinsPistol(choice, PassFailOutcome.CritPass));
        Assert.False(DowntimeRules.WinsPistol(choice, PassFailOutcome.Fail));
        Assert.False(DowntimeRules.WinsPistol(Choice(DowntimeTest.Ps10), PassFailOutcome.Pass));
    }

    [Fact]
    public void TheParticipantGainsXpOrAnInjury()
    {
        var choice = Choice(DowntimeTest.Ps10, new DowntimeTrait(TraitKind.Xp), new DowntimeTrait(TraitKind.Attack));
        var winner = Unit("Alpha");
        var loser = Unit("Bravo");

        DowntimeRules.ApplyToParticipant(choice, PassFailOutcome.CritPass, winner);
        DowntimeRules.ApplyToParticipant(choice, PassFailOutcome.Fail, loser);

        Assert.Equal((3, false), (winner.XpData.DowntimeBonusXp, winner.GainedInjury));
        Assert.Equal((0, true), (loser.XpData.DowntimeBonusXp, loser.GainedInjury));
    }
}
