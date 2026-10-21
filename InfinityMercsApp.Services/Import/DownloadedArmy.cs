using InfinityMercsApp.Domain.Models.Army;

namespace InfinityMercsApp.Services.Import;

/// <summary>An army as downloaded, beside the version stored for it.</summary>
/// <param name="StoredVersion">The stored army's version; null when none is stored.</param>
/// <param name="IsUpdate">True when storing it would change what is stored: it is newer, or nothing usable is stored.</param>
public sealed record DownloadedArmy(int FactionId, string FactionName, ArmyDefinition Army, string? StoredVersion, bool IsUpdate)
{
    public string Version => Army.Version;
}
