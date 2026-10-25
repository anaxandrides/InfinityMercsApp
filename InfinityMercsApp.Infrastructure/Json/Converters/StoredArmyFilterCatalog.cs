using InfinityMercsApp.Domain.Models.Army.Filters;

namespace InfinityMercsApp.Infrastructure.Json.Converters;

/// <summary>An <see cref="ArmyFilterCatalog"/>'s contents as <see cref="ArmyFilterCatalogConverter"/> keeps them.</summary>
public sealed record StoredArmyFilterCatalog(
    IReadOnlyDictionary<string, IReadOnlyList<ArmyFilterItem>> Sections,
    IReadOnlyDictionary<string, IReadOnlyList<ArmyFilterKeyedItem>> KeyedSections);
