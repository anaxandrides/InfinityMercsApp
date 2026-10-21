using InfinityMercsApp.Domain.Captain;

namespace InfinityMercsApp.UnitTests.Captain;

public sealed class CaptainChoiceLabelTests
{
    [Fact]
    public void Format_AnyCost_PrefixesCostAndNeverShowsNegative()
    {
        Assert.Equal("(3) - Sniper", CaptainChoiceLabel.Format(3, "Sniper"));
        Assert.Equal("(0) - Sniper", CaptainChoiceLabel.Format(-2, "Sniper"));
    }

    [Theory]
    [InlineData("(3) - Sniper", 3)]
    [InlineData("(None)", 0)]
    [InlineData("Sniper", 0)]
    [InlineData(null, 0)]
    public void Cost_Label_ReadsTheLeadingNumber(string? label, int expected) => Assert.Equal(expected, CaptainChoiceLabel.Cost(label));

    [Theory]
    [InlineData("(3) - Sniper", "Sniper")]
    [InlineData("(0) - Lt", "Lieutenant")]
    [InlineData("(None)", "")]
    [InlineData("Plain Name", "Plain Name")]
    public void Name_Label_StripsTheCost(string label, string expected) => Assert.Equal(expected, CaptainChoiceLabel.Name(label));
}
