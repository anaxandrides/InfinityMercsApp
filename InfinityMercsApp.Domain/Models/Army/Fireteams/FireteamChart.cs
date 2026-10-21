using System.Globalization;

namespace InfinityMercsApp.Domain.Models.Army.Fireteams;

/// <summary>
/// An army's <c>fireteamChart</c>: how many fireteams of each kind it may field and which teams exist.
/// </summary>
public sealed class FireteamChart
{
    public static FireteamChart Empty { get; } = new();

    /// <summary>The <c>spec</c> entries in payload order.</summary>
    public IReadOnlyList<FireteamSpecEntry> Spec { get; init; } = [];

    public IReadOnlyList<FireteamTeam> Teams { get; init; } = [];

    /// <summary>
    /// A spec count as shown in the viewer: the first entry whose key matches (ignoring case),
    /// <c>"T"</c> above 5 (unlimited), <c>"-"</c> when absent or not a number.
    /// </summary>
    public string SpecDisplay(FireteamType type)
    {
        var entry = Spec.FirstOrDefault(entry => string.Equals(entry.Key, type.Key(), StringComparison.OrdinalIgnoreCase));
        return entry?.Count is int count
            ? count > 5 ? "T" : count.ToString(CultureInfo.InvariantCulture)
            : "-";
    }
}
