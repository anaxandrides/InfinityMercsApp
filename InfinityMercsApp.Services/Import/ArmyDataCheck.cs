using System.Text;
using InfinityMercsApp.Domain.Models.Metadata;

namespace InfinityMercsApp.Services.Import;

/// <summary>
/// What the Infinity Army API has, compared with what is stored: either the download failed, or it holds the
/// metadata and armies with which of them would change the stored data. Nothing is stored by checking.
/// </summary>
public sealed class ArmyDataCheck
{
    private const int ListedUpdates = 8;

    private ArmyDataCheck(
        string failureMessage,
        MetadataDocument? metadata,
        bool metadataChanged,
        IReadOnlyList<DownloadedArmy> armies,
        IReadOnlyList<string> failedFactions)
    {
        FailureMessage = failureMessage;
        Metadata = metadata;
        MetadataChanged = metadataChanged;
        Armies = armies;
        FailedFactions = failedFactions;
        Updates = armies.Where(army => army.IsUpdate).ToList();
    }

    /// <summary>True when the metadata and at least one army were downloaded.</summary>
    public bool Succeeded => Metadata is not null;

    /// <summary>Why the download failed; empty when it succeeded.</summary>
    public string FailureMessage { get; }

    public MetadataDocument? Metadata { get; }

    /// <summary>True when the downloaded metadata differs from the stored metadata.</summary>
    public bool MetadataChanged { get; }

    /// <summary>Every army downloaded.</summary>
    public IReadOnlyList<DownloadedArmy> Armies { get; }

    /// <summary>The armies that would change what is stored.</summary>
    public IReadOnlyList<DownloadedArmy> Updates { get; }

    /// <summary>The factions whose army could not be downloaded.</summary>
    public IReadOnlyList<string> FailedFactions { get; }

    public bool HasUpdates => MetadataChanged || Updates.Count > 0;

    /// <summary>The changes, one per line: the metadata, then each army's stored and new version.</summary>
    public string UpdateSummary
    {
        get
        {
            var summary = new StringBuilder();
            if (MetadataChanged)
            {
                summary.AppendLine("• Factions, weapons, skills and equipment");
            }

            foreach (var army in Updates.OrderBy(army => army.FactionName, StringComparer.OrdinalIgnoreCase).Take(ListedUpdates))
            {
                summary.AppendLine(army.StoredVersion is { } stored
                    ? $"• {army.FactionName}: {stored} → {army.Version}"
                    : $"• {army.FactionName}: new ({army.Version})");
            }

            if (Updates.Count > ListedUpdates)
            {
                summary.AppendLine($"• …and {Updates.Count - ListedUpdates} more armies");
            }

            return summary.ToString().TrimEnd();
        }
    }

    public static ArmyDataCheck Failed(string message) => new(message, null, false, [], []);

    public static ArmyDataCheck Downloaded(
        MetadataDocument metadata,
        bool metadataChanged,
        IReadOnlyList<DownloadedArmy> armies,
        IReadOnlyList<string> failedFactions) =>
        new(string.Empty, metadata, metadataChanged, armies, failedFactions);
}
