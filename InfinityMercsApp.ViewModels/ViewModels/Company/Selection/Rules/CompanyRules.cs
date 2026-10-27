using InfinityMercsApp.Presentation.Company.Selection;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.Factions;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Services.CompanySelection.Teams;
using InfinityMercsApp.Services.Logos;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Company.Selection.Items;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;

namespace InfinityMercsApp.ViewModels.Company.Selection.Rules;

/// <summary>
/// What one kind of company changes on the selection screen: which factions it offers and how its slots fill,
/// which units and fireteams it lists, and what happens when a trooper joins. The defaults are a Standard
/// company's; each kind overrides what it does differently. A screen has its own instance, so a kind may keep
/// state between calls.
/// </summary>
public abstract class CompanyRules(ArmySourceSelectionMode mode)
{
    /// <summary>Whether the screen lists vanilla factions or sectorials.</summary>
    public ArmySourceSelectionMode Mode { get; } = mode;

    /// <summary>The page title and the faction picker's heading.</summary>
    public abstract string Heading { get; }

    /// <summary>The company type written to the saved company file.</summary>
    public abstract string CompanyTypeLabel { get; }

    /// <summary>Whether the company draws units from a second faction slot.</summary>
    public virtual bool HasRightSlot => false;

    /// <summary>Whether the right slot can be picked to receive a faction.</summary>
    public virtual bool CanPickRightSlot => HasRightSlot;

    /// <summary>Whether the right slot's faction is saved as a source of the company.</summary>
    public virtual bool SavesRightSlot => HasRightSlot;

    /// <summary>A slot every faction pick goes to, for companies whose other slot is fixed.</summary>
    public virtual int? FixedFactionSlot => null;

    /// <summary>The faction generated for the company (Airborne, Inspiring), which has only a packaged logo.</summary>
    public virtual GeneratedFactionLogo? GeneratedFaction => null;

    /// <summary>Whether the company tracks a Core fireteam (Cohesive companies).</summary>
    public virtual bool TracksCoreFireteam => false;

    /// <summary>The line above the fireteam list.</summary>
    public virtual string FireteamRuleText => "Unlimited Duo fireteams allowed per company";

    /// <summary>Whether picking a faction switches the units pane to fireteams.</summary>
    public virtual bool ShowsTeamsAfterFactionPick => false;

    /// <summary>Whether the filter offers "lieutenants only".</summary>
    public virtual bool AllowsLieutenantFilter => true;

    /// <summary>Whether the filter offers the fireteam view.</summary>
    public virtual bool AllowsTeamsFilter => true;

    /// <summary>Points off a profile with Tactical Awareness.</summary>
    public virtual int TacticalAwarenessDiscount => 0;

    // ── Setting up ─────────────────────────────────────────────────────────

    /// <summary>Sets the screen up as the company starts: fixed slots, the starting filter.</summary>
    public virtual void Prepare(CompanySelectionViewModel screen)
    {
    }

    /// <summary>Takes up what the saved company fixed when it was founded, before its roster is listed.</summary>
    public virtual void PrepareRecruitment(CompanySelectionViewModel screen, SavedCompanyFile company)
    {
    }

    /// <summary>Where recruiting starts once the slots are filled; by default the slot already picked.</summary>
    public virtual void PrepareRecruitingSlots(CompanySelectionViewModel screen)
    {
    }

    // ── Factions and slots ─────────────────────────────────────────────────

    /// <summary>The factions the picker offers.</summary>
    public abstract Task<IReadOnlyList<FactionRecord>> LoadFactionsAsync(CompanySelectionViewModel screen, CancellationToken cancellationToken);

    /// <summary>
    /// Whether listing the factions reads every army (to find those fit for this company), so every army must be in the
    /// army data first; otherwise the metadata is enough (or the listings worked out ahead), and a faction's army is
    /// needed only once it is picked.
    /// </summary>
    public virtual bool ReadsEveryArmyToList(CompanySelectionViewModel screen) => false;

    /// <summary>Puts a picked faction in its slot and lists the units.</summary>
    public virtual Task AssignFactionAsync(CompanySelectionViewModel screen, ArmyFactionSelectionItem faction) =>
        screen.AssignToSlotAsync(faction);

    /// <summary>Makes <paramref name="slot"/> the one the next faction pick goes to.</summary>
    public virtual void SetActiveSlot(CompanySelectionViewModel screen, int slot) =>
        screen.Slots.ActiveIndex = FixedFactionSlot ?? FactionSlots.Active(slot, HasRightSlot);

