using InfinityMercsApp.Domain.Season.Game;

namespace InfinityMercsApp.UnitTests.Season.Game;

public sealed class AttackModifiersTests
{
    private static string Lines(params IEnumerable<string> lines) => string.Join(Environment.NewLine, lines);

    [Fact]
    public void From_NoSkills_ReturnsNone() => Assert.Equal(AttackModifiers.None, AttackModifiers.From(null, null));

    [Fact]
    public void NormalizeSkills_LegacyBsAttackSpelling_RewritesIt() =>
        Assert.Equal("Courage\nBS Attack (SR-1)", AttackModifiers.NormalizeSkills("Courage\nBS Attack (-1 SR)"));

    [Fact]
    public void From_ModifierSkills_ReadsEachOne()
    {
        var modifiers = AttackModifiers.From(
            Lines("BS Attack (SR-2)", "BS Attack (Shock)", "BS Attack (+1 B)", "CC Attack (SR-3)"),
            "X-Visor");

        Assert.Equal(new AttackModifiers(true, 2, "Shock", 1, 3), modifiers);
    }

    [Fact]
    public void From_LegacyDamageReduction_StillReadsIt() =>
        Assert.Equal(1, AttackModifiers.From("BS Attack (-1 SR)", null).BsDamageReduction);

    [Theory]
    [InlineData("BS Attack (SR-1)")]
    [InlineData("BS Attack (+2 B)")]
    public void From_DamageOrBurstForm_IsNotAmmo(string skill) =>
        Assert.Null(AttackModifiers.From(skill, null).BsAmmoModifier);

    [Fact]
    public void From_XVisorInSkills_HasXVisor() => Assert.True(AttackModifiers.From("X Visor", null).HasXVisor);
}
