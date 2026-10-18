using InfinityMercsApp.Domain.Company;
using InfinityMercsApp.Domain.Models.Perks;
using InfinityMercsApp.Domain.Models.Season;

namespace InfinityMercsApp.Domain.Season.Troopers;

/// <summary>
/// Where one trooper stands this season: its experience and rank, the perk points it still has to spend,
/// and everything that changes its profile (injuries, perks, rank skills, downtime and services).
/// </summary>
public sealed record TrooperProgress(
    int EntryIndex,
    int StartingXp,
    int SeasonXp,
    bool IsTag,
    bool IsLoneWolf,
    IReadOnlyList<string> StartingPerks,
    SeasonTrooper Record)
{
    public int TotalXp => StartingXp + SeasonXp;

    public int RankLevel => CompanyUnitExperienceRanks.GetRankLevel(TotalXp);

    public string RankName => CompanyUnitExperienceRanks.GetRankName(TotalXp);

    /// <summary>Every perk the trooper has: those saved with the company, then those chosen this season.</summary>
    public IReadOnlyList<string> Perks => [.. StartingPerks, .. Record.Perks];

    /// <summary>One perk point per rank (two for a Lone Wolf), less the perks already taken from the usual lists.</summary>
    public int PerkPointsOwed =>
        Math.Max(0, RankLevel * (IsLoneWolf ? ProxyPack.PerksPerRank : 1) - Perks.Count(perk => !PerkChoices.IsTagPerk(perk)));

    /// <summary>The renown its ranks add: 10 a rank for a Lone Wolf, none for anyone else.</summary>
    public int RankRenown => IsLoneWolf ? RankLevel * ProxyPack.RenownPerRank : 0;

    /// <summary>How many Aspects a Lone Wolf has at its rank, its own included; none for anyone else.</summary>
    public int Aspects => IsLoneWolf ? ProxyPack.AspectsAt(RankLevel) : 0;

    /// <summary>A TAG also earns a TAG perk per rank, spent on the Mecha tree.</summary>
    public int TagPerkPointsOwed => IsTag ? Math.Max(0, RankLevel - Perks.Count(PerkChoices.IsTagPerk)) : 0;

    public bool HasPerkPointsToSpend => PerkPointsOwed > 0 || TagPerkPointsOwed > 0;

    /// <summary>The kind of the next point to spend: usual points first.</summary>
    public PerkPointKind? NextPerkPoint => PerkPointsOwed > 0 ? PerkPointKind.Standard : TagPerkPointsOwed > 0 ? PerkPointKind.Tag : null;

    public IReadOnlyList<string> FreeSkills => CompanyUnitExperienceRanks.FreeSkillsFor(TotalXp);

    /// <summary>
    /// What changes the trooper's line on a screen that lists its perks too: a perk's skill shows with the perks, and
    /// only one improving a skill it has changes its skills.
    /// </summary>
    public TrooperChange ChangeBesidePerks => Change with { ListsPerkSkills = false };

    /// <summary>Everything that changes the trooper's saved profile this season.</summary>
    public TrooperChange Change =>
        TrooperChange.Sum(Record.Injuries.Select(injury => injury.Change()))
        + TrooperChange.Sum(Perks.Select(PerkEffects.For))
        + new TrooperChange
        {
            SkillsGained = [.. FreeSkills, .. Record.SkillsGained],
            CharacteristicsGained = Record.CharacteristicsGained,
            CharacteristicsLost = Record.CharacteristicsLost
        };

    /// <summary>The trooper's progress, from its saved entry values and the season so far.</summary>
    public static TrooperProgress For(
        int entryIndex,
        string name,
        int startingXp,
        IReadOnlyList<string> startingPerks,
        bool isTag,
        bool isLoneWolf,
        SeasonFile? seasonFile) =>
        new(
            entryIndex,
            Math.Max(0, startingXp),
            SeasonExperience.GainedBy(seasonFile, entryIndex, name),
            isTag,
            isLoneWolf,
            startingPerks,
            seasonFile?.Troopers.GetValueOrDefault(entryIndex) ?? new SeasonTrooper());
}
