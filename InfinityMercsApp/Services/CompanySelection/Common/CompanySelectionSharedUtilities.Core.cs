using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace InfinityMercsApp.Views.Common;

/// <summary>
/// Shared static helpers used by Standard and Cohesive company selection pages.
/// </summary>
internal static partial class CompanySelectionSharedUtilities
{
    internal static int? ParseAvaLimit(string? ava)
    {
        if (string.IsNullOrWhiteSpace(ava))
        {
            return null;
        }

        var trimmed = ava.Trim();
        if (trimmed is "-" or "T")
        {
            return null;
        }

        if (!int.TryParse(trimmed, out var parsed))
        {
            return null;
        }

        // App convention: 255 means Total (no cap).
        return parsed >= 255 ? null : parsed;
    }

    internal static string ComputeCompanyIdentifier(string fileName)
    {
        var bytes = Encoding.UTF8.GetBytes(fileName);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    internal static int GetNextCompanyIndex(string saveDir, string companyName, string safeFileName)
    {
        var maxIndex = 0;
        var files = Directory.EnumerateFiles(saveDir, "*.json");

        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("CompanyName", out var nameElement) ||
                    !string.Equals(nameElement.GetString(), companyName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (doc.RootElement.TryGetProperty("CompanyIndex", out var indexElement) &&
                    indexElement.ValueKind == JsonValueKind.Number &&
                    indexElement.TryGetInt32(out var parsedIndex))
                {
                    maxIndex = Math.Max(maxIndex, parsedIndex);
                    continue;
                }
            }
            catch
            {
                // Ignore malformed records and continue.
            }

            var baseName = Path.GetFileNameWithoutExtension(file);
            var match = Regex.Match(baseName, $"^{Regex.Escape(safeFileName)}-(\\d+)$", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var fallbackIndex))
            {
                maxIndex = Math.Max(maxIndex, fallbackIndex);
            }
        }

        return maxIndex + 1;
    }

    /// <summary>
    /// Id → name for one section of an army's filters (a fresh, mutable copy of the catalogue's lookup).
    /// </summary>
    internal static Dictionary<int, string> BuildIdNameLookup(ArmyFilterCatalog? filters, string sectionName) =>
        new((filters ?? ArmyFilterCatalog.Empty).Names(sectionName));
}
