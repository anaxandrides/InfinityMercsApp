namespace InfinityMercsApp.Domain.Models.Army.Fireteams;

/// <summary>One fireteam of the chart (the payload's <c>teams[]</c>).</summary>
public sealed class FireteamTeam
{
    /// <summary>The name, or null when the payload has no string name.</summary>
    public string? Name { get; init; }

    /// <summary>The string entries of a <c>type</c> array (<c>["CORE","HARIS"]</c>); null when <c>type</c> is not an array.</summary>
    public IReadOnlyList<string>? TypeList { get; init; }

    /// <summary>The <c>type</c> when the payload sends a single string.</summary>
    public string? TypeText { get; init; }

    public IReadOnlyList<FireteamUnitLimit> Units { get; init; } = [];

    /// <summary>How many times DUO, HARIS and CORE appear in the type list.</summary>
    public FireteamTypeCounts TypeCounts => FireteamTypeCounts.Of(TypeList);

    /// <summary>The name for company team matching: trimmed, empty when missing or blank.</summary>
    public string CompanyName => string.IsNullOrWhiteSpace(Name) ? string.Empty : Name.Trim();

    /// <summary>The name in the unit viewer: as sent, "Unnamed Team" when missing.</summary>
    public string ViewerName => Name ?? "Unnamed Team";

    /// <summary>The types joined for display (<c>"CORE, HARIS"</c>), or the single type string.</summary>
    public string TypesDisplay => TypeList is not null
        ? string.Join(", ", TypeList.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()))
        : TypeText ?? string.Empty;
}
