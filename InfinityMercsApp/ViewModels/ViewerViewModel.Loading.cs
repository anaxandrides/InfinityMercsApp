using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
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
    public async Task LoadFactionsAsync(CancellationToken cancellationToken = default)
    {
        await ApplyGlobalDisplayUnitsPreferenceAsync(cancellationToken);

        if (_armyDataService is null)
        {
            Status = "Metadata service unavailable.";
            return;
        }

        try
        {
            IsLoading = true;
            Status = "Loading factions...";
            var factions = _armyDataService.GetMetadataFactions(includeDiscontinued: true, cancellationToken);
            if (_factionLogoCacheService is not null)
            {
                var factionRecords = factions.Select(x => new FactionRecord
                {
                    Id = x.Id,
                    ParentId = x.ParentId,
                    Name = x.Name,
                    Slug = x.Slug,
                    Discontinued = x.Discontinued,
                    Logo = x.Logo
                });
                await _factionLogoCacheService.CacheFactionLogosFromRecordsAsync(factionRecords, cancellationToken);
            }

            _allFactions = factions.Select(faction => new ViewerFactionItem
            {
                Id = faction.Id,
                ParentId = faction.ParentId,
                Name = faction.Name,
                Logo = faction.Logo,
                CachedLogoPath = _factionLogoCacheService?.TryGetCachedLogoPath(faction.Id),
                PackagedLogoPath = _factionLogoCacheService?.GetPackagedFactionLogoPath(faction.Id)
                    ?? $"SVGCache/factions/{faction.Id}.svg"
            }).ToList();

            ApplyFactionFilter();

            Status = factions.Count == 0 ? "No factions available." : $"{factions.Count} factions loaded.";
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LoadFactionsAsync failed: {ex.Message}");
            Status = $"Failed to load factions: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Everything a faction switch needs, assembled off the UI thread so the tap never blocks on
    // ~53 per-unit DB reads + JSON filtering. Only strings/POCOs cross the thread boundary.
    private sealed record PreparedFactionUnits(
        string? FireteamChartJson,
        HashSet<string> AllowedFireteamSlugs,
        HashSet<string> AllowedFireteamNames,
        List<ViewerUnitItem> Units);

    public async Task LoadUnitsForSelectedFactionAsync(CancellationToken cancellationToken = default)
    {
        Units.Clear();
        SelectedUnit = null;
        ResetUnitDetails();
        ResetFireteamCounts();

        var faction = SelectedFaction;
        if (faction is null)
        {
            UnitsStatus = "Select a faction.";
            return;
        }

        if (_armyDataService is null)
        {
            UnitsStatus = "Army data service unavailable.";
            return;
        }

        try
        {
            UnitsStatus = "Loading units...";

            var armyDataService = _armyDataService;
            var logoCache = _factionLogoCacheService;
            var activeFilter = _activeUnitFilter;

            var prepared = await Task.Run(async () =>
            {
                var units = armyDataService.GetResumeByFaction(faction.Id, cancellationToken);

                // The SVG (and its warmed PNGs) must be on disk before we resolve CachedLogoPath;
                // the download/copy + eager raster now run on this worker thread, not the UI thread.
                if (logoCache is not null)
                {
                    var cacheResult = await logoCache
                        .CacheUnitLogosFromRecordsAsync(faction.Id, units, cancellationToken)
                        .ConfigureAwait(false);
                    Console.Error.WriteLine($"Unit cache for faction {faction.Id}: downloaded={cacheResult.Downloaded}, reused={cacheResult.CachedReuse}, failed={cacheResult.Failed}");
                }

                var snapshot = armyDataService.GetFactionSnapshot(faction.Id, cancellationToken);

                var allowedFireteamSlugs = units
                    .Select(x => x.Slug?.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var allowedFireteamNames = units
                    .Select(x => x.Name?.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => NormalizeFireteamUnitName(x!))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var typeLookup = BuildIdNameLookup(snapshot?.FiltersJson, "type");
                var charsLookup = BuildIdNameLookup(snapshot?.FiltersJson, "chars");
                var categoryLookup = BuildIdNameLookup(snapshot?.FiltersJson, "category");
                var skillsLookup = BuildIdNameLookup(snapshot?.FiltersJson, "skills");
                var equipLookup = BuildIdNameLookup(snapshot?.FiltersJson, "equip");
                var weaponsLookup = BuildIdNameLookup(snapshot?.FiltersJson, "weapons");
                var ammoLookup = BuildIdNameLookup(snapshot?.FiltersJson, "ammunition");

                var orderedUnits = units
                    .OrderBy(unit => ArmyUnitSort.GetUnitTypeSortIndex(unit.Type))
                    .ThenBy(unit => unit.Name, StringComparer.OrdinalIgnoreCase);

                var built = new List<ViewerUnitItem>();
                foreach (var unit in orderedUnits)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var unitRecord = armyDataService.GetUnit(faction.Id, unit.UnitId, cancellationToken);

                    if (!MatchesClassificationFilter(unit, typeLookup, activeFilter))
                    {
                        continue;
                    }

                    if (!UnitHasVisibleOptionWithFilter(
                            unitRecord?.ProfileGroupsJson,
                            skillsLookup,
                            charsLookup,
                            equipLookup,
                            weaponsLookup,
                            ammoLookup,
                            activeFilter,
                            requireLieutenant: activeFilter.LieutenantOnlyUnits,
                            requireZeroSwc: false))
                    {
                        continue;
                    }

                    built.Add(new ViewerUnitItem
                    {
                        Id = unit.UnitId,
                        Name = unit.Name,
                        Logo = unit.Logo,
                        Subtitle = BuildUnitSubtitle(unit, typeLookup, categoryLookup),
                        CachedLogoPath = logoCache?.TryGetCachedUnitLogoPath(faction.Id, unit.UnitId),
                        PackagedLogoPath = logoCache?.GetPackagedUnitLogoPath(faction.Id, unit.UnitId)
                            ?? $"SVGCache/units/{faction.Id}-{unit.UnitId}.svg"
                    });
                }

                return new PreparedFactionUnits(snapshot?.FireteamChartJson, allowedFireteamSlugs, allowedFireteamNames, built);
            }, cancellationToken);

            // A newer faction tap superseded this load while it ran; drop the stale result.
            if (!ReferenceEquals(SelectedFaction, faction))
            {
                return;
            }

            UpdateFireteamCounts(prepared.FireteamChartJson);
            UpdateFireteamTeams(
                prepared.FireteamChartJson,
                MercsOnlyUnits,
                prepared.AllowedFireteamSlugs,
                prepared.AllowedFireteamNames);

            Units.ReplaceAll(prepared.Units);
            UnitsStatus = prepared.Units.Count == 0 ? "No units available for this faction." : $"{prepared.Units.Count} units loaded.";
        }
        catch (OperationCanceledException)
        {
            // Faction switch cancelled; leave the list as the newer load will populate it.
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LoadUnitsForSelectedFactionAsync failed: {ex.Message}");
            UnitsStatus = $"Failed to load units: {ex.Message}";
        }
    }

}
