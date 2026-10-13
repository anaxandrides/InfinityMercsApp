using InfinityMercsApp.Domain.Captain;

namespace InfinityMercsApp.UnitTests.Captain;

public sealed class CaptainUpgradeRulesTests
{
    [Theory]
    [InlineData(10, 18)]
    [InlineData(28, 0)]
    [InlineData(35, 0)]
    public void StartingExperience_IsWhatTheUnitCostsUnder28(int unitCost, int expected) =>
        Assert.Equal(expected, CaptainUpgradeRules.StartingExperience(unitCost));

    [Fact]
    public void StartingExperience_UsesTheOverrideWhenGiven() =>
        Assert.Equal(5, CaptainUpgradeRules.StartingExperience(10, overrideExperience: 5));

    [Fact]
    public void StatOptions_AccumulateCostAcrossTiers()
    {
        var options = CaptainUpgradeRules.StatOptions("CC", 14);
        Assert.Equal(
            new[] { (0, 0, 0), (1, 2, 2), (2, 5, 5), (3, 10, 10) },
            options.Select(o => (o.Tier, o.Bonus, o.Cost)));
    }

    [Fact]
    public void StatOptions_ClampToTheHardCapAndStopThere()
    {
        // PH caps at 14: from 13, tier 1 reaches 14 and no further tiers are offered.
        var options = CaptainUpgradeRules.StatOptions("PH", 13);
        Assert.Equal(new[] { (0, 0, 0), (1, 1, 2) }, options.Select(o => (o.Tier, o.Bonus, o.Cost)));

        // From 12, tier 2 would give +3 but is clamped to +2.
        Assert.Equal(new[] { 0, 1, 2 }, CaptainUpgradeRules.StatOptions("PH", 12).Select(o => o.Bonus));
    }

    [Fact]
    public void StatOptions_AtTheCapOfferOnlyNoUpgrade()
    {
        Assert.Single(CaptainUpgradeRules.StatOptions("VITA", 2));
        Assert.Single(CaptainUpgradeRules.StatOptions("WIP", 15));
    }

    [Fact]
    public void StatOptions_ForAnUnknownStatOfferOnlyNoUpgrade() =>
        Assert.Equal(new[] { 0 }, CaptainUpgradeRules.StatOptions("MOV", 4).Select(o => o.Tier));

    [Fact]
    public void StatOptions_IgnoreCase() =>
        Assert.Equal(4, CaptainUpgradeRules.StatOptions("bts", 3).Count);
}

public sealed class CaptainStatBonusesTests
{
    private static readonly CaptainStatBonuses Bonuses = new(Cc: 2, Bs: 1, Ph: 0, Wip: 3, Arm: 1, Bts: 6, Vitality: 1);

    [Theory]
    [InlineData("VITA", 1)]
    [InlineData("str", 1)]
    [InlineData("W", 1)]
    [InlineData("BTS", 6)]
    [InlineData("MOV", 0)]
    public void For_MapsStatlineKeys(string key, int expected) => Assert.Equal(expected, Bonuses.For(key));

    [Theory]
    [InlineData("13", 2, "15")]
    [InlineData("-", 2, "-")]
    [InlineData("T", 1, "T")]
    [InlineData("13", 0, "13")]
    public void Apply_AddsToWholeNumbersOnly(string value, int bonus, string expected) =>
        Assert.Equal(expected, CaptainStatBonuses.Apply(value, bonus));

    [Fact]
    public void ApplyToStatline_AddsEachBonusAndKeepsOtherSegments() =>
        Assert.Equal(
            "MOV 10-5 | CC 16 | BS 13 | PH 10 | WIP 16 | ARM 2 | BTS 9 | VITA 2 | S 2",
            Bonuses.ApplyToStatline("MOV 10-5 | CC 14 | BS 12 | PH 10 | WIP 13 | ARM 1 | BTS 3 | VITA 1 | S 2"));

    [Fact]
    public void ApplyToStatline_LeavesTextWithoutStatsUnchanged()
    {
        Assert.Equal("Captain", Bonuses.ApplyToStatline("Captain"));
        Assert.Equal(string.Empty, Bonuses.ApplyToStatline(null));
    }
}

public sealed class CaptainChoiceLabelTests
{
    [Fact]
    public void Format_PrefixesTheCostAndNeverShowsANegativeOne()
    {
        Assert.Equal("(3) - Sniper", CaptainChoiceLabel.Format(3, "Sniper"));
        Assert.Equal("(0) - Sniper", CaptainChoiceLabel.Format(-2, "Sniper"));
    }

    [Theory]
    [InlineData("(3) - Sniper", 3)]
    [InlineData("(None)", 0)]
    [InlineData("Sniper", 0)]
    [InlineData(null, 0)]
    public void Cost_ReadsTheLeadingNumber(string? label, int expected) => Assert.Equal(expected, CaptainChoiceLabel.Cost(label));

    [Theory]
    [InlineData("(3) - Sniper", "Sniper")]
    [InlineData("(0) - Lt", "Lieutenant")]
    [InlineData("(None)", "")]
    [InlineData("Plain Name", "Plain Name")]
    public void Name_StripsTheCost(string label, string expected) => Assert.Equal(expected, CaptainChoiceLabel.Name(label));
}

public sealed class ProfileTextTests
{
    private static readonly string NewLine = Environment.NewLine;

    [Fact]
    public void Lines_DropBlanksAndPlaceholders() =>
        Assert.Equal(new[] { "Combi Rifle", "Pistol" }, ProfileText.Lines($" Combi Rifle {NewLine}-{NewLine}{NewLine}Pistol"));

    [Fact]
    public void AppendDistinct_SkipsItemsAlreadyListed() =>
        Assert.Equal($"Combi Rifle{NewLine}Pistol{NewLine}Sniper", ProfileText.AppendDistinct($"Combi Rifle{NewLine}Pistol", ["pistol", "Sniper"]));

    [Fact]
    public void AppendDistinct_NormalisesOrKeepsTextWhenNothingIsAdded()
    {
        Assert.Equal("-", ProfileText.AppendDistinct("  -  ", []));
        Assert.Equal("  -  ", ProfileText.AppendDistinct("  -  ", [], keepTextWhenNothingAdded: true));
        Assert.Equal("-", ProfileText.AppendDistinct(null, [], keepTextWhenNothingAdded: true));
    }

    [Fact]
    public void AppendMarked_MarksEachAddition() =>
        Assert.Equal($"Pistol{NewLine}+ Sniper", ProfileText.AppendMarked("Pistol", ["Sniper"]));

    [Fact]
    public void AppendChoices_TrimsAndSkipsBlanksDashesAndDuplicates() =>
        Assert.Equal(new[] { "Pistol", "Sniper" }, ProfileText.AppendChoices([" Pistol ", ""], "Sniper", "-", null, "pistol"));
}

public sealed class StatlineTests
{
    [Fact]
    public void NumericStats_ReadTheCombatStats() =>
        Assert.Equal(
            new Dictionary<string, int> { ["CC"] = 14, ["BS"] = 12, ["VITA"] = 1 },
            Statline.NumericStats("MOV 10-5 | cc 14 | BS 12 | VITA 1 | AVA T"));

    [Fact]
    public void Entries_KeepEveryKeyValueSegmentInOrder() =>
        Assert.Equal(
            new[] { ("MOV", "10-5"), ("CC", "14"), ("AVA", "T") },
            Statline.Entries("MOV 10-5 | cc 14 | junk | AVA T"));
}
