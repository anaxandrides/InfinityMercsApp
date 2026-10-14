namespace InfinityMercsApp.Domain.Models.Army.Profiles;

/// <summary>How many times each fireteam type appears.</summary>
public sealed record FireteamTypeCounts(int Duo, int Haris, int Core)
{
    public static FireteamTypeCounts Of(IEnumerable<string>? types)
    {
        var parsed = (types ?? []).Select(type => FireteamTypes.TryParse(type, out var parsedType) ? parsedType : (FireteamType?)null).ToList();
        return new FireteamTypeCounts(
            parsed.Count(type => type == FireteamType.Duo),
            parsed.Count(type => type == FireteamType.Haris),
            parsed.Count(type => type == FireteamType.Core));
    }
}
