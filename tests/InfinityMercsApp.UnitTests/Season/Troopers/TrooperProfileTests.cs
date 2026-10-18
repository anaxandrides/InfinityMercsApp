using InfinityMercsApp.Domain.Season.Troopers;

namespace InfinityMercsApp.UnitTests.Season.Troopers;

public sealed class TrooperProfileTests
{
    private static TrooperProfile Fusilier(string vitality = "1", IReadOnlyList<string>? skills = null) =>
        new("10-5", "13", "12", "10", "13", "1", "0", vitality, "2", skills ?? ["Courage"], ["Regular", "Cube"]);

    [Fact]
    public void With_AttributeChange_ChangesNumbersKeepsDashes()
    {
        var profile = Fusilier() with { Bts = "-" };

        var changed = profile.With(new TrooperChange { Cc = -3, Wip = -1, Bts = 3 });

        Assert.Equal(("10", "12", "-"), (changed.Cc, changed.Wip, changed.Bts));
    }

    [Fact]
    public void With_MoveChange_ChangesInGameInches()
    {
        Assert.Equal("10cm-2.5cm", Fusilier().With(new TrooperChange { SecondMoveInches = -1 }).Move);
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
    public void With_WoundsSkillSteps_StepsAlongDoggedNwiVita(string vitality, IReadOnlyList<string> skills, int steps, string expectedVitality, string? expectedSkill)
    {
        var changed = Fusilier(vitality, skills).With(new TrooperChange { WoundsSkillSteps = steps });

        Assert.Equal(expectedVitality, changed.Vitality);
        Assert.Equal(expectedSkill is null ? [] : [expectedSkill], changed.Skills);
    }

    [Fact]
    public void With_CharacteristicChanges_GainsAndLosesThem()
    {
        var changed = Fusilier().With(new TrooperChange
        {
            CharacteristicsGained = ["Frenzy", "Impetuous"],
            CharacteristicsLost = ["regular", "Impetuous"]
        });

        Assert.Equal(["Cube", "Frenzy"], changed.Characteristics);
    }

    [Fact]
    public void With_SkillsGained_AddsEachOnce()
    {
        Assert.Equal(["Courage", "Climbing Plus"], Fusilier().With(new TrooperChange { SkillsGained = ["courage", "Climbing Plus"] }).Skills);
    }

    [Fact]
    public void With_Silhouette_ReplacesIt()
    {
        Assert.Equal("7", Fusilier().With(new TrooperChange { Silhouette = 7 }).Silhouette);
    }

    // Screens with a perk list show a perk's skill there; only one improving a held skill changes the skills.
    [Fact]
    public void With_PerkSkills_JoinSkillsOnlyWithoutAPerkList()
    {
        var change = new TrooperChange { PerkSkills = ["Climbing Plus"] };

        Assert.Contains("Climbing Plus", Fusilier().With(change).Skills);
        Assert.DoesNotContain("Climbing Plus", Fusilier().With(change with { ListsPerkSkills = false }).Skills);
    }

    [Fact]
    public void With_PerkSkillAlreadyHeld_AddsIntoIt()
    {
        var change = new TrooperChange { PerkSkills = ["Mimetism (-3)"], ListsPerkSkills = false };

        Assert.Equal(["Courage", "Mimetism (-6)"], Fusilier(skills: ["Courage", "Mimetism (-3)"]).With(change).Skills);
        Assert.Equal(["Dodge (+4)"], Fusilier(skills: ["Dodge (+1)"]).With(change with { PerkSkills = ["Dodge (+3)"] }).Skills);
    }
}
