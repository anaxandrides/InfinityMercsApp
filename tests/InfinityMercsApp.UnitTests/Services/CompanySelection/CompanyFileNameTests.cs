using InfinityMercsApp.UnitTests.Support;

namespace InfinityMercsApp.UnitTests.Services.CompanySelection;

public sealed class CompanyFileNameTests : IDisposable
{
    private readonly TempRecords _records = new();

    public void Dispose() => _records.Dispose();

    [Fact]
    public void NextCompanyFileNameSlugsTheNameAndTakesTheNextIndex()
    {
        var dir = _records.Paths.CompaniesDirectory;

        Assert.Equal("iron-wolves-0001.json", InfinityMercsApp.Views.Common.CompanyStartSaveWorkflow.NextCompanyFileName(dir, "  Iron Wolves! "));
        Assert.Equal("company-0001.json", InfinityMercsApp.Views.Common.CompanyStartSaveWorkflow.NextCompanyFileName(dir, "???"));
    }
}
