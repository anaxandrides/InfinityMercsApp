namespace InfinityMercsApp.Services.CompanySelection.Teams;

public sealed class CompanyUnitVisibilityLookupContext
{
    public Dictionary<int, UnitFilterLookups> LookupsByFactionId { get; } = [];
    public Dictionary<int, IReadOnlyDictionary<int, string>> TypeByFactionId { get; } = [];
}
