using InfinityMercsApp.Services.Packaging;

namespace InfinityMercsApp.Services.Logos;

/// <inheritdoc/>
/// <remarks>Logos as files on the device, downloaded or copied from the app's package.</remarks>
public sealed class LogoFileStore(HttpClient httpClient, IAppPackageFiles packageFiles) : ILogoStore
{
    /// <inheritdoc/>
    public bool Has(string localPath) => File.Exists(localPath) && new FileInfo(localPath).Length > 0;

    /// <inheritdoc/>
    public async Task<bool> FetchAsync(string url, string localPath, CancellationToken cancellationToken)
    {
        try
        {
            CreateDirectoryOf(localPath);
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                Console.Error.WriteLine($"SVG download failed for '{url}': HTTP {(int)response.StatusCode}.");
                return false;
            }

            // Written aside and moved into place, so an interrupted download never leaves a part-file that looks cached.
            var partPath = localPath + ".part";
            await using (var remoteStream = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var fileStream = File.Create(partPath))
            {
                await remoteStream.CopyToAsync(fileStream, cancellationToken);
            }

            File.Move(partPath, localPath, overwrite: true);
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"SVG download failed for '{url}': {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> CopyPackagedAsync(string packagedPath, string localPath, CancellationToken cancellationToken)
    {
        try
        {
            CreateDirectoryOf(localPath);
            await using var packageStream = await packageFiles.OpenAsync(packagedPath);
            await using var fileStream = File.Create(localPath);
            await packageStream.CopyToAsync(fileStream, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Packaged asset copy failed for '{packagedPath}': {ex.Message}");
            return false;
        }
    }

    private static void CreateDirectoryOf(string localPath)
    {
        if (Path.GetDirectoryName(localPath) is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
        }
    }
}
