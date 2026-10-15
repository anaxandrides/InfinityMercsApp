using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Models.Stores;

namespace InfinityMercsApp.Domain.Season.Marketplace;

/// <summary>
/// How stores are named in the season's store list: "Name [factions, type, alignment]" for markets, plain
/// names for services, and "Name - Round N Store" for a round's temporary store.
/// </summary>
public static partial class StoreLabels
{
    // Shortened faction names; sub-factions map to null and are left out.
    private const string MedicalBadge = "🩺";

    private static readonly Dictionary<string, string?> FactionAbbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PanOceania"] = "PanO",
        ["White Company"] = "White Co.",
        ["Yu Jing"] = "YJ",
        ["Nomads"] = "Nomads",
        ["Haqqislam"] = "Haqqislam",
        ["Druze"] = "Druze",
        ["Ariadna"] = "Ariadna",
        ["Dashat"] = "Dashat",
        ["Aleph"] = "ALEPH",
        ["O-12"] = "O12",
        ["Tohaa"] = "Tohaa",
        ["Combined Army"] = "CA",
        ["Japanese Secessionist Army"] = "JSA",
        ["Shindenbutai"] = null,
        ["Oban"] = null,
        ["Hayabusa Jōrikusentai"] = null,
        ["Ikari"] = null,
    };

    /// <summary>The store as listed: services by name (Medical Services with its badge), markets with their details in brackets.</summary>
    public static string For(Store store)
    {
        var name = store.Name;
        if (string.Equals(name, ServiceStoreNames.MedicalServices, StringComparison.OrdinalIgnoreCase))
        {
            return $"{MedicalBadge} {ServiceStoreNames.MedicalServices}";
        }

        if (string.Equals(name, ServiceStoreNames.AdditionalRecruitment, StringComparison.OrdinalIgnoreCase))
        {
            return ServiceStoreNames.AdditionalRecruitment;
        }

        if (store.AssociatedFactions.Count == 0 && store.IsNeutral)
        {
            return $"{name} [{StoreAlignments.Neutral}]";
        }

        List<string> parts = [];
        var factions = store.AssociatedFactions
            .Select(faction => FactionAbbreviations.TryGetValue(faction, out var abbreviation) ? abbreviation : faction)
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (factions.Count > 0)
        {
            parts.Add(string.Join("/", factions));
        }

        if (!string.IsNullOrWhiteSpace(store.AssociatedType))
        {
            parts.Add(store.AssociatedType.Replace(", ", " | "));
        }

        if (!string.IsNullOrWhiteSpace(store.Alignment) && !store.IsAlwaysAvailable)
        {
            parts.Add(store.Alignment);
        }

        return parts.Count > 0 ? $"{name} [{string.Join(", ", parts)}]" : name;
    }

    /// <summary>A round's temporary store, e.g. "Jade Temu - Round 2 Store".</summary>
    public static string ForTemporary(string storeName, int roundIndex) => $"{storeName} - Round {roundIndex} Store";

    /// <summary>
    /// The store's own name from a label or a saved origin: without a leading "+" or badge, a "- Round N Store"
    /// suffix or a bracketed suffix.
    /// </summary>
    public static string StoreName(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return string.Empty;
        }

        var name = label.Trim().TrimStart('+').Trim();
        if (name.StartsWith(MedicalBadge, StringComparison.Ordinal))
        {
            name = name[MedicalBadge.Length..].Trim();
        }

        var round = RoundSuffix().Match(name);
        if (round.Success)
        {
            name = name[..round.Index].Trim();
        }

        var bracket = name.LastIndexOf('[');
        if (bracket > 0 && name.EndsWith(']'))
        {
            name = name[..bracket].Trim();
        }

        return name;
    }

    [GeneratedRegex(@"\s*-\s*Round\s+\d+\s+Store\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex RoundSuffix();
}
