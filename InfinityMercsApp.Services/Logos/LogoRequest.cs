namespace InfinityMercsApp.Services.Logos;

/// <summary>One logo to have on disk: where it goes, where it comes from, and the bundled copy if the download fails.</summary>
/// <param name="Url">An http(s) address, or a path to a logo bundled with the app ("SVGCache/…").</param>
/// <param name="BundledFallbackPath">A bundled copy to use when the download fails; null when there is none.</param>
/// <param name="AlwaysDownload">Downloads it even when a copy is on disk, for a logo that changes.</param>
internal sealed record LogoRequest(string LocalPath, string? Url, string? BundledFallbackPath, bool AlwaysDownload);
