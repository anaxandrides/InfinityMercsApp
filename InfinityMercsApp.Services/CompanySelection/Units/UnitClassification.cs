using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;

namespace InfinityMercsApp.Services.CompanySelection.Units;

/// <summary>A listed unit's troop type and category as the army data names them.</summary>
public static class UnitClassification
{
    private const int CharacterCategoryId = 10;

    /// <summary>"Type - Category", falling back to the ids (or "?") when the army data has no name for them.</summary>
    public static string Subtitle(
        ArmyResumeRecord unit, IReadOnlyDictionary<int, string> typeLookup, IReadOnlyDictionary<int, string> categoryLookup)
    {
        var typeName = unit.Type is int type && typeLookup.TryGetValue(type, out var t) ? t : unit.Type?.ToString() ?? "?";
        var categoryName = unit.Category is int category && categoryLookup.TryGetValue(category, out var c) ? c : unit.Category?.ToString() ?? "?";
        return $"{typeName} - {categoryName}";
    }

    /// <summary>Whether the unit is a character: category 10, or a category whose name says so.</summary>
    public static bool IsCharacter(ArmyResumeRecord unit, IReadOnlyDictionary<int, string> categoryLookup) =>
        unit.Category is int category &&
        (category == CharacterCategoryId ||
         (categoryLookup.TryGetValue(category, out var name) && name.Contains("character", StringComparison.OrdinalIgnoreCase)));
}
