namespace InfinityMercsApp.Services;

public class LogoCacheResult
{
    public int TotalFactions { get; set; }

    public int Downloaded { get; set; }

    public int Failed { get; set; }

    public int MissingLogoUrl { get; set; }

    public int InvalidLogoUrl { get; set; }

    public int CachedReuse { get; set; }
}
