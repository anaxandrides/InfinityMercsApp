namespace InfinityMercsApp.Infrastructure.Json;

/// <summary>An item a spec-ops choice grants: a bare id or <c>{"id":74,"extras":[304]}</c>.</summary>
internal sealed record SpecopsLinkDto(int Id, List<int> Extras);
