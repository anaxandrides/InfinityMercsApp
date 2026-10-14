using System.Globalization;

namespace InfinityMercsApp.Domain.Models.Army.Profiles;

/// <summary>The kinds of fireteam an army can field.</summary>
public enum FireteamType
{
    Duo,
    Haris,
    Core
}

public static class FireteamTypes
{
    /// <summary>The type as the API writes it: "DUO", "HARIS" or "CORE".</summary>
    public static string Key(this FireteamType type) => type.ToString().ToUpperInvariant();

    /// <summary>Reads "DUO", "haris" and so on, ignoring case only.</summary>
    public static bool TryParse(string? key, out FireteamType type)
    {
        foreach (var candidate in Enum.GetValues<FireteamType>())
        {
            if (string.Equals(candidate.Key(), key, StringComparison.OrdinalIgnoreCase))
            {
                type = candidate;
                return true;
            }
        }

        type = default;
        return false;
    }
}

/// <summary>One entry of a chart's <c>spec</c>, e.g. CORE 1; the count is null when not a number.</summary>
public sealed record FireteamSpecEntry(string Key, int? Count);

/// <summary>How many times each fireteam type appears.</summary>
public sealed record FireteamTypeCounts(int Duo, int Haris, int Core)
{
    public static FireteamTypeCounts Of(IEnumerable<string>? types)
    {
        var parsed = (types ?? []).Select(type => FireteamTypes.TryParse(type, out var parsedType) ? parsedType : (FireteamType?)null).ToList();
        return new FireteamTypeCounts(
            parsed.Count(type => type == FireteamType.Duo),
            parsed.Count(type => type == FireteamType.Haris),
            parsed.Count(type => type == FireteamType.Core));
    }
}

/// <summary>
/// An army's <c>fireteamChart</c>: how many fireteams of each kind it may field and which teams exist.
/// </summary>
public sealed class FireteamChart
{
    public static FireteamChart Empty { get; } = new();

    /// <summary>The <c>spec</c> entries in payload order.</summary>
    public IReadOnlyList<FireteamSpecEntry> Spec { get; init; } = [];

    public IReadOnlyList<FireteamTeam> Teams { get; init; } = [];

    /// <summary>
    /// A spec count as shown in the viewer: the first entry whose key matches (ignoring case),
    /// <c>"T"</c> above 5 (unlimited), <c>"-"</c> when absent or not a number.
    /// </summary>
    public string SpecDisplay(FireteamType type)
    {
        var entry = Spec.FirstOrDefault(entry => string.Equals(entry.Key, type.Key(), StringComparison.OrdinalIgnoreCase));
        return entry?.Count is int count
            ? count > 5 ? "T" : count.ToString(CultureInfo.InvariantCulture)
            : "-";
    }
}

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

/// <summary>One unit slot of a fireteam (the payload's <c>teams[].units[]</c>).</summary>
public sealed class FireteamUnitLimit
{
    public string? Name { get; init; }

    public string? Slug { get; init; }

    public string? Comment { get; init; }

    /// <summary>Minimum count (a number or numeric string); null otherwise, e.g. for <c>"*"</c>.</summary>
    public int? Min { get; init; }

    public int? Max { get; init; }

    /// <summary>True when the payload marks the minimum as <c>"*"</c> (at least one required).</summary>
    public bool MinIsAsterisk { get; init; }

    /// <summary>The <c>required</c> flag (true/false or "true"/"false"); null when absent.</summary>
    public bool? Required { get; init; }

    /// <summary>True when the team must include this unit.</summary>
    public bool IsMandatory => MinIsAsterisk || Required == true;

    /// <summary>
    /// The slot label used for company team matching: the trimmed name (or the slug, or "Unknown")
    /// followed by the trimmed comment when there is one.
    /// </summary>
    public string CompanyDisplayName
    {
        get
        {
            var name = string.IsNullOrWhiteSpace(Name) ? string.Empty : Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                name = string.IsNullOrWhiteSpace(Slug) ? "Unknown" : Slug;
            }

            var comment = string.IsNullOrWhiteSpace(Comment) ? string.Empty : Comment.Trim();
            return string.IsNullOrWhiteSpace(comment) ? name : $"{name} {comment}".Trim();
        }
    }

    /// <summary>The trimmed slug for company team matching, or null when missing or blank.</summary>
    public string? CompanySlug => string.IsNullOrWhiteSpace(Slug) ? null : Slug.Trim();

    /// <summary>The slot name in the unit viewer: the name, else the slug, else "Unknown".</summary>
    public string ViewerName => string.IsNullOrWhiteSpace(Name) ? Slug ?? "Unknown" : Name;
}
