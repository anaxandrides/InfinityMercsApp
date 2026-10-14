namespace InfinityMercsApp.Services.Packaging;

internal sealed class AppPackageFiles : IAppPackageFiles
{
    public async Task<string> ReadTextAsync(string path)
    {
        await using var stream = await FileSystem.Current.OpenAppPackageFileAsync(path);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
