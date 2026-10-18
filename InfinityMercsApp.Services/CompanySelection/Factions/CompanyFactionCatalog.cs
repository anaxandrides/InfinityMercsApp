using System.Text.RegularExpressions;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.Logos;
using InfinityMercsApp.Services.Navigation;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;

namespace InfinityMercsApp.Services.CompanySelection.Factions;

/// <summary>The factions a company selection screen offers, from the army metadata.</summary>
public sealed class CompanyFactionCatalog(IArmyDataService armyData, FactionLogoCacheService? logos = null)
{
    /// <summary>
    /// Every vanilla faction or every sectorial (by <paramref name="mode"/>), without the Non-Aligned Armies and
    /// with one Contracted Back-Up, by name; their logos are cached on the way.
    /// </summary>
    public async Task<List<FactionRecord>> LoadAsync(ArmySourceSelectionMode mode, CancellationToken cancellationToken = default)
    {
        var factions = armyData
            .GetMetadataFactions(includeDiscontinued: true, cancellationToken)
            .ToList();

        if (logos is not null)
        {
            await logos.CacheFactionLogosFromRecordsAsync(factions, cancellationToken);
        }

        IEnumerable<FactionRecord> filtered = factions;
        if (mode == ArmySourceSelectionMode.VanillaFactions)
        {
            filtered = filtered.Where(x => x.Id == x.ParentId);
        }
        else
        {
            filtered = filtered.Where(x => x.Id != x.ParentId);
        }

        filtered = filtered.Where(x => !IsNonAlignedArmyName(x.Name));
        filtered = CollapseContractedBackUpVariants(filtered);
        return filtered.OrderBy(x => x.Name).ToList();
    }

    public static IEnumerable<FactionRecord> CollapseContractedBackUpVariants(IEnumerable<FactionRecord> factions)
    {
        var list = factions.ToList();
        var contracted = list
            .Where(x => IsContractedBackUpName(x.Name))
            .ToList();

        if (contracted.Count <= 1)
        {
            return list;
        }

        var preferred = contracted.FirstOrDefault(x => !LooksAllCaps(x.Name))
            ?? contracted.First();

        return list.Where(x => !IsContractedBackUpName(x.Name) || x.Id == preferred.Id);
    }

    public static bool IsNonAlignedArmyName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return name.Trim().Equals("Non-Aligned Armies", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsContractedBackUpName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var normalized = Regex.Replace(name.Trim(), @"[\s\-]+", " ").ToLowerInvariant();
        return normalized == "contracted back up";
    }

    private static bool LooksAllCaps(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var letters = value.Where(char.IsLetter).ToList();
        if (letters.Count == 0)
        {
            return false;
        }

        return letters.All(char.IsUpper);
    }
}
