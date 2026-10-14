namespace InfinityMercsApp.Domain.Fireteams;

/// <summary>One allowed-profile slot of a tracked fireteam: its name, the unit it resolved to (if any) and its Max.</summary>
public sealed record FireteamSlot(string? Name, int? UnitId, int? FactionId, string? Max);
