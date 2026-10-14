using InfinityMercsApp.Domain.Models.Filtering;
using ArmySpecopsUnitRecord = InfinityMercsApp.Domain.Models.Army.SpecopsUnit;

namespace InfinityMercsApp.Views.Common;

public sealed class CompanyUnitVisibilityLookupContext
{
    public Dictionary<int, UnitFilterLookups> LookupsByFactionId { get; } = [];
    public Dictionary<int, IReadOnlyDictionary<int, string>> TypeByFactionId { get; } = [];
    public Dictionary<int, Dictionary<int, ArmySpecopsUnitRecord>> SpecopsByFactionId { get; } = [];
}
