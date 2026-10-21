using InfinityMercsApp.Services.CompanySelection.Units;

namespace InfinityMercsApp.Services.CompanySelection.Teams;

/// <summary>Matching a fireteam's slots to the units a selection screen lists.</summary>
public static class TeamSlots
{
    /// <summary>The listed unit a slot stands for: the one it was matched to, else by slug, else by name.</summary>
    public static TUnit? UnitFor<TUnit>(IReadOnlyList<TUnit> units, ITeamSlot slot)
        where TUnit : class, ISelectionUnit
    {
        if (slot.ResolvedUnitId is int unitId && slot.ResolvedSourceFactionId is int factionId &&
            units.FirstOrDefault(x => x.Id == unitId && x.SourceFactionId == factionId) is { } resolved)
        {
            return resolved;
        }

        if (!string.IsNullOrWhiteSpace(slot.Slug) &&
            units.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Slug) &&
                                      string.Equals(x.Slug.Trim(), slot.Slug.Trim(), StringComparison.OrdinalIgnoreCase)) is { } bySlug)
        {
            return bySlug;
        }

        return units.FirstOrDefault(x => string.Equals(x.Name, slot.Name, StringComparison.OrdinalIgnoreCase));
    }
}
