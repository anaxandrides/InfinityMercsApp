using InfinityMercsApp.Views.Common;
using InfinityMercsApp.Views.Controls;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;

namespace InfinityMercsApp.Views.Common;

public abstract partial class GeneratedFactionCompanySelectionPageBase
{
    protected async Task LoadFactionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // FilterFactionsWithLieutenantAsync does a synchronous, lock-guarded DB read per
            // candidate faction (and per unit within it) — its "async" query source has no real
            // await in it, so none of this yields the UI thread on its own. Run the whole
            // data-gathering step on a worker thread and only touch the UI-bound Factions
            // collection on the continuation.
            var items = await Task.Run(async () =>
            {
                var filteredFactions = await LoadFilteredFactionRecordsAsync(cancellationToken);
                var factionsWithLieutenant = await FilterFactionsWithLieutenantAsync(filteredFactions, cancellationToken);
                return BuildFactionSelectionItems(
                    factionsWithLieutenant,
                    (id, parentId, name, cachedLogoPath, packagedLogoPath) => new ArmyFactionSelectionItem
                    {
                        Id = id,
                        ParentId = parentId,
                        Name = name,
                        CachedLogoPath = cachedLogoPath,
                        PackagedLogoPath = packagedLogoPath
                    });
            }, cancellationToken);

            Factions.ReplaceRange(items);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"GeneratedFactionCompanyPage LoadFactionsAsync failed: {ex.Message}");
        }
    }

    protected async Task<List<FactionRecord>> FilterFactionsWithLieutenantAsync(
        List<FactionRecord> factions,
        CancellationToken cancellationToken = default)
    {
        var result = new List<FactionRecord>();
        foreach (var faction in factions)
        {
            var snapshot = ArmyDataService.GetFactionSnapshot(faction.Id, cancellationToken);
            var skillsLookup = CompanySelectionSharedUtilities.BuildIdNameLookup(snapshot?.FiltersJson, "skills");
            var mercsEntries = await ArmyDataService.GetMergedMercsArmyListAsync([faction.Id], cancellationToken);

            var hasLieutenant = mercsEntries.Any(entry =>
                CompanySelectionSharedUtilities.UnitHasLieutenantOption(entry.ProfileGroupsJson, skillsLookup));

            if (hasLieutenant)
            {
                result.Add(faction);
            }
        }

        return result;
    }

    protected void SetSelectedFaction(ArmyFactionSelectionItem item)
    {
        SetSelectedFactionCore(
            _factionSelectionState,
            item,
            AssignSelectedFactionToActiveSlot);
    }

    protected void AssignSelectedFactionToActiveSlot(ArmyFactionSelectionItem item)
    {
        if (!TryAssignSelectedFactionToActiveSlotCore(
                true,
                0,
                _factionSelectionState,
                item,
                (slotIndex, cachedPath, packagedPath) => _ = LoadSlotIconAsync(slotIndex, cachedPath, packagedPath),
                blockCrossSlotDuplicateSelection: true,
                out var factionChanged))
        {
            Console.WriteLine($"[GeneratedFactionCompanyPage] Duplicate selection blocked for faction {item.Id} ({item.Name}).");
            return;
        }

        SwitchToLeftSlot();

        HandleFactionAssignmentSideEffectsCore(
            factionChanged,
            () => { },
            ResetMercsCompany,
            () => LoadUnitsForActiveSlotAsync(),
            onAssignmentCompleted: () =>
            {
                TeamsView = false;
                ShowFactionStrip = false;
            });
    }

    protected void ResetMercsCompany()
    {
        ResetMercsCompanyCore(
            MercsCompanyEntries,
            UpdateMercsCompanyTotal);
    }
}

