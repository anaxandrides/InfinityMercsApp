namespace InfinityMercsApp.Infrastructure.Json.Dtos;

/// <summary>
/// One item of an entry list such as <c>skills</c> or <c>weapons</c>. The API sends either a bare id
/// (<c>5</c>, <c>"5"</c>) or an object (<c>{"id":5,"q":2,"order":1,"extra":[7]}</c>).
/// </summary>
internal sealed class ProfileEntryDto
{
    public int? Id { get; init; }

    /// <summary>True when the entry was a JSON object (only objects carry q/order/extra).</summary>
    public bool IsObject { get; init; }

    public JsonScalar Q { get; init; }

    public JsonScalar Order { get; init; }

    /// <summary>The <c>extra</c> ids; <c>null</c> when the entry has no <c>extra</c> array.</summary>
    public List<int?>? Extra { get; init; }
}
