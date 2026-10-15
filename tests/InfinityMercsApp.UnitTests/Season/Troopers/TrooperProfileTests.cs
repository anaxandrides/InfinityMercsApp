using InfinityMercsApp.Domain.Season.Troopers;

namespace InfinityMercsApp.UnitTests.Season.Troopers;

public sealed class TrooperProfileTests
{
    private static TrooperProfile Fusilier(string vitality = "1", IReadOnlyList<string>? skills = null) =>
        new("10-5", "13", "12", "10", "13", "1", "0", vitality, "2", skills ?? ["Courage"], ["Regular", "Cube"]);

    [Fact]
    public void AttributesChangeAndDashesStay()
    {
        var profile = Fusilier() with { Bts = "-" };

        var changed = profile.With(new TrooperChange { Cc = -3, Wip = -1, Bts = 3 });

        Assert.Equal(("10", "12", "-"), (changed.Cc, changed.Wip, changed.Bts));
    }

    [Fact]
    public void MovementChangesInGameInches()
    {
        Assert.Equal("10cm-3cm", Fusilier().With(new TrooperChange { SecondMoveInches = -1 }).Move);
        Assert.Equal("5cm-5cm", Fusilier().With(new TrooperChange { FirstMoveInches = -2 }).Move);
        Assert.Equal("15cm-10cm", Fusilier().With(new TrooperChange { Move = new MoveInches(6, 4) }).Move);
        Assert.Equal("10-5", Fusilier().With(TrooperChange.None).Move);
    }

    public static TheoryData<string, IReadOnlyList<string>, int, string, string?> WoundsSteps => new()
    {
        { "1", [], 1, "1", "Dogged" },
        { "1", ["Dogged"], 1, "1", "No Wound Incapacitation" },
        { "1", ["NWI"], 1, "2", null },
        { "2", [], -1, "1", "No Wound Incapacitation" },
        { "1", [], -1, "1", null },
        { "1", [], 2, "1", "No Wound Incapacitation" }
    };

    [Theory]
    [MemberData(nameof(WoundsSteps))]
    public void WoundsSkillStepsAlongDoggedNwiVita(string vitality, IReadOnlyList<string> skills, int steps, string expectedVitality, string? expectedSkill)
    {
        var changed = Fusilier(vitality, skills).With(new TrooperChange { WoundsSkillSteps = steps });

        Assert.Equal(expectedVitality, changed.Vitality);
        Assert.Equal(expectedSkill is null ? [] : [expectedSkill], changed.Skills);
    }

    [Fact]
    public void CharacteristicsAreGainedAndLost()
    {
        var changed = Fusilier().With(new TrooperChange
        {
            CharacteristicsGained = ["Frenzy", "Impetuous"],
            CharacteristicsLost = ["regular", "Impetuous"]
        });

        Assert.Equal(["Cube", "Frenzy"], changed.Characteristics);
    }

    [Fact]
    public void SkillsAreAddedOnce()
    {
        Assert.Equal(["Courage", "Climbing Plus"], Fusilier().With(new TrooperChange { SkillsGained = ["courage", "Climbing Plus"] }).Skills);
    }

    [Fact]
    public void SilhouetteIsReplaced()
    {
        Assert.Equal("7", Fusilier().With(new TrooperChange { Silhouette = 7 }).Silhouette);
    }
}
