using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Services.CompanySelection.Factions;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.CompanySelection.Profiles;
using InfinityMercsApp.Services.CompanySelection.Units;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Company.Selection.Items;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;

namespace InfinityMercsApp.ViewModels.Company.Selection;

/// <summary>
/// The unit picked on a company selection screen: its statline, icons and shared gear in <see cref="Detail"/>,
/// and the profiles that can be added to the company.
/// </summary>
public sealed partial class UnitPreviewViewModel(UnitDetailsLoader unitDetails, ThemeFactions themes, ArmySourceSelectionMode mode)
    : ObservableObject
{
    public const string NoUnitStatus = "Select a unit.";

    [ObservableProperty]
    public partial ArmyUnitSelectionItem? SelectedUnit { get; set; }

    [ObservableProperty]
    public partial string ProfilesStatus { get; set; } = NoUnitStatus;

    /// <summary>The selected unit as the unit-detail display shows it.</summary>
    public CompanyUnitDetail Detail { get; } = new();

    public ObservableCollection<ViewerProfileItem> Profiles { get; } = [];

    /// <summary>Whether only fireteam-only profiles are shown for the selected unit.</summary>
    public bool RestrictsToFto { get; private set; }

    /// <summary>
    /// Selects <paramref name="unit"/>; true when its details need loading, because it is another unit or the
    /// fireteam-only restriction changed.
    /// </summary>
    public bool Select(ArmyUnitSelectionItem unit, bool restrictToFto)
    {
        var restrictionChanged = RestrictsToFto != restrictToFto;
        RestrictsToFto = restrictToFto;
        if (ReferenceEquals(SelectedUnit, unit))
        {
            return restrictionChanged;
        }

        if (SelectedUnit is not null)
        {
            SelectedUnit.IsSelected = false;
        }

        unit.IsSelected = true;
        SelectedUnit = unit;
        return true;
    }

    /// <summary>Selects nothing and empties the detail.</summary>
    public void Clear()
    {
        if (SelectedUnit is not null)
        {
            SelectedUnit.IsSelected = false;
        }

        SelectedUnit = null;
        Detail.Reset();
        Profiles.Clear();
        ProfilesStatus = NoUnitStatus;
    }

    /// <summary>
    /// Loads the selected unit's statline, trait icons, shared equipment and skills, and profiles; true when it has
    /// profiles to show.
    /// </summary>
    public bool Load(UnitPreviewRequest request, CancellationToken cancellationToken = default)
    {
        Detail.Reset(clearLogo: false, resetHeaderColors: false);
        Profiles.Clear();
        ProfilesStatus = NoUnitStatus;
        if (SelectedUnit is not { } unit)
        {
            return false;
        }

        Detail.UnitNameHeading = unit.Name;
        var details = unitDetails.Load(
            unit.SourceFactionId, unit.Id, unit.IsSpecOps, request.LieutenantOnlyUnits, Detail.ShowUnitsInInches, cancellationToken);
        Detail.ApplyHeaderColors(themes.NameFor(mode, unit.SourceFactionId, details.Unit?.FactionIds));
        if (details.ProfileGroups.IsEmpty)
        {
            Console.Error.WriteLine($"No profiles found for unit {unit.Id} of faction {unit.SourceFactionId}.");
            return false;
        }

        Detail.SetProfileData(details.ProfileGroups, details.Filters);
        Detail.ApplyFirstProfileStats(details.ProfileGroups);
        if (!details.TreatAsSpecOps &&
            unitDetails.HighestAva(unit.Id, request.AvaFactionIds, cancellationToken) is { } ava)
        {
            Detail.ApplyAva(ava);
        }

        var order = details.OrderTraits;
        Detail.SetOrderTraits(order.HasRegular, order.HasIrregular, order.HasImpetuous, order.HasTacticalAwareness);
        var tech = details.TechTraits;
        Detail.SetTechTraits(tech.HasCube, tech.HasCube2, tech.HasHackable);
        Detail.SetCommonEquipmentAndSkills(details.CommonEquipment, details.CommonSkills, details.TreatAsSpecOps);
        Detail.EquipmentSummary = $"Equipment: {Listed(details.CommonEquipment)}";
        Detail.SpecialSkillsSummary = $"Special Skills: {Listed(details.CommonSkills)}";

        var configurations = ProfileConfigurations.Build(new ProfileConfigurationRequest
        {
            ProfileGroups = details.ProfileGroups,
            Filters = details.Filters,
            ForceLieutenant = details.TreatAsSpecOps,
            ShowUnitsInInches = Detail.ShowUnitsInInches,
            FireteamOnly = RestrictsToFto,
            TacticalAwarenessDiscount = request.TacticalAwarenessDiscount
        });
        foreach (var configuration in configurations)
        {
            Profiles.Add(ViewerProfileItem.From(configuration, Detail.ShowUnitsInInches, Detail.Icons.TacticalAwareness));
        }

        return true;
    }

    /// <summary>Shows the profiles the points, filter and lieutenant rules allow, and blocks those that can't be added.</summary>
    /// <returns>How many profiles show.</returns>
    public int ApplyAvailability(RosterRoom room, UnitFilterCriteria filter, bool lieutenantOnlyUnits)
    {
        var visibleProfiles = 0;
        foreach (var profile in Profiles)
        {
            var availability = ProfileAvailability.Of(
                ProfileValues.ParseCost(profile.Cost), profile.IsLieutenant, room, filter, lieutenantOnlyUnits);
            profile.IsVisible = availability.IsVisible;
            profile.IsLieutenantBlocked = availability.IsBlocked;
            if (availability.IsVisible)
            {
                visibleProfiles++;
            }
        }

        return visibleProfiles;
    }

    /// <summary>Blocks every profile, for a company with no room left for this unit.</summary>
    public void BlockAll()
    {
        foreach (var profile in Profiles)
        {
            profile.IsLieutenantBlocked = true;
        }
    }

    /// <summary>The status line for how many profiles show, and the peripheral of the first visible one that has one.</summary>
    public void ShowAvailability(int visibleProfiles)
    {
        ProfilesStatus = visibleProfiles == 0 ? "No configurations found for this unit." : $"{visibleProfiles} configurations loaded.";
        Detail.ResetPeripheral();
        if (Profiles.FirstOrDefault(profile => profile.IsVisible && profile.HasPeripherals) is { } profile &&
            PeripheralStatsOf(profile) is { } stats)
        {
            Detail.ApplyPeripheral(stats);
        }
    }

    /// <summary>The statline of the profile's first peripheral, when the selected unit has its profile.</summary>
    public PeripheralMercsCompanyStats? PeripheralStatsOf(ViewerProfileItem profile)
    {
        var peripheralName = PeripheralNames.FirstOf(profile.Peripherals);
        return !string.IsNullOrWhiteSpace(peripheralName) && Detail.ProfileGroups.FindPeripheralProfile(peripheralName) is { } peripheralProfile
            ? PeripheralMercsCompanyStats.From(peripheralName, peripheralProfile, Detail.Filters, Detail.ShowUnitsInInches)
            : null;
    }

    private static string Listed(IReadOnlyList<string> names) => names.Count == 0 ? "-" : string.Join(", ", names);
}
