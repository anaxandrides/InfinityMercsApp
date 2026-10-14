using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Services.ArmyData;

namespace InfinityMercsApp.Views.Common;

/// <summary>
/// The reverse of <see cref="SavedCodeNames"/>: finds the filter ids for display names such as
/// "Combi Rifle (+1B)" so a company can be saved as codes. Name lookups are built once per faction and kind.
/// </summary>
public sealed partial class SavedCodeEncoder(IArmyDataService armyData)
{
    private readonly Dictionary<(int FactionId, string Section), Dictionary<string, int>> _idsByName = [];

    /// <summary>
    /// Codes for each name, looked up in the factions in order. A name with bracketed extras is coded as its
    /// base item plus the extras; when the base or any extra has no code, the name is also kept as custom text.
    /// </summary>
    public CodedNames Encode(IEnumerable<string> names, ProfileEntryKind kind, IEnumerable<int> factionIds)
    {
        var factions = factionIds.Where(id => id > 0).Distinct().ToList();
        var section = ArmyFilterSection.For(kind);
        var codes = new List<CompanySavedCodeRef>();
        var seenCodes = new HashSet<string>(StringComparer.Ordinal);
        var custom = new List<string>();
        var seenCustom = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddCode(CompanySavedCodeRef code)
        {
            if (seenCodes.Add(CodeKey(code)))
            {
                codes.Add(code);
            }
        }

        foreach (var name in names.Where(name => !string.IsNullOrWhiteSpace(name)).Select(name => name.Trim()))
        {
            if (FindId(section, NormalizeName(name), factions) is int id)
            {
                AddCode(new CompanySavedCodeRef { Id = id });
                continue;
            }

            var fullyCoded = false;
            if (SplitExtras(name) is { } split && FindId(section, NormalizeName(split.BaseName), factions) is int baseId)
            {
                var extraIds = split.ExtraNames.Select(extra => FindExtraId(extra, factions)).ToList();
                var knownExtraIds = extraIds.OfType<int>().Distinct().Order().ToList();
                AddCode(new CompanySavedCodeRef { Id = baseId, Extra = knownExtraIds.Count == 0 ? null : knownExtraIds });
                fullyCoded = extraIds.All(extraId => extraId.HasValue);
            }

            if (!fullyCoded && seenCustom.Add(name))
            {
                custom.Add(name);
            }
        }

        return new CodedNames(codes, custom);
    }

    private int? FindId(string section, string normalizedName, IReadOnlyList<int> factionIds)
    {
        foreach (var factionId in factionIds)
        {
            if (IdsByName(factionId, section).TryGetValue(normalizedName, out var id))
            {
                return id;
            }
        }

        return null;
    }

    // Extras may be written in either unit ("+1B" or a distance in inches or cm), so each spelling is tried.
    private int? FindExtraId(string extraName, IReadOnlyList<int> factionIds)
    {
        IEnumerable<string> candidates =
        [
            extraName,
            CompanySelectionSharedUtilities.ConvertDistanceText(extraName, showUnitsInInches: true),
            CompanySelectionSharedUtilities.ConvertDistanceText(extraName, showUnitsInInches: false)
        ];
        var spellings = candidates
            .Select(NormalizeName)
            .Where(spelling => spelling.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var factionId in factionIds)
        {
            var ids = IdsByName(factionId, ArmyFilterSection.Extras);
            foreach (var spelling in spellings)
            {
                if (ids.TryGetValue(spelling, out var id))
                {
                    return id;
                }
            }
        }

        return null;
    }

    private Dictionary<string, int> IdsByName(int factionId, string section)
    {
        if (_idsByName.TryGetValue((factionId, section), out var cached))
        {
            return cached;
        }

        var ids = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var names = (armyData.GetFactionSnapshot(factionId)?.Filters ?? ArmyFilterCatalog.Empty).Names(section);
        foreach (var (id, name) in names)
        {
            var normalized = NormalizeName(name);
            if (normalized.Length > 0)
            {
                ids.TryAdd(normalized, id);
            }
        }

        _idsByName[(factionId, section)] = ids;
        return ids;
    }

    /// <summary>"Combi Rifle (+1B, Shock)" as its base name and extras; null when the name has no bracketed extras.</summary>
    private static ExtrasSplit? SplitExtras(string value)
    {
        var openParenIndex = value.IndexOf(" (", StringComparison.Ordinal);
        if (openParenIndex <= 0 || !value.EndsWith(')'))
        {
            return null;
        }

        var baseName = value[..openParenIndex].Trim();
        var inside = value[(openParenIndex + 2)..^1].Trim();
        if (baseName.Length == 0 || inside.Length == 0)
        {
            return null;
        }

        var extras = SplitTopLevel(inside);
        return extras.Count == 0 ? null : new ExtrasSplit(baseName, extras);
    }

    // Splits on commas that are not inside nested brackets.
    private static List<string> SplitTopLevel(string value)
    {
        var result = new List<string>();
        var depth = 0;
        var tokenStart = 0;
        for (var i = 0; i <= value.Length; i++)
        {
            var atEnd = i == value.Length;
            var c = atEnd ? ',' : value[i];
            if (c == '(')
            {
                depth++;
            }
            else if (c == ')' && depth > 0)
            {
                depth--;
            }
            else if (c == ',' && (depth == 0 || atEnd))
            {
                var token = value[tokenStart..i].Trim();
                if (token.Length > 0)
                {
                    result.Add(token);
                }

                tokenStart = i + 1;
            }
        }

        return result;
    }

    private static string CodeKey(CompanySavedCodeRef code) =>
        code.Extra is not { Count: > 0 } ? $"{code.Id}|" : $"{code.Id}|{string.Join(",", code.Extra.Order())}";

    private static string NormalizeName(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : WhitespaceRun().Replace(value.Trim(), " ").ToLowerInvariant();

    private sealed record ExtrasSplit(string BaseName, IReadOnlyList<string> ExtraNames);

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRun();
}
