using System.Collections.Concurrent;
using System.IO.Compression;

namespace InfinityMercsApp.Api.ArmyData.Logos;

/// <summary>
/// Packs the army data's logos into one zip, so the app downloads one file on its first launch instead of thousands.
/// The bundle is named by the hash of its logos and kept, so it is built again only when a logo is added, removed or
/// moves to another address.
/// </summary>
public sealed class LogoBundler(ILogoSource source, ILogoBundleStore store)
{
    private const int ConcurrentDownloads = 16;

    /// <summary>
    /// Builds and keeps the bundle for <paramref name="logos"/> under <paramref name="hash"/>. A logo that can't be
    /// downloaded is left out; the app downloads it on its own.
    /// </summary>
    /// <returns>How many logos the bundle holds.</returns>
    public async Task<int> BuildAsync(string hash, IReadOnlyList<LogoFile> logos, CancellationToken cancellationToken = default)
    {
        var downloaded = new ConcurrentDictionary<string, byte[]>(StringComparer.Ordinal);
        var options = new ParallelOptions { MaxDegreeOfParallelism = ConcurrentDownloads, CancellationToken = cancellationToken };
        await Parallel.ForEachAsync(logos, options, async (logo, token) =>
        {
            if (await source.GetAsync(logo.Url, token) is { Length: > 0 } bytes)
            {
                downloaded[logo.Path] = bytes;
            }
        });

        using var zip = new MemoryStream();
        using (var archive = new ZipArchive(zip, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (path, bytes) in downloaded.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                await using var entry = await archive.CreateEntry(path, CompressionLevel.SmallestSize).OpenAsync(cancellationToken);
                await entry.WriteAsync(bytes, cancellationToken);
            }
        }

        zip.Position = 0;
        await store.SaveAsync(hash, zip, cancellationToken);
        return downloaded.Count;
    }
}
