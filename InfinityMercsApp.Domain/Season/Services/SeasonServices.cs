using InfinityMercsApp.Domain.Models.Perks.Catalog;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Troopers;

namespace InfinityMercsApp.Domain.Season.Services;

/// <summary>What each service costs and does to a trooper's season record.</summary>
public static class SeasonServices
{
    public const string StoreName = "Services";
    public const string Irregular = "Irregular";
    public const string Regular = "Regular";
    public const string Impetuous = "Impetuous";
    public const string NoCover = "No Cover";
    public const string PartialCover = "Partial Cover";

    public static IReadOnlyList<SeasonService> All { get; } = Enum.GetValues<SeasonService>();

    public static int Cost(this SeasonService service) => service switch
    {
        SeasonService.AdvancedTherapy => 4,
        SeasonService.FullTherapy => 8,
        SeasonService.CalmFrenzy => 4,
        SeasonService.CalmImpetuous => 8,
        SeasonService.ForcedRetraining => 15,
        _ => 11
    };

    public static string Name(this SeasonService service) => service switch
    {
        SeasonService.AdvancedTherapy => "Advanced Therapy",
        SeasonService.FullTherapy => "Advanced Therapy (all injuries)",
        SeasonService.CalmFrenzy => "Anger Management (Frenzy)",
        SeasonService.CalmImpetuous => "Anger Management (Impetuous)",
        SeasonService.ForcedRetraining => "Forced Retraining",
        _ => "Regular Training"
    };

    public static string Description(this SeasonService service) => service switch
    {
        SeasonService.AdvancedTherapy => "Write off one injury.",
        SeasonService.FullTherapy => "Write off every injury.",
        SeasonService.CalmFrenzy => "The trooper no longer has Frenzy.",
        SeasonService.CalmImpetuous => "The trooper no longer has Impetuous, and loses No or Partial Cover (except No Cover from Aerial or Nanoscreen).",
        SeasonService.ForcedRetraining => "Replace one perk with another of the same tier. Rank 1 or above.",
        _ => "The trooper is now regular."
    };

    /// <summary>Writes off one of the trooper's injuries.</summary>
    public static bool RemoveInjury(SeasonTrooper trooper, Injury injury) => trooper.Injuries.Remove(injury);

    public static void RemoveAllInjuries(SeasonTrooper trooper) => trooper.Injuries.Clear();

    /// <summary>
    /// Takes Frenzy away, or Impetuous with No and Partial Cover. No Cover stays when the trooper is Aerial or
    /// has a Nanoscreen (<paramref name="keepsNoCover"/>).
    /// </summary>
    public static void CalmDown(SeasonTrooper trooper, SeasonService service, bool keepsNoCover)
    {
        if (service == SeasonService.CalmFrenzy)
        {
            Lose(trooper, InjuryEffects.Frenzy);
            return;
        }

        Lose(trooper, Impetuous);
        Lose(trooper, PartialCover);
        if (!keepsNoCover)
        {
            Lose(trooper, NoCover);
        }
    }

    /// <summary>The trooper stops being irregular and becomes regular.</summary>
    public static void TrainRegular(SeasonTrooper trooper)
    {
        Lose(trooper, Irregular);
        trooper.CharacteristicsLost.RemoveAll(lost => string.Equals(lost, Regular, StringComparison.OrdinalIgnoreCase));
        if (!trooper.CharacteristicsGained.Contains(Regular, StringComparer.OrdinalIgnoreCase))
        {
            trooper.CharacteristicsGained.Add(Regular);
        }
    }

    /// <summary>
    /// Swaps one of the trooper's perks for another of the same tier. One chosen this season keeps its place in the
    /// order chosen; one saved with the company is recorded as swapped for the season. False when the trooper doesn't
    /// have the old perk, the tiers differ, or another perk builds on the old one.
    /// </summary>
    /// <param name="startingPerks">The perks saved with the company.</param>
    public static bool Retrain(SeasonTrooper trooper, IReadOnlyList<string> startingPerks, string oldPerkId, PerkPick replacement)
    {
        var perks = TrooperProgress.Retrained(startingPerks, trooper).Concat(trooper.Perks).ToList();
        if (!perks.Contains(oldPerkId, StringComparer.OrdinalIgnoreCase) || !CanRetrain(perks, oldPerkId) || TierOf(oldPerkId) != replacement.Tier)
        {
            return false;
        }

        var index = trooper.Perks.FindIndex(perk => string.Equals(perk, oldPerkId, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            trooper.Perks[index] = replacement.NodeId;
        }
        else if (trooper.RetrainedPerks.FirstOrDefault(swap => string.Equals(swap.Replacement, oldPerkId, StringComparison.OrdinalIgnoreCase)) is { } swapped)
        {
            swapped.Replacement = replacement.NodeId;
        }
        else
        {
            trooper.RetrainedPerks.Add(new PerkSwap { Replaced = oldPerkId, Replacement = replacement.NodeId });
        }

        return true;
    }

    /// <summary>Whether a perk can be retrained: no other perk the trooper has follows on from it.</summary>
    public static bool CanRetrain(IReadOnlyList<string> perks, string perkId) =>
        !perks.Any(perk => string.Equals(ParentOf(perk), perkId, StringComparison.OrdinalIgnoreCase));

    public static int TierOf(string perkId) => NodeOf(perkId)?.Tier ?? 0;

    private static string? ParentOf(string perkId) => NodeOf(perkId)?.ParentId;

    private static PerkNode? NodeOf(string perkId) =>
        CompanyPerkCatalog.GetPerkNodeLists()
            .SelectMany(list => list.Roots)
            .Select(root => root.FindById(perkId))
            .FirstOrDefault(node => node is not null);

    private static void Lose(SeasonTrooper trooper, string characteristic)
    {
        trooper.CharacteristicsGained.RemoveAll(gained => string.Equals(gained, characteristic, StringComparison.OrdinalIgnoreCase));
        if (!trooper.CharacteristicsLost.Contains(characteristic, StringComparer.OrdinalIgnoreCase))
        {
            trooper.CharacteristicsLost.Add(characteristic);
        }
    }
}
