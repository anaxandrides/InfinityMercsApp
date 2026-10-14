using System.Text.RegularExpressions;

namespace InfinityMercsApp.Domain.Models.Metadata;

/// <summary>Links to rules pages on the Infinity wiki.</summary>
public static partial class WikiLinks
{
    /// <summary>The wiki page for a rule, skill or weapon; "Mimetism (-6)" links to the Mimetism page.</summary>
    public static string ForName(string name) =>
        $"https://infinitythewiki.com/{BaseName(name).Replace(' ', '_')}?version=n4";

    /// <summary>The name before any bracketed level or modifier: "Combi Rifle (+1B)" is "Combi Rifle".</summary>
    public static string BaseName(string name) => BeforeBracket().Match(name).Value.Trim();

    [GeneratedRegex(@"^[^(]+")]
    private static partial Regex BeforeBracket();
}

/// <summary>One ability from a comma-separated list, with its wiki page when it names a known skill.</summary>
public sealed record AbilityLink(string Text, string? WikiUrl)
{
    public bool IsSkill => WikiUrl is not null;
}

/// <summary>Wiki pages for skills by name, built once from the skill metadata.</summary>
public sealed partial class SkillWikiLinks
{
    private readonly Dictionary<string, string> _urlBySkill;

    private SkillWikiLinks(Dictionary<string, string> urlBySkill) => _urlBySkill = urlBySkill;

    /// <summary>
    /// Each skill's own wiki link, or a link built from its name when it has none. The first skill with a
    /// name wins; names are matched ignoring case.
    /// </summary>
    public static SkillWikiLinks From(IEnumerable<Skill> skills)
    {
        var urlBySkill = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var skill in skills)
        {
            urlBySkill.TryAdd(skill.Name.Trim(), string.IsNullOrWhiteSpace(skill.Wiki) ? WikiLinks.ForName(skill.Name) : skill.Wiki);
        }

        return new SkillWikiLinks(urlBySkill);
    }

    /// <summary>
    /// The wiki page for an ability such as "Mimetism (-6)" or "Climbing Plus*", read by its name without
    /// the bracket or trailing marks; null when it is not a known skill.
    /// </summary>
    public string? For(string ability)
    {
        var name = TrailingMarks().Replace(WikiLinks.BaseName(ability), string.Empty).Trim();
        return _urlBySkill.GetValueOrDefault(name);
    }

    /// <summary>A comma-separated ability list as links, in order; known skills carry their wiki page.</summary>
    public IReadOnlyList<AbilityLink> Links(string? abilities) =>
        (abilities ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ability => new AbilityLink(ability, For(ability)))
            .ToList();

    [GeneratedRegex(@"\W+$")]
    private static partial Regex TrailingMarks();
}
