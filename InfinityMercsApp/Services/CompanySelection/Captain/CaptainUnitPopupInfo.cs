namespace InfinityMercsApp.Views.Common.Captain;

public sealed class CaptainUnitPopupInfo
{
    public string Name { get; init; } = string.Empty;
    public int Cost { get; init; }
    public string Statline { get; init; } = "-";
    public string RangedWeapons { get; init; } = "-";
    public string CcWeapons { get; init; } = "-";
    public string Skills { get; init; } = "-";
    public string Equipment { get; init; } = "-";
    public string? CachedLogoPath { get; init; }
    public string? PackagedLogoPath { get; init; }
}
