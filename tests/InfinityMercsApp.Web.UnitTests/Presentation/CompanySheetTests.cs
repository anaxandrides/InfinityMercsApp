using InfinityMercsApp.Web.Presentation;

namespace InfinityMercsApp.Web.UnitTests.Presentation;

public sealed class CompanySheetTests
{
    [Fact]
    public void From_Company_PutsTheCaptainFirstAndNamesEachRole()
    {
        var sheet = CompanySheet.From("c1", TestFiles.Company());

        Assert.Equal(["Bulleteer", "Rook", "Traktor Mul"], sheet.Troopers.Select(trooper => trooper.Name));
        Assert.Equal(["Captain", "", "Peripheral"], sheet.Troopers.Select(trooper => trooper.Role));
        Assert.Equal("PanOceania", sheet.Armies);
        Assert.Equal(75, sheet.StartingPoints);
    }

    [Fact]
    public void From_RenamedTrooper_ShowsItsNameAndItsUnit()
    {
        var rook = CompanySheet.From("c1", TestFiles.Company()).Troopers[1];

        Assert.Equal("Rook", rook.Name);
        Assert.Equal("Fusilier", rook.Unit);
    }

    [Fact]
    public void From_Trooper_ListsItsAttributesInOrder() =>
        Assert.StartsWith("MOV - · CC 15 · BS -", CompanySheet.From("c1", TestFiles.Company()).Troopers[0].Attributes);

    [Fact]
    public void CompanySummary_From_CountsTroopersWithoutPeripherals() =>
        Assert.Equal(2, CompanySummary.From("c1", TestFiles.Company()).Troopers);
}
