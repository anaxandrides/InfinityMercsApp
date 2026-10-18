using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Company;
using InfinityMercsApp.Domain.Lieutenants;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Services.CompanySelection.Captain;
using InfinityMercsApp.Services.CompanySelection.Factions;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.Records;
using TagGen = InfinityMercsApp.Infrastructure.Providers.CompanyFactions.TagCompanyFactionGenerator;

namespace InfinityMercsApp.Services.CompanySelection.StartCompany;

/// <summary>
/// Founds a company: the captain (or a TAG company's lieutenant TAG) is upgraded through its popup, then the
/// company is written to a new file in the companies folder.
/// </summary>
public sealed class CompanyFounder(CaptainSetup captains, SavedEntryWriter entries, RecordPaths paths)
{
    /// <summary>The company type a TAG Company is saved with; its TAG, when captain, gets the TAG Spec-Ops popup.</summary>
    public const string TagCompanyType = "TAG Company";

    public async Task<FoundingResult> FoundAsync<TFaction, TEntry>(CompanyFoundingRequest<TFaction, TEntry> request)
        where TFaction : class, ICompanySourceFaction
        where TEntry : class, ICompanyMercsEntry
    {
        var troopers = request.MercsCompanyEntries.ToList();
        var captain = troopers.FirstOrDefault(x => x.IsLieutenant) ?? troopers.FirstOrDefault();
        if (captain is null)
        {
            return FoundingResult.NoTroopers;
        }

        var sourceFactions = SourceFactions(request);
        var stats = await ConfigureCaptainAsync(request, captain, sourceFactions);
        if (stats is null)
        {
            return FoundingResult.CaptainCancelled;
        }

        var saveDir = paths.CompaniesDirectory;
        var filePath = Path.Combine(saveDir, NextCompanyFileName(saveDir, request.CompanyName));
        await WriteCompanyFileAsync(request, troopers, sourceFactions, stats, filePath);
        return FoundingResult.Founded(filePath);
    }

    /// <summary>The next free <c>{name}-{index:D4}.json</c> file name for a company in <paramref name="saveDir"/>.</summary>
    public static string NextCompanyFileName(string saveDir, string companyName)
    {
        var trimmedName = companyName.Trim();
        var safeFileName = Regex.Replace(trimmedName.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            safeFileName = "company";
        }

        Directory.CreateDirectory(saveDir);
        var companyIndex = NextCompanyIndex(saveDir, trimmedName, safeFileName);
        return $"{safeFileName}-{companyIndex:D4}.json";
    }

    private static List<TFaction> SourceFactions<TFaction, TEntry>(CompanyFoundingRequest<TFaction, TEntry> request)
        where TFaction : class, ICompanySourceFaction
        where TEntry : class, ICompanyMercsEntry =>
        FactionSlots.SourceFactions(request.ShowRightSelectionBox, request.LeftSlotFaction, request.RightSlotFaction).ToList();

    // A TAG Company's TAG captain gets the TAG Spec-Ops popup (its 20 XP, no more); any other captain gets the captain popup.
    private Task<SavedImprovedCaptainStats?> ConfigureCaptainAsync<TFaction, TEntry>(
        CompanyFoundingRequest<TFaction, TEntry> request,
        TEntry captain,
        IReadOnlyList<TFaction> sourceFactions)
        where TFaction : class, ICompanySourceFaction
        where TEntry : class, ICompanyMercsEntry
    {
        var skillLines = SavedEntryWriter.SplitCodes(captain.SavedSkills);
        if (captain.IsLieutenant)
        {
            LieutenantRules.AddSkillIfMissing(skillLines);
        }

        var unit = new CaptainUnitPopupInfo
        {
            Name = captain.Name,
            Cost = captain.CostValue,
            Statline = captain.Subtitle ?? "-",
            RangedWeapons = captain.SavedRangedWeapons,
            CcWeapons = captain.SavedCcWeapons,
            Skills = skillLines.Count == 0 ? "-" : string.Join(Environment.NewLine, skillLines),
            Equipment = captain.SavedEquipment,
            CachedLogoPath = captain.CachedLogoPath,
            PackagedLogoPath = captain.PackagedLogoPath
        };
        var known = request.Factions.Cast<ICompanySourceFaction>().ToList();
        var firstSourceFactionId = sourceFactions.FirstOrDefault()?.Id;

        if (!string.Equals(request.CompanyType, TagCompanyType, StringComparison.OrdinalIgnoreCase) || !UnitTypeCodes.IsTag(captain.UnitTypeCode))
        {
            return captains.ConfigureCaptainAsync(
                unit,
                new CaptainFactions(captain.SourceFactionId, firstSourceFactionId, known),
                captain.IsSpecOps,
                ProxyPack.Is(request.CompanyType),
                request.ShowUnitsInInches);
        }

        var otherFactionId = sourceFactions.Select(x => x.Id).FirstOrDefault(id => id != TagGen.TagCompanyFactionId);
        return captains.ConfigureTagSpecOpsAsync(
            unit,
            new CaptainFactions(captain.SourceFactionId, firstSourceFactionId, known, otherFactionId > 0 ? otherFactionId : null),
            captain.IsLieutenant,
            baseExperience: 20,
            request.ShowUnitsInInches);
    }

