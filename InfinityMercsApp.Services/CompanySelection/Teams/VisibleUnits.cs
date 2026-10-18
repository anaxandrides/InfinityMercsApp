using InfinityMercsApp.Services.CompanySelection.Units;

namespace InfinityMercsApp.Services.CompanySelection.Teams;

/// <summary>The units a selection screen shows right now, for deciding which fireteam slots show.</summary>
public sealed class VisibleUnits
{
    private readonly IReadOnlyList<ISelectionUnit> _units;
    private readonly HashSet<string> _names;
    private readonly HashSet<string> _slugs;
    private readonly IReadOnlyList<string> _normalizedNames;

    private VisibleUnits(IReadOnlyList<ISelectionUnit> units)
    {
        _units = units;
        _names = new HashSet<string>(units.Select(x => x.Name), StringComparer.OrdinalIgnoreCase);
        _slugs = new HashSet<string>(
            units.Select(x => x.Slug?.Trim()).OfType<string>().Where(x => x.Length > 0),
            StringComparer.OrdinalIgnoreCase);
        _normalizedNames = units
            .Select(x => FireteamMatching.NormalizeTeamUnitName(x.Name))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>The visible ones of <paramref name="units"/>.</summary>
    public static VisibleUnits Of(IEnumerable<ISelectionUnit> units) => new(units.Where(x => x.IsVisible).ToList());

    /// <summary>
    /// Whether a slot shows: never a character's; a wildcard slot always, except in the wildcard bucket, where
    /// it shows like any other slot, when a unit it stands for is visible.
    /// </summary>
    public bool Shows(ITeamSlot slot, bool inWildcardBucket) =>
        !slot.IsCharacter &&
        FireteamMatching.IsVisibleTeamAllowedProfile(
            slot.Name,
            slot.Slug,
            _names,
            _slugs,
            _normalizedNames,
            treatWildcardAsAlwaysVisible: !inWildcardBucket,
            visibleUnits: _units,
            resolvedUnitId: slot.ResolvedUnitId,
            resolvedSourceFactionId: slot.ResolvedSourceFactionId,
            readUnitId: x => x.Id,
            readSourceFactionId: x => x.SourceFactionId);
}
