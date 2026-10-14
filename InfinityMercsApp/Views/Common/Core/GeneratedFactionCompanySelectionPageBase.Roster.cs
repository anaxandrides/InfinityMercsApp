using InfinityMercsApp.Services.CompanySelection.Models.Entries;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.CompanySelection.Models.Selection;
using InfinityMercsApp.ViewModels.UnitBrowser;

namespace InfinityMercsApp.Views.Common;

public abstract partial class GeneratedFactionCompanySelectionPageBase
{
    protected void SetSelectedUnit(ArmyUnitSelectionItem item)
    {
        _selectedUnit = SetSelectedUnitCore(
            item,
            _selectedUnit,
            selectionContextChanged: false,
            onContextChangedForSameSelection: () => { },
            loadSelectedUnitLogo: selectedItem => _ = LoadSelectedUnitLogoAsync(selectedItem),
            loadSelectedUnitDetails: selectedItem =>
            {
                _selectedUnit = selectedItem;
                LoadSelectedUnitDetails();
            });
    }

    protected void AddProfileToMercsCompany(ViewerProfileItem? profile)
    {
        if (profile is null || _selectedUnit is null)
        {
            return;
        }

        if (!profile.IsVisible || profile.IsLieutenantBlocked)
        {
            return;
        }

        if (IsLeftSlotUnit(_selectedUnit) && HasLeftSlotEntry())
        {
            return;
        }

        var commonSkills = UnitDisplayConfigurationsViewForVisuals.SelectedUnitCommonSkills;
        var captainSkills = ApplyCaptainSkills(commonSkills, profile);

        var peripheralStats = BuildMercsCompanyPeripheralStats(profile);
        var entry = BuildMercsCompanyEntryCore(
            _selectedUnit,
            profile,
            UnitDisplayConfigurationsViewForVisuals.SelectedUnitCommonEquipment,
            captainSkills,
            UnitMov,
            UnitCc,
            UnitBs,
            UnitPh,
            UnitWip,
            UnitArm,
            UnitBts,
            UnitVitalityHeader,
            UnitVitality,
            UnitS,
            UnitMoveFirstCm,
            UnitMoveSecondCm,
            FormatMoveValue,
            peripheralStats);

        var isLeftSlotEntry = IsLeftSlotUnit(_selectedUnit);

        AddMercsCompanyEntryCore(
            entry,
            MercsCompanyEntries,
            x => x.IsLieutenant,
            UpdateMercsCompanyTotal,
            ApplyLieutenantVisualStates,
            () => ApplyUnitVisibilityFilters());

        if (isLeftSlotEntry)
        {
            SetActiveSlot(1);
        }
    }

    private PeripheralMercsCompanyStats? BuildMercsCompanyPeripheralStats(ViewerProfileItem profile)
    {
        return BuildMercsCompanyPeripheralStatsCore(
            profile,
            UnitDisplayConfigurationsViewForVisuals.SelectedUnitProfileGroups,
            UnitDisplayConfigurationsViewForVisuals.SelectedUnitFilters,
            (peripheralName, peripheralProfile, selectedUnitFilters) =>
                BuildPeripheralStatBlock(peripheralName, peripheralProfile, selectedUnitFilters));
    }

    protected void RemoveMercsCompanyEntry(MercsCompanyEntry? entry)
    {
        RemoveMercsCompanyEntryCore(
            entry,
            MercsCompanyEntries,
            UpdateMercsCompanyTotal,
            ApplyLieutenantVisualStates,
            () => ApplyUnitVisibilityFilters());
    }

    protected void SelectMercsCompanyEntry(MercsCompanyEntry? entry, CancellationToken cancellationToken = default)
    {
        SelectMercsCompanyEntryCore(
            entry,
            Units,
            GetUnitFromProvider,
            (sourceUnitId, sourceFactionId, unitName, cachedLogoPath, packagedLogoPath) => new ArmyUnitSelectionItem
            {
                Id = sourceUnitId,
                SourceFactionId = sourceFactionId,
                Name = unitName,
                CachedLogoPath = cachedLogoPath,
                PackagedLogoPath = packagedLogoPath,
                Subtitle = null,
                IsVisible = false
            },
            SetSelectedUnit,
            LoadSelectedUnitDetails,
            cancellationToken);
    }

    protected void ApplyLieutenantVisualStates()
    {
        var visibleProfiles = ApplyLieutenantVisualStatesCore(
            MercsCompanyEntries,
            Profiles,
            SelectedStartSeasonPoints,
            SeasonPointsCapText,
            UnitAva,
            _selectedUnit,
            _filterState.ActiveUnitFilter,
            LieutenantOnlyUnits,
            entry => entry.IsLieutenant,
            entry => entry.SourceUnitId,
            entry => entry.SourceFactionId,
            unit => unit.Id,
            unit => unit.SourceFactionId);

        if (IsLeftSlotUnit(_selectedUnit) && HasLeftSlotEntry())
        {
            foreach (var profile in Profiles)
            {
                profile.IsLieutenantBlocked = true;
            }

            visibleProfiles = 0;
        }

        ProfilesStatus = visibleProfiles == 0
            ? "No configurations found for this unit."
            : $"{visibleProfiles} configurations loaded.";

        UpdatePeripheralStatBlockFromVisibleProfiles();
        UpdateSeasonValidationState();
    }

    protected bool IsLeftSlotUnit(ArmyUnitSelectionItem? unit)
    {
        return unit is not null && unit.SourceFactionId != CompanyFactionId;
    }

    protected bool HasLeftSlotEntry()
    {
        return MercsCompanyEntries.Any(e => e.SourceFactionId != CompanyFactionId);
    }
}
