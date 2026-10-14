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
