using InfinityMercsApp.Services.Records;

namespace InfinityMercsApp.UnitTests.Services.Records;

/// <summary>The Windows data folder moved with the publisher name; what was in the old one is copied across once.</summary>
public sealed class DataFolderMoveTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"datafoldermove-{Guid.NewGuid():N}");

    private string Old => Path.Combine(_root, "User Name", "app", "Data");

    private string New => Path.Combine(_root, "infinitymercsteam", "app", "Data");

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private void Write(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? _root);
        File.WriteAllText(path, text);
    }

    [Fact]
    public void CopyIfNew_OldFolderWithData_CopiesIntoTheNewOne()
    {
        Write(Path.Combine(Old, "infinitymercs.db3"), "db");
        Write(Path.Combine(Old, "Companies", "blue.json"), "company");

        Assert.True(DataFolderMove.CopyIfNew(Old, New));

        Assert.Equal("db", File.ReadAllText(Path.Combine(New, "infinitymercs.db3")));
        Assert.Equal("company", File.ReadAllText(Path.Combine(New, "Companies", "blue.json")));
        Assert.True(File.Exists(Path.Combine(Old, "infinitymercs.db3")));
    }

    [Fact]
    public void CopyIfNew_NewFolderHasData_NeverOverwritesIt()
    {
        Write(Path.Combine(Old, "infinitymercs.db3"), "old");
        Write(Path.Combine(New, "infinitymercs.db3"), "new");

        Assert.False(DataFolderMove.CopyIfNew(Old, New));
        Assert.Equal("new", File.ReadAllText(Path.Combine(New, "infinitymercs.db3")));
    }

    [Fact]
    public void CopyIfNew_NoOldFolder_DoesNothing()
    {
        Assert.False(DataFolderMove.CopyIfNew(Old, New));
        Assert.False(Directory.Exists(New));
    }
}
