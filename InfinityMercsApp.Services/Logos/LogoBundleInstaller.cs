using System.IO.Compression;
using InfinityMercsApp.Infrastructure.API.ArmyMirror;
using InfinityMercsApp.Services.Records;

namespace InfinityMercsApp.Services.Logos;

/// <summary>
/// Fills the logo cache from the API's logo bundle: one zip of every faction's and unit's logo, instead of thousands of
/// downloads on a first launch. A marker file in the cache says which bundle it holds, so a bundle is fetched only when
/// the manifest names a new one, and again if the cache is cleared. Any logo the bundle lacks is downloaded on its own
/// afterwards, by <see cref="FactionLogoCacheService"/>.
/// </summary>
public sealed class LogoBundleInstaller(IArmyMirror mirror, RecordPaths paths) : ILogoBundleInstaller
{
    public const string MarkerFileName = ".logo-bundle";

    private string CacheDirectory => paths.FactionLogoDirectory;

    /// <summary>Installs the current bundle when the cache doesn't hold it yet; true when it did.</summary>
    public async Task<bool> InstallAsync(CancellationToken cancellationToken = default)
    {
        if (await mirror.GetManifestAsync(cancellationToken) is not { Logos.Hash: { Length: > 0 } hash } || InstalledHash() == hash)
        {
            return false;
        }

        var zipPath = Path.Combine(CacheDirectory, "logos.zip.part");
        try
        {
            Directory.CreateDirectory(CacheDirectory);
            if (!await mirror.DownloadLogoBundleAsync(zipPath, cancellationToken))
            {
                return false;
            }

            await using (var archive = await ZipFile.OpenReadAsync(zipPath, cancellationToken))
            {
                foreach (var entry in archive.Entries)
                {
                    if (TargetOf(entry.FullName) is { } target)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(target) ?? CacheDirectory);
                        await entry.ExtractToFileAsync(target, overwrite: true, cancellationToken);
                    }
                }
            }

            await File.WriteAllTextAsync(Path.Combine(CacheDirectory, MarkerFileName), hash, cancellationToken);
            return true;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"The logo bundle couldn't be installed: {ex.Message}");
            return false;
        }
        finally
        {
            TryDelete(zipPath);
        }
    }

    // Where an entry goes in the cache: only an .svg, and only inside the cache, whatever path the zip gives it.
    private string? TargetOf(string entryName)
    {
        if (!entryName.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) || Path.IsPathRooted(entryName))
        {
            return null;
        }

        var root = Path.GetFullPath(CacheDirectory) + Path.DirectorySeparatorChar;
        var target = Path.GetFullPath(Path.Combine(root, entryName));
        return target.StartsWith(root, StringComparison.Ordinal) ? target : null;
    }

    private string? InstalledHash()
    {
        var marker = Path.Combine(CacheDirectory, MarkerFileName);
        try
        {
            return File.Exists(marker) ? File.ReadAllText(marker).Trim() : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"'{path}' couldn't be deleted: {ex.Message}");
        }
    }
}
