namespace InfinityMercsApp.Services.Records;

/// <summary>A saved company as listed for loading.</summary>
/// <param name="FactionIconPaths">Logos of up to two source factions.</param>
public sealed record CompanyRecord(
    string FilePath,
    string CompanyName,
    string CompanyType,
    IReadOnlyList<string> FactionIconPaths,
    DateTimeOffset CreatedUtc)
{
    public string Subtitle => $"{CreatedUtc:yyyy-MM-dd HH:mm:ss} UTC";

    public string CompanyIconPath => CompanyType switch
    {
        "Cohesive Company" => "SVGCache/MercsIcons/noun-team-7662436.svg",
        "Inspiring Leader" => "SVGCache/MercsIcons/noun-leadership-7195245.svg",
        "Airborne Company" => "SVGCache/MercsIcons/noun-airborne-8005870.svg",
        "TAG Company" => "SVGCache/MercsIcons/noun-battle-mech-1731140.svg",
        "Proxy Pack" => "SVGCache/MercsIcons/noun-assassin-5981200.svg",
        _ => "SVGCache/MercsIcons/noun-hack-2277937.svg"
    };

    public string? PrimaryFactionIconPath => FactionIconPaths.ElementAtOrDefault(0);

    public string? SecondaryFactionIconPath => FactionIconPaths.ElementAtOrDefault(1);

    public bool HasSecondaryFactionIcon => SecondaryFactionIconPath is not null;
}
