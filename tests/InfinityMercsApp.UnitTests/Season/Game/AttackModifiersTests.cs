using InfinityMercsApp.Domain.Season.Game;

namespace InfinityMercsApp.UnitTests.Season.Game;

public sealed class AttackModifiersTests
{
    private static string Lines(params IEnumerable<string> lines) => string.Join(Environment.NewLine, lines);

    [Fact]
    public void NothingMeansNoModifiers() => Assert.Equal(AttackModifiers.None, AttackModifiers.From(null, null));

    [Fact]
    public void TheLegacyBsAttackSpellingIsRewritten() =>
        Assert.Equal("Courage\nBS Attack (SR-1)", AttackModifiers.NormalizeSkills("Courage\nBS Attack (-1 SR)"));

    [Fact]
    public void EachModifierIsReadFromItsSkill()
    {
        var modifiers = AttackModifiers.From(
            Lines("BS Attack (SR-2)", "BS Attack (Shock)", "BS Attack (+1 B)", "CC Attack (SR-3)"),
            "X-Visor");

        Assert.Equal(new AttackModifiers(true, 2, "Shock", 1, 3), modifiers);
    }

    [Fact]
    public void TheLegacyDamageReductionIsStillRead() =>
        Assert.Equal(1, AttackModifiers.From("BS Attack (-1 SR)", null).BsDamageReduction);

    [Theory]
    [InlineData("BS Attack (SR-1)")]
    [InlineData("BS Attack (+2 B)")]
    public void DamageAndBurstFormsAreNotAmmo(string skill) =>
        Assert.Null(AttackModifiers.From(skill, null).BsAmmoModifier);

    [Fact]
    public void AnXVisorInTheSkillsCounts() => Assert.True(AttackModifiers.From("X Visor", null).HasXVisor);
}
