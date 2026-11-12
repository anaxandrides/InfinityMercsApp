using InfinityMercsApp.Domain.Company;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.SpecOps;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.Profiles;
using InfinityMercsApp.Services.CompanySelection.SpecOps;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Company.Selection.Items;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;

namespace InfinityMercsApp.ViewModels.Company.Selection.Rules;

/// <summary>
/// A Team-Ops Company: two or three of one army's Team-Ops troopers (its MI, HI and SpecBot, one of each), each with
/// one upgrade from the army's Team-Ops table. The MI or HI picked as the company is founded is its captain.
/// </summary>
public sealed class TeamOpsCompanyRules(ArmySourceSelectionMode mode) : StandardCompanyRules(mode)
{
    public override string Heading => "Choose your army";

    public override string CompanyTypeLabel => TeamOpsCompany.CompanyType;

    public override bool AllowsLieutenantFilter => false;

    public override bool OffersSpecOps => false;

    public override bool ChoosesCaptainAtFounding => true;

    public override string FireteamRuleText =>
        $"Your Team-Ops form {TeamOpsCompany.FireteamName}: a Duo of two, a Haris of three";

    public override Task<IReadOnlyList<FactionRecord>> LoadFactionsAsync(CompanySelectionViewModel screen, CancellationToken cancellationToken) =>
        EveryArmyAsync(screen, cancellationToken);

    /// <summary>Only the army's Team-Ops entry is listed.</summary>
    public override IReadOnlyList<ArmyResumeRecord> ResumesFor(IArmyDataService armyData, int factionId, CancellationToken cancellationToken) =>
        new SpecUpgradeCatalog(armyData).ResumesOf(factionId, SpecialOperativeKind.TeamOps, cancellationToken);

    /// <summary>The army's fireteams aren't the company's; it has only its own.</summary>
    public override int TeamCount(MergedFireteam team) => 0;

    /// <summary>Each of the entry's MI, HI and SpecBot joins once, up to three troopers.</summary>
    public override int AdjustProfileAvailability(CompanySelectionViewModel screen, int visibleProfiles)
    {
        if (screen.Preview.SelectedUnit is not { } unit)
        {
            return visibleProfiles;
        }

        var taken = screen.Roster.Entries
            .Where(entry => entry.SourceUnitId == unit.Id && entry.SourceFactionId == unit.SourceFactionId)
            .Select(entry => entry.ProfileGroupId)
            .ToHashSet();
        var full = screen.Roster.Entries.Count >= TeamOpsCompany.MaxTroopers;
        foreach (var profile in screen.Preview.Profiles.Where(profile => profile.IsVisible))
        {
            profile.IsLieutenantBlocked = full || taken.Contains(profile.GroupId);
        }

        return visibleProfiles;
    }

    public override bool CanAddProfile(CompanySelectionViewModel screen, ArmyUnitSelectionItem unit) =>
        screen.Roster.Entries.Count < TeamOpsCompany.MaxTroopers;

    /// <summary>The HI and the SpecBot are recruited on their own group's profile, not the MI's.</summary>
    public override TrooperForm? FormFor(CompanyUnitDetail detail, ViewerProfileItem profile) =>
        GroupProfile(detail, profile) is { IsFirst: false, Profile: { } groupProfile }
            ? TransmutationForms.Of(groupProfile, detail.Filters, detail.ShowUnitsInInches)
            : null;

    /// <summary>Each trooper keeps its own group's troop type: MI, HI or REM.</summary>
    public override string? UnitTypeCodeFor(CompanyUnitDetail detail, ViewerProfileItem profile) =>
        GroupProfile(detail, profile)?.Profile.Type is int type &&
        detail.Filters.Names(ArmyFilterSection.Type).TryGetValue(type, out var name) &&
        !string.IsNullOrWhiteSpace(name)
            ? name.Trim().ToUpperInvariant()
            : null;

    public override int UpgradesFor(MercsCompanyEntry entry) => SpecUpgradeRules.TeamOpsUpgrades;

    public override bool OffersSpecBotWith(MercsCompanyEntry entry) => false;

    public override string? StartBlockedReason(CompanySelectionViewModel screen) =>
        screen.Roster.Entries.Count < TeamOpsCompany.MinTroopers ? "Choose at least two Team-Ops troopers." : null;

    /// <summary>
    /// The captain is the company's MI or HI: the only one, or the one the player picks. False when there is none or
    /// the player backs out.
    /// </summary>
    public override async Task<bool> PrepareFoundingAsync(CompanySelectionViewModel screen)
    {
        var troopers = screen.Roster.Entries.ToList();
        var candidates = troopers.Where(entry => !UnitTypeCodes.IsRemote(entry.UnitTypeCode)).ToList();
        MercsCompanyEntry? captain;
        switch (candidates.Count)
        {
            case 0:
                await screen.Services.Dialogs.ShowAlertAsync("Needs a Captain", "A Team-Ops Company's captain is its MI or HI. Add one first.");
                return false;
            case 1:
                captain = candidates[0];
                break;
            default:
                var labels = candidates.Select(candidate => candidate.Name).ToList();
                var choice = await screen.Services.Dialogs.ChooseAsync(
                    "Who captains the company? This can't change later.", "Cancel", labels);
                var picked = choice is null ? -1 : labels.IndexOf(choice);
                if (picked < 0)
                {
                    return false;
                }

                captain = candidates[picked];
                break;
        }

        foreach (var trooper in troopers)
        {
            trooper.IsLieutenant = ReferenceEquals(trooper, captain);
        }

        return true;
    }

    // The profile group the configuration belongs to, its first profile, and whether it is the unit's first group.
    private static GroupProfileOf? GroupProfile(CompanyUnitDetail detail, ViewerProfileItem profile)
    {
        var groups = detail.ProfileGroups.Groups;
        var index = groups.ToList().FindIndex(group => group.Id == profile.GroupId);
        return index >= 0 && groups[index].Profiles.FirstOrDefault() is { } first ? new GroupProfileOf(first, index == 0) : null;
    }

    private sealed record GroupProfileOf(UnitProfile Profile, bool IsFirst);
}
