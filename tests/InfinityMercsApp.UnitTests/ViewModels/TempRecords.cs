using InfinityMercsApp.Services;

namespace InfinityMercsApp.UnitTests.ViewModels;

/// <summary>A throwaway MercenaryRecords folder.</summary>
internal sealed class TempRecords : IDisposable
{
    public TempRecords()
    {
        Paths = new RecordPaths(Path.Combine(Path.GetTempPath(), "mercs-tests-" + Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(Paths.SeasonsDirectory);
    }

    public RecordPaths Paths { get; }

    public string WriteCompany(string name, string json) => Write(Paths.CompaniesDirectory, name, json);

    public string WriteSeason(string name, string json) => Write(Paths.SeasonsDirectory, name, json);

    private static string Write(string directory, string name, string json)
    {
        var path = Path.Combine(directory, name);
        File.WriteAllText(path, json);
        return path;
    }

    public void Dispose() => Directory.Delete(Paths.CompaniesDirectory, recursive: true);
}
