using System.Globalization;
using InfinityMercsApp.Domain.Company;
using InfinityMercsApp.Services.CompanySelection.Factions;
using InfinityMercsApp.Services.CompanySelection.Teams;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Company.Selection.Items;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;
using CCFactionFireteamValidityRecord = InfinityMercsApp.Domain.Models.Army.CCFactionFireteamValidityRecord;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;

namespace InfinityMercsApp.ViewModels.Company.Selection.Rules;

/// <summary>
/// A Cohesive company: one sectorial that can field a Core fireteam within the points limit. The player
/// tracks one fireteam (<see cref="CoreFireteamTracker"/>); its level gives a bonus, and troopers outside it are
/// irregular. Every trooper must be able to join it. Troopers get Number 2 (and the lieutenant FT Master), and
/// Tactical Awareness costs 3 points less. The fireteam is saved with the company and fixed from then on.
/// </summary>
public sealed class CohesiveCompanyRules(ArmySourceSelectionMode mode) : CompanyRules(mode)
{
    public const string NumberTwoSkill = "Number 2";
    public const string FireteamMasterSkill = "FT Master";
    public const int TacticalAwarenessPointsOff = 3;

    public override string Heading => "Choose your sectorial:";

    public override string CompanyTypeLabel => CompanyTypes.Cohesive;

    public override bool TracksCoreFireteam => true;

    public override string FireteamRuleText => "One Core fireteam per company";

    public override bool ShowsTeamsAfterFactionPick => true;

    /// <summary>Tactical Awareness (on the option, what it includes, or the group's profiles) costs 3 points less.</summary>
    public override int TacticalAwarenessDiscount => TacticalAwarenessPointsOff;

    /// <summary>The company opens on its fireteams.</summary>
    public override void Prepare(CompanySelectionViewModel screen)
    {
        screen.UnitList.TeamsView = true;
        screen.UnitList.IsFilterActive = true;
    }

    // ── Factions ────────────────────────────────────────────────────────────

    /// <summary>
    /// Only factions with a valid core fireteam are listed: each army is read, unless that was worked out ahead for this
    /// points limit. It was worked out with no unit filter, so a filter set before a faction is picked reads every army.
    /// </summary>
    public override bool ReadsEveryArmyToList(CompanySelectionViewModel screen) => ListingFor(screen) is null;

