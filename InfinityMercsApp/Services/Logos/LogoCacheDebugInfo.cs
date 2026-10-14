namespace InfinityMercsApp.Services.Logos;

public class LogoCacheDebugInfo
{
    public int FactionId { get; set; }

    public string? ExpectedLogoUrl { get; set; }

    public string LocalPath { get; set; } = string.Empty;

    public bool Exists { get; set; }

    public long SizeBytes { get; set; }
}
