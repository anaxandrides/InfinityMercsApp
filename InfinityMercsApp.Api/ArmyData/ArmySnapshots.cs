using System.Security.Cryptography;
using System.Text;
using InfinityMercsApp.Infrastructure.API.ArmyMirror;

namespace InfinityMercsApp.Api.ArmyData;

/// <summary>Snapshot ids, and finding a payload's hash in a snapshot.</summary>
public static class ArmySnapshots
{
    private const int IdLength = 16;

    /// <summary>
    /// The id of the snapshot holding these payloads: the first 16 hex digits of a SHA-256 over the metadata's hash
    /// and each army's faction and hash, so the same army data always gets the same id.
    /// </summary>
    public static string IdFor(MirroredPayload? metadata, IEnumerable<MirroredArmy> armies)
    {
        var text = new StringBuilder(metadata?.Hash ?? string.Empty).Append('\n');
        foreach (var army in armies.OrderBy(army => army.FactionId))
        {
            text.Append(army.FactionId).Append(':').Append(army.Hash).Append('\n');
        }

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())))[..IdLength];
    }

    /// <summary>Whether <paramref name="text"/> could be a snapshot id: 16 lower-case hex digits.</summary>
    public static bool IsId(string text) => text.Length == IdLength && text.All(char.IsAsciiHexDigitLower);

    /// <summary>The hash of the payload <paramref name="request"/> asks for, or null when the snapshot doesn't hold it.</summary>
    public static string? HashOf(ArmyMirrorManifest snapshot, PayloadRequest request) =>
        request.FactionId is { } factionId
            ? snapshot.Armies.FirstOrDefault(army => army.FactionId == factionId)?.Hash
            : snapshot.Metadata?.Hash;
}
