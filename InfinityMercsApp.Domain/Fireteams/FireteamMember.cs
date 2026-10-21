namespace InfinityMercsApp.Domain.Fireteams;

/// <summary>One trooper of the company, as matched against fireteam slots.</summary>
public sealed record FireteamMember(string? BaseUnitName, string? Name, int UnitId, int FactionId);
