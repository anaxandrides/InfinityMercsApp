namespace InfinityMercsApp.Domain.Fireteams;

/// <summary>A unit as fireteam slots are matched to it: by its id and faction, slug or name.</summary>
public interface IFireteamUnit
{
    int Id { get; }
    int SourceFactionId { get; }
    string Name { get; }
    string? Slug { get; }
}
