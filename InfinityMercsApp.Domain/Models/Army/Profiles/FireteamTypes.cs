namespace InfinityMercsApp.Domain.Models.Army.Profiles;

public static class FireteamTypes
{
    /// <summary>The type as the API writes it: "DUO", "HARIS" or "CORE".</summary>
    public static string Key(this FireteamType type) => type.ToString().ToUpperInvariant();

    /// <summary>Reads "DUO", "haris" and so on, ignoring case only.</summary>
    public static bool TryParse(string? key, out FireteamType type)
    {
        foreach (var candidate in Enum.GetValues<FireteamType>())
        {
            if (string.Equals(candidate.Key(), key, StringComparison.OrdinalIgnoreCase))
            {
                type = candidate;
                return true;
            }
        }

        type = default;
        return false;
    }
}
