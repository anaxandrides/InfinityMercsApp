using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Services.CompanySelection.Units;

namespace InfinityMercsApp.UnitTests.Services.CompanySelection.Units;

public sealed class UnitClassificationTests
{
    private static readonly Dictionary<int, string> Types = new() { [1] = "LI" };
    private static readonly Dictionary<int, string> Categories = new() { [3] = "Elite Troops", [7] = "Character (Merc)" };

    [Fact]
    public void Subtitle_KnownTypeAndCategory_NamesBoth() =>
        Assert.Equal("LI - Elite Troops", UnitClassification.Subtitle(new Resume { Type = 1, Category = 3 }, Types, Categories));

    [Fact]
    public void Subtitle_UnknownIds_FallsBackToIdsOrQuestionMarks() =>
        Assert.Equal("9 - ?", UnitClassification.Subtitle(new Resume { Type = 9 }, Types, Categories));

    [Theory]
    [InlineData(10, true)]
    [InlineData(7, true)]
    [InlineData(3, false)]
    [InlineData(null, false)]
    public void IsCharacter_Category_TrueForTenOrACharacterName(int? category, bool expected) =>
        Assert.Equal(expected, UnitClassification.IsCharacter(new Resume { Category = category }, Categories));
}
