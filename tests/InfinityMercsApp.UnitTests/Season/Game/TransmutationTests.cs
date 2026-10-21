using InfinityMercsApp.Domain.Season.Game;

namespace InfinityMercsApp.UnitTests.Season.Game;

public sealed class TransmutationTests
{
    [Theory]
    [InlineData("Transmutation (1)", 1)]
    [InlineData(" Transmutation(2) ", 2)]
    [InlineData("Transmutation (Auto)", null)]
    [InlineData("Transmutation (WIP)", null)]
    [InlineData("Transmutation (Escape System -2)", null)]
    [InlineData(null, null)]
    public void Threshold_Skill_ReadsX(string? skill, int? expected) => Assert.Equal(expected, Transmutation.Threshold(skill));

    [Theory]
    [InlineData(1, 2, 2, 1)]
    [InlineData(1, 2, 3, 2)]
    [InlineData(2, 3, 2, 2)]
    public void Raised_Vitality_AddsAWoundForEachFullVitaGained(int threshold, int profileVitality, int vitality, int expected) =>
        Assert.Equal(expected, Transmutation.Raised(threshold, profileVitality, vitality));

    [Theory]
    [InlineData("Symbio - Resonance", true)]
    [InlineData("Symbio-Resonance", true)]
    [InlineData("Ablative Armor", false)]
    [InlineData(null, false)]
    public void IsSymbioResonance_ArmorName_MatchesItsSpellings(string? armorName, bool expected) =>
        Assert.Equal(expected, Transmutation.IsSymbioResonance(armorName));
}
