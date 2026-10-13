namespace InfinityMercsApp.Services;

/// <summary>Reads files bundled with the app, injectable so tests can supply their own.</summary>
public interface IAppPackageFiles
{
    Task<string> ReadTextAsync(string path);
}

internal sealed class AppPackageFiles : IAppPackageFiles
{
    public async Task<string> ReadTextAsync(string path)
    {
        await using var stream = await FileSystem.Current.OpenAppPackageFileAsync(path);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
