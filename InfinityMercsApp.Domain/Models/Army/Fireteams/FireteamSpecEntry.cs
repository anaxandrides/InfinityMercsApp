namespace InfinityMercsApp.Domain.Models.Army.Fireteams;

/// <summary>One entry of a chart's <c>spec</c>, e.g. CORE 1; the count is null when not a number.</summary>
public sealed record FireteamSpecEntry(string Key, int? Count);
