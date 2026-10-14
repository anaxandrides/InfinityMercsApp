using System.Globalization;

namespace InfinityMercsApp.Domain.Models.Army.Profiles;

/// <summary>
/// An army's <c>fireteamChart</c>: how many fireteams of each kind it may field and which teams exist.
/// </summary>
public sealed class FireteamChart
{
    public static FireteamChart Empty { get; } = new();

    /// <summary>The <c>spec</c> entries in payload order, e.g. <c>("CORE", 1)</c>; the value is null when not a number.</summary>
    public IReadOnlyList<(string Key, int? Value)> Spec { get; init; } = [];

    public IReadOnlyList<FireteamTeam> Teams { get; init; } = [];

    /// <summary>
    /// A spec count as shown in the viewer: the first entry whose key matches (ignoring case),
    /// <c>"T"</c> above 5 (unlimited), <c>"-"</c> when absent or not a number.
    /// </summary>
    public string SpecDisplay(string key)
    {
        foreach (var (entryKey, value) in Spec)
        {
            if (string.Equals(entryKey, key, StringComparison.OrdinalIgnoreCase))
            {
                return value is int count
                    ? count > 5 ? "T" : count.ToString(CultureInfo.InvariantCulture)
                    : "-";
            }
        }

        return "-";
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
    public (int Duo, int Haris, int Core) TypeCounts
    {
        get
        {
            int duo = 0, haris = 0, core = 0;
            foreach (var type in TypeList ?? [])
            {
                if (type.Equals("DUO", StringComparison.OrdinalIgnoreCase))
                {
                    duo++;
                }
                else if (type.Equals("HARIS", StringComparison.OrdinalIgnoreCase))
                {
                    haris++;
                }
                else if (type.Equals("CORE", StringComparison.OrdinalIgnoreCase))
                {
                    core++;
                }
            }

            return (duo, haris, core);
        }
    }

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
