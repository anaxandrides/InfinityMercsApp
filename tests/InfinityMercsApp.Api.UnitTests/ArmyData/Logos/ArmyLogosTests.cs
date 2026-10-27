using InfinityMercsApp.Api.ArmyData.Logos;

namespace InfinityMercsApp.Api.UnitTests.ArmyData.Logos;

public sealed class ArmyLogosTests
{
    [Fact]
    public void OfMetadata_Factions_NamesEachLogoByFaction() =>
        Assert.Equal(
            [new LogoFile("101.svg", "https://cdn.example/101.svg")],
            ArmyLogos.OfMetadata("""{"factions":[{"id":101,"logo":"https://cdn.example/101.svg"},{"id":102,"logo":"SVGCache/factions/102.svg"},{"id":103}]}"""));

    [Fact]
    public void OfArmy_Resume_NamesEachLogoByFactionAndUnitOnce() =>
        Assert.Equal(
            [new LogoFile("units/101-7.svg", "https://cdn.example/7.svg")],
            ArmyLogos.OfArmy(101, """{"resume":[{"id":7,"logo":"https://cdn.example/7.svg"},{"id":"7","logo":"https://cdn.example/7b.svg"},{"id":0,"logo":"https://cdn.example/0.svg"}]}"""));

    [Fact]
    public void Hash_SameLogosInAnotherOrder_IsTheSame() =>
        Assert.Equal(
            ArmyLogos.Hash([new LogoFile("a.svg", "https://x/a"), new LogoFile("b.svg", "https://x/b")]),
            ArmyLogos.Hash([new LogoFile("b.svg", "https://x/b"), new LogoFile("a.svg", "https://x/a")]));

    [Fact]
    public void Hash_LogoMoved_Changes() =>
        Assert.NotEqual(
            ArmyLogos.Hash([new LogoFile("a.svg", "https://x/a")]),
            ArmyLogos.Hash([new LogoFile("a.svg", "https://x/a2")]));

    [Fact]
    public void OfMetadata_UnusableIdsOrLinks_AreLeftOut() =>
        Assert.Equal(
            [new LogoFile("104.svg", "http://cdn.example/104.svg")],
            ArmyLogos.OfMetadata("""{"factions":[7,{"id":true,"logo":"https://cdn.example/x.svg"},{"id":"x","logo":"https://cdn.example/y.svg"},{"id":105,"logo":5},{"id":"104","logo":"http://cdn.example/104.svg"}]}"""));
}
