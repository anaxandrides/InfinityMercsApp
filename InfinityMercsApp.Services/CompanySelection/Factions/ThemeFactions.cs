using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.Navigation;

namespace InfinityMercsApp.Services.CompanySelection.Factions;

/// <summary>Which vanilla faction's colours a selection screen shows for a unit.</summary>
public sealed class ThemeFactions(IArmyDataService armyData)
{
    // A sectorial's parents are walked at most this far.
    private const int MaxParentSteps = 8;

    /// <summary>
    /// A sectorial screen shows its source faction's vanilla faction; a vanilla screen the first of the unit's
    /// factions that has colours, else the source faction's.
    /// </summary>
    public string? NameFor(ArmySourceSelectionMode mode, int sourceFactionId, IReadOnlyList<int>? unitFactionIds)
    {
        if (mode != ArmySourceSelectionMode.Sectorials)
        {
            foreach (var factionId in unitFactionIds ?? [])
            {
                if (VanillaName(factionId) is { } name && HasColours(name))
                {
                    return name;
                }
            }
        }

        return VanillaName(sourceFactionId);
    }

    /// <summary>
    /// The first faction with colours up the faction's parents; else the vanilla faction its id belongs to (101 →
    /// PanOceania), else the last faction reached.
    /// </summary>
    public string? VanillaName(int factionId)
    {
        if (factionId <= 0)
        {
            return null;
        }

        var current = armyData.GetMetadataFactionById(factionId);
        for (var steps = 0; current is not null && steps < MaxParentSteps; steps++)
        {
            if (HasColours(current.Name))
            {
                return current.Name;
            }

            if (current.ParentId <= 0 || armyData.GetMetadataFactionById(current.ParentId) is not { } parent || parent.Id == current.Id)
            {
                break;
            }

            current = parent;
        }

        return ByFactionId(factionId) ?? (current is null ? null : ByFactionId(current.Id) ?? current.Name);
    }

    /// <summary>
    /// A faction name as the colour themes know it: lower case, letters and digits only, reinforcements dropped,
    /// and the long names shortened ("Yu Jing" → "yujing", "Japanese Secessionist Army" → "jsa").
    /// </summary>
    public static string Key(string? factionName)
    {
        if (string.IsNullOrWhiteSpace(factionName))
        {
            return string.Empty;
        }

        var cleaned = factionName.Trim().ToLowerInvariant()
            .Replace("reinforcements", string.Empty, StringComparison.Ordinal)
            .Replace("reinforcement", string.Empty, StringComparison.Ordinal)
            .Trim();
        return cleaned switch
        {
            "yu jing" => "yujing",
            "combined army" => "combinedarmy",
            "non aligned army" or "non-aligned armies" or "non aligned armies" or "non-aligned army" => "nonalignedarmy",
            "japanese secessionist army" => "jsa",
            "o-12" => "o12",
            _ => string.Concat(cleaned.Where(char.IsLetterOrDigit))
        };
    }

    /// <summary>Whether the faction is one of the vanilla factions with its own colours.</summary>
    public static bool HasColours(string? factionName) =>
        Key(factionName) is "panoceania" or "yujing" or "ariadna" or "haqqislam" or "nomads" or "combinedarmy" or "aleph" or
            "tohaa" or "nonalignedarmy" or "o12" or "jsa";

    /// <summary>The vanilla faction a faction id belongs to by its hundreds (1xx PanOceania … 11xx JSA); null for any other.</summary>
    public static string? ByFactionId(int factionId) => factionId <= 0 ? null : (factionId / 100) switch
    {
        1 => "PanOceania",
        2 => "Yu Jing",
        3 => "Ariadna",
        4 => "Haqqislam",
        5 => "Nomads",
        6 => "Combined Army",
        7 => "Aleph",
        8 => "Tohaa",
        9 => "Non-Aligned Armies",
        10 => "O-12",
        11 => "JSA",
        _ => null
    };
}
