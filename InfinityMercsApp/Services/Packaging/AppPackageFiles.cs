namespace InfinityMercsApp.Services.Packaging;

internal sealed class AppPackageFiles : IAppPackageFiles
{
    public async Task<string> ReadTextAsync(string path)
    {
        await using var stream = await FileSystem.Current.OpenAppPackageFileAsync(path);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    public Task<Stream> OpenAsync(string path) => FileSystem.Current.OpenAppPackageFileAsync(path);

    public Task<bool> ExistsAsync(string path) => FileSystem.Current.AppPackageFileExistsAsync(path);
}
