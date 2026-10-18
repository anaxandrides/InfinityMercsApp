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
}
