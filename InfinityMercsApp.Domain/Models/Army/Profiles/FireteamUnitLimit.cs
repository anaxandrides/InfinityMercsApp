namespace InfinityMercsApp.Domain.Models.Army.Profiles;

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
