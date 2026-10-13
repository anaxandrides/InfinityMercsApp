using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using InfinityMercsApp.Domain.Utilities;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;
using Resume = InfinityMercsApp.Domain.Models.Army.Resume;

namespace InfinityMercsApp.ViewModels;

public partial class ViewerViewModel
{
    private void ResetFireteamCounts()
    {
        FireteamDuoCount = "-";
        FireteamHarisCount = "-";
        FireteamCoreCount = "-";
        Fireteams.Clear();
        FireteamsStatus = "No fireteams available.";
    }

    private void UpdateFireteamCounts(FireteamChart? fireteamChart)
    {
        var chart = fireteamChart ?? FireteamChart.Empty;
        FireteamDuoCount = chart.SpecDisplay("DUO");
        FireteamHarisCount = chart.SpecDisplay("HARIS");
        FireteamCoreCount = chart.SpecDisplay("CORE");
    }

    private void UpdateFireteamTeams(
        FireteamChart? fireteamChart,
        bool mercsOnlyFilterEnabled,
        IReadOnlySet<string>? allowedUnitSlugs = null,
        IReadOnlySet<string>? allowedUnitNames = null)
    {
        Fireteams.Clear();
        FireteamsStatus = "No fireteams available.";

        foreach (var team in (fireteamChart ?? FireteamChart.Empty).Teams)
        {
            var unitLimits = team.Units
                .Select(unit => new FireteamUnitLimitItem
                {
                    Name = unit.ViewerName,
                    Slug = unit.Slug ?? string.Empty,
                    Min = (unit.Min ?? 0).ToString(CultureInfo.InvariantCulture),
                    Max = (unit.Max ?? 0).ToString(CultureInfo.InvariantCulture)
                })
                .ToList();
            if (mercsOnlyFilterEnabled)
            {
                unitLimits = unitLimits
                    .Where(x => IsAllowedFireteamUnit(x, allowedUnitSlugs, allowedUnitNames))
                    .ToList();
                if (unitLimits.Count == 0)
                {
                    continue;
                }
            }

            Fireteams.Add(new FireteamTeamItem
            {
                Name = team.ViewerName,
                TeamTypes = string.IsNullOrWhiteSpace(team.TypesDisplay) ? "-" : team.TypesDisplay,
                UnitLimits = unitLimits
            });
        }

        FireteamsStatus = Fireteams.Count == 0
            ? "No fireteams available."
            : $"{Fireteams.Count} fireteams loaded.";
    }

    private static bool IsAllowedFireteamUnit(
        FireteamUnitLimitItem unit,
        IReadOnlySet<string>? allowedUnitSlugs,
        IReadOnlySet<string>? allowedUnitNames)
    {
        if (!string.IsNullOrWhiteSpace(unit.Slug) &&
            allowedUnitSlugs is not null &&
            allowedUnitSlugs.Contains(unit.Slug))
        {
            return true;
        }

        if (allowedUnitNames is not null &&
            allowedUnitNames.Contains(NormalizeFireteamUnitName(unit.Name)))
        {
            return true;
        }

        return false;
    }

    private static string NormalizeFireteamUnitName(string value)
    {
        return Regex.Replace(value, @"\s+", " ").Trim().ToUpperInvariant();
    }

    private void ApplyFactionFilter()
    {
        IEnumerable<ViewerFactionItem> filtered = _allFactions;
        if (_factionFilterMode == FactionFilterMode.Factions)
        {
            filtered = filtered.Where(x => x.ParentId <= 0 || x.Id == x.ParentId);
        }
        else if (_factionFilterMode == FactionFilterMode.Sectorials)
        {
            filtered = filtered.Where(x => x.ParentId > 0 && x.Id != x.ParentId);
        }

        var filteredList = filtered.ToList();

        Factions.Clear();
        foreach (var faction in filteredList)
        {
            Factions.Add(faction);
        }

        if (SelectedFaction is not null && !filteredList.Contains(SelectedFaction))
        {
            SelectedFaction = null;
            Units.Clear();
            UnitsStatus = "Select a faction.";
        }
    }

    private void ResetUnitDetails()
    {
        Profiles.Clear();
        ProfilesStatus = "Select a unit.";
        UnitNameHeading = "Select a unit";
        ResetUnitStats();
        EquipmentSummary = "Equipment: -";
        SpecialSkillsSummary = "Special Skills: -";
        _currentEquipmentLookup = new Dictionary<int, string>();
        _currentEquipmentLinks = new Dictionary<int, string>();
        _currentSkillsLookup = new Dictionary<int, string>();
        _currentSkillsLinks = new Dictionary<int, string>();
    }

    private void ResetUnitStats()
    {
        _unitMoveFirstCm = null;
        _unitMoveSecondCm = null;
        ShowRegularOrderIcon = false;
        ShowIrregularOrderIcon = false;
        ShowImpetuousIcon = false;
        ShowTacticalAwarenessIcon = false;
        ShowCubeIcon = false;
        ShowCube2Icon = false;
        ShowHackableIcon = false;
        ImpetuousIconUrl = null;
        TacticalAwarenessIconUrl = null;
        CubeIconUrl = null;
        Cube2IconUrl = null;
        HackableIconUrl = null;
        UnitMov = "-";
        UnitCc = "-";
        UnitBs = "-";
        UnitPh = "-";
        UnitWip = "-";
        UnitArm = "-";
        UnitBts = "-";
        UnitVitalityHeader = "VITA";
        UnitVitality = "-";
        UnitS = "-";
        UnitAva = "-";
    }

    private static string BuildUnitSubtitle(
        Resume unit,
        IReadOnlyDictionary<int, string> typeLookup,
        IReadOnlyDictionary<int, string> categoryLookup)
    {
        var typeName = unit.Type.HasValue && typeLookup.TryGetValue(unit.Type.Value, out var t)
            ? t
            : (unit.Type?.ToString() ?? "?");

        var categoryName = unit.Category.HasValue && categoryLookup.TryGetValue(unit.Category.Value, out var c)
            ? c
            : (unit.Category?.ToString() ?? "?");

        return $"{typeName} - {categoryName}";
    }

}