    /// <summary>The sectorials that can field a valid Core fireteam; each answer is cached per points limit and filter.</summary>
    public override async Task<IReadOnlyList<FactionRecord>> LoadFactionsAsync(CompanySelectionViewModel screen, CancellationToken cancellationToken)
    {
        var services = screen.Services;
        var ordered = await services.FactionCatalog.LoadAsync(Mode, cancellationToken);
        if (ListingFor(screen) is { } listing)
        {
            var fireteams = listing.Factions.ToDictionary(faction => faction.FactionId);
            screen.CoreFireteam?.SetValidTeams(fireteams.Values.ToDictionary(
                faction => faction.FactionId, faction => CoreFireteamEligibility.ToValidCoreFireteamSet(faction.ValidCoreFireteams.ToList())));
            return ordered.Where(faction => fireteams.ContainsKey(faction.Id)).ToList();
        }

        var maxCost = PointsLimit(screen);
        var filterKey = ValidityFilterKey(screen.UnitList, maxCost);
        var cachedByFaction = services.SpecOps
            .GetCCFactionFireteamValidity(filterKey, ordered.Select(faction => faction.Id).ToList(), cancellationToken)
            .GroupBy(row => row.FactionId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(row => row.EvaluatedAtUnixSeconds).First());

        foreach (var faction in ordered.Where(faction => !cachedByFaction.ContainsKey(faction.Id)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var validTeams = services.CoreFireteams.ValidCoreFireteams(faction.Id, screen.UnitList.ActiveFilter, maxCost, cancellationToken);
            services.SpecOps.UpsertCCFactionFireteamValidity(faction.Id, filterKey, validTeams.Count > 0, validTeams, cancellationToken);
            cachedByFaction[faction.Id] = new CCFactionFireteamValidityRecord
            {
                FactionId = faction.Id,
                FilterKey = filterKey,
                HasValidCoreFireteams = validTeams.Count > 0,
                ValidCoreFireteams = validTeams,
                EvaluatedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }

        screen.CoreFireteam?.SetValidTeams(cachedByFaction.Values.ToDictionary(
            row => row.FactionId, row => CoreFireteamEligibility.ToValidCoreFireteamSet(row.ValidCoreFireteams)));
        return ordered.Where(faction => cachedByFaction.TryGetValue(faction.Id, out var row) && row.HasValidCoreFireteams).ToList();
    }

    private static int PointsLimit(CompanySelectionViewModel screen) =>
        int.TryParse(screen.SelectedStartSeasonPoints, NumberStyles.Integer, CultureInfo.InvariantCulture, out var limit) ? limit : 0;

    // The listing worked out ahead for the screen's points limit, when there is one and no unit filter is set (it was
    // worked out without one); a recruiting company's limit (its CR) is never one of the starting limits.
    private static CohesiveListing? ListingFor(CompanySelectionViewModel screen)
    {
        var unitList = screen.UnitList;
        var filter = unitList.ActiveFilter;
        var unfiltered = filter.ToQuery().Terms.Count == 0 &&
                         filter.MinPoints is null &&
                         filter.MaxPoints is null &&
                         !filter.LieutenantOnlyUnits &&
                         !unitList.LieutenantOnlyUnits;
        return unfiltered ? screen.Services.ArmyLoader?.Listings?.CohesiveAt(PointsLimit(screen)) : null;
    }

    // What the cached validity depends on: the points limit and the filter.
    private static string ValidityFilterKey(UnitListViewModel unitList, int maxCost)
    {
        var filter = unitList.ActiveFilter;
        var terms = string.Join(";", filter.ToQuery().Terms
            .OrderBy(term => term.Field)
            .Select(term => $"{term.Field}:{term.MatchMode}:{string.Join(",", term.Values.Order(StringComparer.OrdinalIgnoreCase))}"));

        return string.Join("|",
            "cc-core-v2",
            $"pts:{maxCost}",
            $"lt:{(unitList.LieutenantOnlyUnits ? 1 : 0)}",
            $"terms:{terms}",
            $"min:{filter.MinPoints?.ToString(CultureInfo.InvariantCulture) ?? string.Empty}",
            $"max:{filter.MaxPoints?.ToString(CultureInfo.InvariantCulture) ?? string.Empty}",
            $"filterlt:{(filter.LieutenantOnlyUnits ? 1 : 0)}");
    }

    /// <summary>A new points limit changes which sectorials and fireteams are valid, so everything reloads.</summary>
    public override async Task OnPointsLimitChangedAsync(CompanySelectionViewModel screen)
    {
        try
        {
            screen.UnitList.PreparedOptions = null;
            await screen.LoadFactionsAsync();
            await screen.LoadUnitsAsync();
            screen.ApplyUnitVisibilityFilters();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Reloading for the new points limit failed: {ex.Message}");
        }
    }

    // ── Fireteams ───────────────────────────────────────────────────────────

    public override int TeamCount(MergedFireteam team) => team.Core;

    public override string TeamCountText(MergedFireteam team) => $"C: {team.Core}";

    /// <summary>A Cohesive company needn't fill a fireteam's minimums, so they all read 0.</summary>
    public override string DisplayedTeamMinimum(string min) =>
        string.Equals(min?.Trim(), "*", StringComparison.Ordinal) ||
        (int.TryParse(min, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
            ? "0"
            : min ?? "0";

    /// <summary>Picking a unit from a fireteam-only (FTO) slot shows only its FTO profiles.</summary>
    public override bool RestrictsProfilesFor(ArmyTeamUnitLimitItem teamItem) => FireteamMatching.IsFtoLabel(teamItem.Name);

    /// <summary>Once the company has troopers, only those who can join its fireteam show.</summary>
    public override void AdjustUnitVisibility(
        CompanySelectionViewModel screen, CompanyUnitVisibilityLookupContext context, CancellationToken cancellationToken)
    {
        base.AdjustUnitVisibility(screen, context, cancellationToken);
        if (!IsHeldToFireteam(screen) || screen.CoreFireteam is not { } fireteam)
        {
            return;
        }

        foreach (var unit in screen.UnitList.Units.Where(unit => unit.IsVisible && !fireteam.CanJoin(unit)))
        {
            unit.IsVisible = false;
        }
    }

    /// <summary>Once the company has troopers, the other fireteams are hidden; the wildcard buckets stay.</summary>
    public override void AdjustTeamVisibility(CompanySelectionViewModel screen)
    {
        if (!IsHeldToFireteam(screen))
        {
            return;
        }

        foreach (var team in screen.UnitList.TeamEntries.Where(team => !team.IsWildcardBucket && !team.IsTrackedTeam))
        {
            team.IsVisible = false;
        }
    }

    // The first trooper commits the company to the tracked fireteam: everyone after must be able to join it.
    private static bool IsHeldToFireteam(CompanySelectionViewModel screen) =>
        screen.Roster.Entries.Count > 0 && screen.CoreFireteam?.Slots() is not null;

    // ── Troopers ────────────────────────────────────────────────────────────

    /// <summary>Every trooper must be able to join the core fireteam.</summary>
    public override bool CanAddProfile(CompanySelectionViewModel screen, ArmyUnitSelectionItem unit) =>
        screen.CoreFireteam?.CanJoin(unit) ?? true;

    public override string? CannotAddMessage(CompanySelectionViewModel screen, ArmyUnitSelectionItem unit) =>
        $"{unit.Name} can't join {screen.CoreFireteam?.Name}, and every trooper in a Cohesive company must be able to.";

    /// <summary>Every trooper has Number 2; the lieutenant also has FT Master.</summary>
    public override IReadOnlyCollection<string> CommonSkillsFor(
        ArmyUnitSelectionItem unit, ViewerProfileItem profile, IReadOnlyList<string> commonSkills)
    {
        var skills = commonSkills.Where(skill => !string.IsNullOrWhiteSpace(skill)).Select(skill => skill.Trim()).ToList();
        if (!skills.Any(skill => skill.Contains("number 2", StringComparison.OrdinalIgnoreCase)))
        {
            skills.Add(NumberTwoSkill);
        }

        if (profile.IsLieutenant && !skills.Any(skill => skill.Contains("ft master", StringComparison.OrdinalIgnoreCase)))
        {
            skills.Add(FireteamMasterSkill);
        }

        return skills;
    }

    /// <summary>Remembers whether the trooper is normally irregular, for the order badges.</summary>
    public override void OnEntryBuilt(CompanySelectionViewModel screen, MercsCompanyEntry entry) =>
        entry.NormallyIrregular = screen.Preview.Detail.Icons.Irregular;
}
