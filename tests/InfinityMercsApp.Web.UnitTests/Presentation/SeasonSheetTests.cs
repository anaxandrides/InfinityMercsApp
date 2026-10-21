using InfinityMercsApp.Web.Presentation;

namespace InfinityMercsApp.Web.UnitTests.Presentation;

public sealed class SeasonSheetTests
{
    [Fact]
    public void From_Season_ListsContractsInOrderWithTheirResults()
    {
        var sheet = SeasonSheet.From("s1", TestFiles.Season());

        Assert.Equal(
            [new ContractRow(1, "Won", 7, 5, 7), new ContractRow(2, "Bye", 0, 0, 0)],
            sheet.Contracts);
        Assert.Equal(1, sheet.Wins);
        Assert.Equal(["Rook"], sheet.Retired);
        Assert.Equal("c1", sheet.CompanyId);
    }

    [Fact]
    public void SeasonSummary_From_CountsContractsAndWins()
    {
        var summary = SeasonSummary.From("s1", TestFiles.Season());

        Assert.Equal((2, 1), (summary.Contracts, summary.Wins));
    }
}
