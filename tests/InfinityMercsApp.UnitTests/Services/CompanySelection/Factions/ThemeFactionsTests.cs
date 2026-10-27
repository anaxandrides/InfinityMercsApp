using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.Factions;
using InfinityMercsApp.Services.Navigation;
using NSubstitute;
using MetadataFaction = InfinityMercsApp.Domain.Models.Metadata.Faction;

namespace InfinityMercsApp.UnitTests.Services.CompanySelection.Factions;

public sealed class ThemeFactionsTests
{
    private readonly ThemeFactions _themes;

    public ThemeFactionsTests()
    {
        var armyData = Substitute.For<IArmyDataService>();
        armyData.GetMetadataFactionById(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(call => call.ArgAt<int>(0) switch
        {
            101 => new MetadataFaction { Id = 101, ParentId = 101, Name = "PanOceania" },
            102 => new MetadataFaction { Id = 102, ParentId = 101, Name = "Military Orders" },
            4242 => new MetadataFaction { Id = 4242, ParentId = 4242, Name = "Somebody's Company" },
            _ => null
        });
        _themes = new ThemeFactions(armyData);
    }

    [Fact]
    public void VanillaName_Sectorial_ReturnsItsVanillaFaction() =>
        Assert.Equal("PanOceania", _themes.VanillaName(102));

    [Fact]
    public void VanillaName_NotInMetadata_GoesByItsId() =>
        Assert.Equal("Tohaa", _themes.VanillaName(899));

    [Fact]
    public void VanillaName_NoTheme_KeepsItsOwnName() =>
        Assert.Equal("Somebody's Company", _themes.VanillaName(4242));

    [Fact]
    public void NameFor_VanillaScreen_PrefersTheUnitsFactionWithColours() =>
        Assert.Equal("PanOceania", _themes.NameFor(ArmySourceSelectionMode.VanillaFactions, 4242, [4242, 102]));

    [Theory]
    [InlineData("Yu Jing Reinforcements", "yujing")]
    [InlineData("Japanese Secessionist Army", "jsa")]
    [InlineData("O-12", "o12")]
    [InlineData("Military Orders", "militaryorders")]
    public void Key_FactionName_NormalisesIt(string name, string expected) =>
        Assert.Equal(expected, ThemeFactions.Key(name));

    [Theory]
    [InlineData("PanOceania", true)]
    [InlineData("Non-Aligned Armies", true)]
    [InlineData("Military Orders", false)]
    public void HasColours_FactionName_TrueForVanillaFactions(string name, bool expected) =>
        Assert.Equal(expected, ThemeFactions.HasColours(name));

    [Theory]
    [InlineData(0, null)]
    [InlineData(101, "PanOceania")]
    [InlineData(201, "Yu Jing")]
    [InlineData(301, "Ariadna")]
    [InlineData(401, "Haqqislam")]
    [InlineData(501, "Nomads")]
    [InlineData(601, "Combined Army")]
    [InlineData(701, "Aleph")]
    [InlineData(801, "Tohaa")]
    [InlineData(901, "Non-Aligned Armies")]
    [InlineData(1001, "O-12")]
    [InlineData(1101, "JSA")]
    [InlineData(2001, null)]
    public void ByFactionId_Hundreds_NameTheVanillaFaction(int factionId, string? expected) =>
        Assert.Equal(expected, ThemeFactions.ByFactionId(factionId));

    [Theory]
    [InlineData(null, "")]
    [InlineData("Combined Army", "combinedarmy")]
    [InlineData("Non Aligned Army", "nonalignedarmy")]
    [InlineData("PanOceania Reinforcement", "panoceania")]
    public void Key_OtherNames_NormaliseThem(string? name, string expected) => Assert.Equal(expected, ThemeFactions.Key(name));

    [Fact]
    public void VanillaName_NoFaction_IsNull() => Assert.Null(_themes.VanillaName(0));

    [Fact]
    public void NameFor_SectorialScreen_UsesTheSourceFaction() =>
        Assert.Equal("PanOceania", _themes.NameFor(ArmySourceSelectionMode.Sectorials, 102, [4242]));

    [Fact]
    public void NameFor_VanillaScreenNoUnitColours_UsesTheSourceFaction() =>
        Assert.Equal("Somebody's Company", _themes.NameFor(ArmySourceSelectionMode.VanillaFactions, 4242, null));
}
