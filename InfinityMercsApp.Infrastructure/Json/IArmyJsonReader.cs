using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.Fireteams;
using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Domain.Models.Army.SpecOps;

namespace InfinityMercsApp.Infrastructure.Json;

/// <summary>
/// The single place that turns stored Infinity Army JSON (the SQLite blob columns such as
/// <c>ProfileGroupsJson</c> and <c>FiltersJson</c>) into typed domain models.
/// Missing or malformed JSON reads as an empty model; it never throws.
/// </summary>
public interface IArmyJsonReader
{
    /// <summary>Reads a unit's <c>ProfileGroupsJson</c>.</summary>
    UnitProfileGroups ReadProfileGroups(string? profileGroupsJson);

    /// <summary>Reads a unit's own <c>FiltersJson</c> (the ids it can field).</summary>
    UnitFilterIds ReadUnitFilters(string? unitFiltersJson);

    /// <summary>Reads an army's <c>FiltersJson</c> (the id → name/link tables).</summary>
    ArmyFilterCatalog ReadArmyFilters(string? armyFiltersJson);

    /// <summary>Reads a unit's <c>FactionsJson</c>: integer and numeric-string ids of a JSON array.</summary>
    IReadOnlyList<int> ReadFactionIds(string? factionsJson);

    /// <summary>Reads an army's <c>FireteamChartJson</c>.</summary>
    FireteamChart ReadFireteamChart(string? fireteamChartJson);

    /// <summary>
    /// Reads a stored id array such as a spec-ops choice's <c>ExtrasJson</c>: numbers, numeric strings and
    /// <c>{"id":..}</c> objects, in order; anything else is skipped.
    /// </summary>
    IReadOnlyList<int> ReadIdList(string? idListJson);

    /// <summary>Reads a unit's own <c>options</c> array (options declared outside its profile groups).</summary>
    IReadOnlyList<ProfileOption> ReadUnitOptions(string? unitOptionsJson);

    /// <summary>Reads an army's <c>relations</c> array.</summary>
    IReadOnlyList<ArmyRelation> ReadRelations(string? relationsJson);

    /// <summary>Reads an army's <c>reinforcements</c> value: the id of the army that provides them.</summary>
    int? ReadReinforcements(string? reinforcementsJson);

    /// <summary>Reads an army's <c>specops</c> object.</summary>
    SpecopsCatalog ReadSpecops(string? specopsJson);

    /// <summary>
    /// Reads a whole army payload as the API sends it. Returns <c>null</c> when the text is not a JSON object.
    /// </summary>
    ArmyDefinition? ReadArmyPayload(string? armyPayloadJson);
}
