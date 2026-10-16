namespace InfinityMercsApp.Services.Logos;

/// <summary>Chooses which logo file to show for a unit or faction.</summary>
public static class UnitLogo
{
    /// <summary>The downloaded logo when it exists on disk, otherwise the packaged one.</summary>
    public static string? Source(string? cachedPath, string? packagedPath) =>
        !string.IsNullOrWhiteSpace(cachedPath) && File.Exists(cachedPath) ? cachedPath : packagedPath;
}
