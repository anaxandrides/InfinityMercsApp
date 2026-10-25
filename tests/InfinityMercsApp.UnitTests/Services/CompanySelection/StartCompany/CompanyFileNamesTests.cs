using InfinityMercsApp.Services.CompanySelection.StartCompany;

namespace InfinityMercsApp.UnitTests.Services.CompanySelection.StartCompany;

/// <summary>A new company's file is numbered one past every company of the same name already saved.</summary>
public sealed class CompanyFileNamesTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("company-names-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void NextCompanyFileName_NoCompanies_IsTheFirst() =>
        Assert.Equal("iron-wolves-0001.json", CompanyFounder.NextCompanyFileName(_folder, " Iron Wolves! "));

    [Fact]
    public void NextCompanyFileName_NoUsableLetters_IsACompany() =>
        Assert.Equal("company-0001.json", CompanyFounder.NextCompanyFileName(_folder, "!!!"));

    [Fact]
    public void NextCompanyFileName_SameNameSaved_FollowsItsIndex()
    {
        Save("anything.json", """{"CompanyName":"Iron Wolves","CompanyIndex":3}""");
        Save("other.json", """{"CompanyName":"Steel Wolves","CompanyIndex":9}""");

        Assert.Equal("iron-wolves-0004.json", CompanyFounder.NextCompanyFileName(_folder, "Iron Wolves"));
    }

    [Fact]
    public void NextCompanyFileName_SameNameWithoutAnIndex_FollowsItsFileName()
    {
        Save("iron-wolves-0005.json", """{"CompanyName":"Iron Wolves","CompanyIndex":"x"}""");

        Assert.Equal("iron-wolves-0006.json", CompanyFounder.NextCompanyFileName(_folder, "Iron Wolves"));
    }

    [Fact]
    public void NextCompanyFileName_UnreadableFile_CountsByItsName()
    {
        Save("iron-wolves-0007.json", "{ broken");
        Save("iron-wolves-notes.json", "{ broken");

        Assert.Equal("iron-wolves-0008.json", CompanyFounder.NextCompanyFileName(_folder, "Iron Wolves"));
    }

    [Fact]
    public void NextCompanyFileName_FolderMissing_CreatesIt()
    {
        var folder = Path.Combine(_folder, "companies");

        Assert.Equal("iron-wolves-0001.json", CompanyFounder.NextCompanyFileName(folder, "Iron Wolves"));
        Assert.True(Directory.Exists(folder));
    }

    private void Save(string fileName, string json) => File.WriteAllText(Path.Combine(_folder, fileName), json);
}