    // Writes the company to filePath with the captain stats already chosen.
    private async Task WriteCompanyFileAsync<TFaction, TEntry>(
        CompanyFoundingRequest<TFaction, TEntry> request,
        IList<TEntry> troopers,
        IReadOnlyList<TFaction> sourceFactions,
        SavedImprovedCaptainStats improvedCaptainStats,
        string filePath)
        where TFaction : class, ICompanySourceFaction
        where TEntry : class, ICompanyMercsEntry
    {
        var outputDir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        var fileName = Path.GetFileName(filePath);
        var captainName = improvedCaptainStats.CaptainName;
        var normalizedCaptainName = string.IsNullOrWhiteSpace(captainName) ? "Captain" : captainName.Trim();
        var startSeasonPoints = int.TryParse(request.SelectedStartSeasonPoints, out var parsedStartSeasonPoints)
            ? parsedStartSeasonPoints
            : 0;
        var payload = new
        {
            CompanyName = request.CompanyName.Trim(),
            CompanyType = request.CompanyType,
            CompanyIdentifier = CompanyIdentifier(fileName),
            CompanyIndex = ParseCompanyIndexFromFileName(filePath),
            CreatedUtc = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            StartSeasonPoints = startSeasonPoints,
            PointsLimit = startSeasonPoints,
            CurrentPoints = int.TryParse(request.SeasonPointsCapText, out var currentPoints) ? currentPoints : 0,
            SourceFactions = sourceFactions
                .Select(faction => new
                {
                    FactionId = faction.Id,
                    FactionName = faction.Name
                })
                .ToList(),
            Entries = entries.Build(troopers, normalizedCaptainName, improvedCaptainStats)
        };

        await File.WriteAllTextAsync(
            filePath,
            JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            }));
    }

    // The SHA-256 of the file name, in hex: a company's id for the seasons that play it.
    private static string CompanyIdentifier(string fileName) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fileName)));

    // One more than the highest index of a company of the same name: read from each file, else from its name.
    private static int NextCompanyIndex(string saveDir, string companyName, string safeFileName)
    {
        var maxIndex = 0;
        foreach (var file in Directory.EnumerateFiles(saveDir, "*.json"))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(file));
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
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                // A malformed or unreadable file still counts by its name.
            }

            var match = Regex.Match(Path.GetFileNameWithoutExtension(file), $"^{Regex.Escape(safeFileName)}-(\\d+)$", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var fallbackIndex))
            {
                maxIndex = Math.Max(maxIndex, fallbackIndex);
            }
        }

        return maxIndex + 1;
    }

    private static int ParseCompanyIndexFromFileName(string filePath)
    {
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
        if (string.IsNullOrWhiteSpace(fileNameWithoutExtension))
        {
            return 1;
        }

        var match = Regex.Match(fileNameWithoutExtension, @"-(\d+)$");
        return match.Success && int.TryParse(match.Groups[1].Value, out var parsed) && parsed > 0
            ? parsed
            : 1;
    }
}