    /// <summary>The factions whose units are listed.</summary>
    public virtual IReadOnlyList<ArmyFactionSelectionItem> UnitSources(CompanySelectionViewModel screen) => screen.Slots.Sources;

    /// <summary>A new points limit: by default the units are re-filtered and the filter's options rebuilt.</summary>
    public virtual Task OnPointsLimitChangedAsync(CompanySelectionViewModel screen)
    {
        screen.RefreshUnitsForPoints();
        return Task.CompletedTask;
    }

    // ── Units and logos ────────────────────────────────────────────────────

    /// <summary>A faction's units: by default only those Mercs companies can field.</summary>
    public virtual IReadOnlyList<ArmyResumeRecord> ResumesFor(IArmyDataService armyData, int factionId, CancellationToken cancellationToken) =>
        armyData.GetResumeByFactionMercsOnly(factionId, cancellationToken);

    public virtual Task CacheUnitLogosAsync(
        FactionLogoCacheService? logos, int factionId, IReadOnlyList<ArmyResumeRecord> units, CancellationToken cancellationToken) =>
        logos?.CacheUnitLogosFromRecordsAsync(factionId, units, cancellationToken) ?? Task.CompletedTask;

    /// <summary>Whose logo a unit shows; by default its own.</summary>
    public virtual UnitLogoIds LogoIdsFor(int factionId, int unitId, string? logoField) => new(factionId, unitId);

    /// <summary>Whether a unit stands out in the list (the TAG company's own TAG).</summary>
    public virtual bool IsHighlightedUnit(int factionId, int unitId) => false;

    /// <summary>Lets a company reload from another slot when its list came back empty; true when it did.</summary>
    public virtual Task<bool> TryRecoverEmptyUnitListAsync(CompanySelectionViewModel screen, CancellationToken cancellationToken) =>
        Task.FromResult(false);

    /// <summary>Company-specific visibility rules; by default an empty result shows every unit rather than none.</summary>
    public virtual void AdjustUnitVisibility(
        CompanySelectionViewModel screen, CompanyUnitVisibilityLookupContext context, CancellationToken cancellationToken) =>
        screen.UnitList.ShowAllWhenNoneVisible();

    /// <summary>Company-specific fireteam visibility, after the fireteams have followed the units they list.</summary>
    public virtual void AdjustTeamVisibility(CompanySelectionViewModel screen)
    {
    }

    // ── Fireteams ──────────────────────────────────────────────────────────

    /// <summary>How many of the fireteam the company may field; fireteams it can field none of aren't listed.</summary>
    public virtual int TeamCount(MergedFireteam team) => team.Duo;

    public virtual string TeamCountText(MergedFireteam team) => $"D: {team.Duo}";

    /// <summary>A fireteam slot's minimum as shown.</summary>
    public virtual string DisplayedTeamMinimum(string min) => min;

    /// <summary>Whether picking a unit from this fireteam slot shows only its fireteam-only profiles.</summary>
    public virtual bool RestrictsProfilesFor(ArmyTeamUnitLimitItem teamItem) => false;

    // ── Troopers ───────────────────────────────────────────────────────────

    /// <summary>A company's extra profile blocking, after the points and AVA; returns how many profiles still show.</summary>
    public virtual int AdjustProfileAvailability(CompanySelectionViewModel screen, int visibleProfiles) => visibleProfiles;

    /// <summary>Whether the unit can join the company at all.</summary>
    public virtual bool CanAddProfile(CompanySelectionViewModel screen, ArmyUnitSelectionItem unit) => true;

    /// <summary>Why <see cref="CanAddProfile"/> turned the unit away, to tell the player; null to say nothing.</summary>
    public virtual string? CannotAddMessage(CompanySelectionViewModel screen, ArmyUnitSelectionItem unit) => null;

    /// <summary>The skills every new trooper of this kind of company carries.</summary>
    public virtual IReadOnlyCollection<string> CommonSkillsFor(
        ArmyUnitSelectionItem unit, ViewerProfileItem profile, IReadOnlyList<string> commonSkills) => commonSkills.ToList();

    /// <summary>Finishes a new trooper's entry before it joins the company.</summary>
    public virtual void OnEntryBuilt(CompanySelectionViewModel screen, MercsCompanyEntry entry)
    {
    }

    /// <summary>Called once a trooper has joined the company.</summary>
    public virtual Task AfterEntryAddedAsync(
        CompanySelectionViewModel screen, ArmyUnitSelectionItem unit, ViewerProfileItem profile, MercsCompanyEntry entry) =>
        Task.CompletedTask;
}
