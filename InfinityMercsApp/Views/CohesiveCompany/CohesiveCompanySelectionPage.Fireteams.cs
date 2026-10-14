using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Domain.Models.Filtering;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Common;
using Svg.Skia;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;

namespace InfinityMercsApp.Views.CohesiveCompany;

public partial class CohesiveCompanySelectionPage
{
    private Task<List<string>> EvaluateValidCoreFireteamsForFactionAsync(
        FactionRecord faction,
        int maxCost,
        CancellationToken cancellationToken)
    {
        var validCoreFireteams = new List<string>();
        var snapshot = GetFactionSnapshotFromProvider(faction.Id, cancellationToken);
        if (snapshot is null || snapshot.FireteamChart is null)
        {
            return Task.FromResult(validCoreFireteams);
        }

        var typeLookup = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot.Filters, ArmyFilterSection.Type);
        var categoryLookup = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot.Filters, ArmyFilterSection.Category);
        var units = GetResumeByFactionMercsOnlyFromProvider(faction.Id, cancellationToken);

        return Task.FromResult(
            CohesiveCompanyTeamEligibilityWorkflow.EvaluateValidCoreFireteams(
                snapshot.FireteamChart,
                units,
                UnitFilterLookups.From(snapshot.Filters),
                typeLookup,
                _filterState.ActiveUnitFilter,
                maxCost,
                unit => IsCharacterCategory(unit, categoryLookup),
                unitId => GetUnitFromProvider(faction.Id, unitId, cancellationToken)));
    }

    private void ResetMercsCompany()
    {
        ResetMercsCompanyCore(
            MercsCompanyEntries,
            UpdateMercsCompanyTotal,
            ReevaluateTrackedFireteamLevel);
    }

    private void OnTeamTrackingRadioButtonCheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (_isUpdatingTrackedTeamSelection || !e.Value)
        {
            return;
        }

        if (sender is not RadioButton radioButton || radioButton.BindingContext is not ArmyTeamListItem team)
        {
            return;
        }

        if (!team.ShowTrackingRadioButton)
        {
            return;
        }

        SetTrackedFireteamSelection(team.Name);
    }

    private void RestoreTrackedFireteamSelection(string? trackedFireteamName)
    {
        if (string.IsNullOrWhiteSpace(trackedFireteamName))
        {
            SetTrackedFireteamSelection(GetDefaultTrackedFireteamName());
            return;
        }

        var matchingTeam = TeamEntries.FirstOrDefault(x =>
            x.ShowTrackingRadioButton &&
            string.Equals(x.Name, trackedFireteamName, StringComparison.OrdinalIgnoreCase));
        SetTrackedFireteamSelection(matchingTeam?.Name ?? GetDefaultTrackedFireteamName());
    }

    private string GetDefaultTrackedFireteamName()
    {
        var firstVisibleTeam = TeamEntries.FirstOrDefault(x => x.ShowTrackingRadioButton && x.IsVisible);
        if (firstVisibleTeam is not null)
        {
            return firstVisibleTeam.Name;
        }

        var firstTrackableTeam = TeamEntries.FirstOrDefault(x => x.ShowTrackingRadioButton);
        return firstTrackableTeam?.Name ?? string.Empty;
    }

    private void SetTrackedFireteamSelection(string? teamName)
    {
        var normalizedTeamName = teamName?.Trim() ?? string.Empty;
        var hasSelection = !string.IsNullOrWhiteSpace(normalizedTeamName);

        _isUpdatingTrackedTeamSelection = true;
        try
        {
            foreach (var team in TeamEntries)
            {
                team.IsTrackedTeam = team.ShowTrackingRadioButton &&
                                     hasSelection &&
                                     string.Equals(team.Name, normalizedTeamName, StringComparison.OrdinalIgnoreCase);
            }
        }
        finally
        {
            _isUpdatingTrackedTeamSelection = false;
        }

        if (string.Equals(_trackedFireteamName, normalizedTeamName, StringComparison.Ordinal))
        {
            ReevaluateTrackedFireteamLevel();
            return;
        }

        _trackedFireteamName = normalizedTeamName;
        OnPropertyChanged(nameof(TrackedFireteamNameDisplay));
        ReevaluateTrackedFireteamLevel();
    }

    private void ReevaluateTrackedFireteamLevel()
    {
        var evaluatedLevel = EvaluateTrackedFireteamLevel();
        OnTrackedFireteamLevelEvaluated(evaluatedLevel);
        ReevaluateIrregularStatus();
    }

    private int EvaluateTrackedFireteamLevel()
    {
        var slots = TrackedFireteamSlots();
        return slots is null ? 0 : FireteamLevels.Level(TrackedFireteamMembers(), slots);
    }

    private void ReevaluateIrregularStatus()
    {
        if (MercsCompanyEntries.Count == 0)
        {
            return;
        }

        // Without a tracked fireteam nobody is irregular.
        var slots = TrackedFireteamSlots();
        var claimed = slots is null ? null : FireteamLevels.ClaimSlots(TrackedFireteamMembers(), slots);
        for (var i = 0; i < MercsCompanyEntries.Count; i++)
        {
            MercsCompanyEntries[i].IsIrregular = claimed is not null && !claimed[i];
        }
    }

    // The tracked team's slots plus every wildcard bucket's, or null when no fireteam is tracked.
    private List<FireteamSlot>? TrackedFireteamSlots()
    {
        if (string.IsNullOrWhiteSpace(_trackedFireteamName))
        {
            return null;
        }

        var trackedTeam = TeamEntries.FirstOrDefault(x =>
            x.ShowTrackingRadioButton &&
            string.Equals(x.Name, _trackedFireteamName, StringComparison.OrdinalIgnoreCase));
        if (trackedTeam is null)
        {
            return null;
        }

        return trackedTeam.AllowedProfiles
            .Concat(TeamEntries
                .Where(x => x.IsWildcardBucket)
                .SelectMany(x => x.AllowedProfiles))
            .DistinctBy(x => $"{x.Name}|{x.ResolvedUnitId}|{x.ResolvedSourceFactionId}", StringComparer.OrdinalIgnoreCase)
            .Select(x => new FireteamSlot(x.Name, x.ResolvedUnitId, x.ResolvedSourceFactionId, x.Max))
            .ToList();
    }

    private List<FireteamMember> TrackedFireteamMembers() =>
        MercsCompanyEntries
            .Select(entry => new FireteamMember(entry.BaseUnitName, entry.Name, entry.SourceUnitId, entry.SourceFactionId))
            .ToList();

    // Hook point for full fireteam-level evaluation logic. Provide an evaluated value in [1..6].
    private void OnTrackedFireteamLevelEvaluated(int fireteamLevel)
    {
        SetTrackedFireteamLevel(fireteamLevel);
    }

    private void SetTrackedFireteamLevel(int fireteamLevel)
    {
        var normalizedLevel = Math.Clamp(fireteamLevel, 0, FireteamLevels.MaxLevel);
        if (_trackedFireteamLevel == normalizedLevel)
        {
            return;
        }

        _trackedFireteamLevel = normalizedLevel;
        OnPropertyChanged(nameof(TrackedFireteamLevelBonusText));
        OnPropertyChanged(nameof(HasTrackedFireteamLevelBonus));
        _ = LoadTrackedFireteamLevelIconAsync(_trackedFireteamLevel);
    }

    private async Task LoadTrackedFireteamLevelIconAsync(int fireteamLevel)
    {
        _trackedFireteamLevelPicture?.Dispose();
        _trackedFireteamLevelPicture = null;

        if (fireteamLevel >= 1 && fireteamLevel <= FireteamLevels.MaxLevel)
        {
            try
            {
                var iconPath = $"SVGCache/NonCBIcons/Fireteam/noun-team-{fireteamLevel}.svg";
                await using var stream = await FileSystem.Current.OpenAppPackageFileAsync(iconPath);
                var svg = new SKSvg();
                _trackedFireteamLevelPicture = svg.Load(stream);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"CompanySelectionPage tracked fireteam icon load failed: {ex.Message}");
            }
        }

        TrackedFireteamLevelCanvas?.InvalidateSurface();
    }
}



