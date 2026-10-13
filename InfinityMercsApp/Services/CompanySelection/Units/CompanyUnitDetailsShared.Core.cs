using System.Globalization;
using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;

namespace InfinityMercsApp.Views.Common;

internal static partial class CompanyUnitDetailsShared
{
    private const int CharacterCategoryId = 10;

    internal sealed record CompanyUnitStatProjection(
        int? MoveFirstCm,
        int? MoveSecondCm,
        string Mov,
        string Cc,
        string Bs,
        string Ph,
        string Wip,
        string Arm,
        string Bts,
        string S,
        string Ava,
        string VitalityHeader,
        string Vitality);

    internal static Dictionary<int, string> BuildIdNameLookup(ArmyFilterCatalog? filters, string sectionName)
    {
        return CompanySelectionSharedUtilities.BuildIdNameLookup(filters, sectionName);
    }

    internal static string ReplaceSubtitleMoveDisplay(string? subtitle, string moveDisplay)
    {
        return CompanySelectionSharedUtilities.ReplaceSubtitleMoveDisplay(subtitle, moveDisplay);
    }

    internal static string ExtractFirstPeripheralName(string? peripheralsText)
    {
        return CompanySelectionSharedUtilities.ExtractFirstPeripheralName(peripheralsText);
    }

    internal static string NormalizePeripheralNameForDedupe(string? value)
    {
        return CompanySelectionSharedUtilities.NormalizePeripheralNameForDedupe(value);
    }

    internal static int GetPeripheralTotalCount(IEnumerable<string> peripheralNames)
    {
        return CompanySelectionSharedUtilities.GetPeripheralTotalCount(peripheralNames);
    }

    internal static bool TryBuildSinglePeripheralDisplay(
        IReadOnlyList<string> peripheralNames,
        out string peripheralName,
        out int peripheralCount)
    {
        peripheralName = string.Empty;
        peripheralCount = 0;

        if (peripheralNames.Count != 1)
        {
            return false;
        }

        var only = peripheralNames[0];
        if (string.IsNullOrWhiteSpace(only) || only == "-")
        {
            return false;
        }

        var match = Regex.Match(only, @"^(.*)\((\d+)\)\s*$");
        if (!match.Success || !int.TryParse(match.Groups[2].Value, out peripheralCount))
        {
            return false;
        }

        peripheralName = ExtractFirstPeripheralName(match.Groups[1].Value);
        if (string.IsNullOrWhiteSpace(peripheralName))
        {
            return false;
        }

        return peripheralCount > 0;
    }

    internal static string BuildUnitSubtitle(
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

    internal static bool IsCharacterCategory(Resume unit, IReadOnlyDictionary<int, string> categoryLookup)
    {
        if (unit.Category.HasValue && unit.Category.Value == CharacterCategoryId)
        {
            return true;
        }

        if (unit.Category.HasValue && categoryLookup.TryGetValue(unit.Category.Value, out var categoryName))
        {
            return !string.IsNullOrWhiteSpace(categoryName) &&
                   categoryName.Contains("character", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    internal static async Task<Stream?> OpenBestUnitLogoStreamAsync(
        string unitName,
        int unitId,
        int sourceFactionId,
        IEnumerable<string?> cachedPathCandidates,
        IEnumerable<string?> packagedPathCandidates)
    {
        return await CompanyUnitLogoWorkflowService.OpenBestUnitLogoStreamAsync(
            unitName,
            unitId,
            sourceFactionId,
            cachedPathCandidates,
            packagedPathCandidates);
    }

    internal static bool MatchesClassificationFilter(
        UnitFilterCriteria activeUnitFilter,
        int? unitType,
        IReadOnlyDictionary<int, string> typeLookup)
    {
        return CompanyUnitFilterService.MatchesClassificationFilter(activeUnitFilter, unitType, typeLookup);
    }

    internal static Task ApplyGlobalDisplayUnitsPreferenceAsync(
        Func<bool> getShowUnitsInInches,
        bool showUnitsInInches,
        Action<bool> setShowUnitsInInches,
        Action updateUnitMoveDisplay,
        Action updatePeripheralMoveDisplay,
        Action refreshDistanceDisplays,
        Action<string> logError)
    {
        try
        {
            var showInches = getShowUnitsInInches();
            if (showUnitsInInches == showInches)
            {
                return Task.CompletedTask;
            }

            setShowUnitsInInches(showInches);
            updateUnitMoveDisplay();
            updatePeripheralMoveDisplay();
            refreshDistanceDisplays();
        }
        catch (Exception ex)
        {
            logError($"CompanySelectionPage ApplyGlobalDisplayUnitsPreferenceAsync failed: {ex.Message}");
        }

        return Task.CompletedTask;
    }
}
