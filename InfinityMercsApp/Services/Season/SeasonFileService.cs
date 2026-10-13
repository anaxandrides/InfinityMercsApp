using InfinityMercsApp.Domain.Season;
using System.Text.Json;
using System.Text.Json.Serialization;
using InfinityMercsApp.Domain.Models.Season;

namespace InfinityMercsApp.Services.Season;

internal static class SeasonFileService
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal static async Task<string> CreateSeasonFileAsync(string companyName, string companyIdentifier, string companyFilePath)
    {
        var now = DateTimeOffset.UtcNow;
        var seasonsDir = Path.Combine(FileSystem.Current.AppDataDirectory, "MercenaryRecords", "Seasons");
        Directory.CreateDirectory(seasonsDir);

        var timestamp = now.ToString("yyyyMMddHHmmss");
        var safeName = companyName.Replace(' ', '_');
        var fileName = $"{safeName}-{timestamp}.json";
        var filePath = Path.Combine(seasonsDir, fileName);

        var seasonFile = new SeasonFile
        {
            CompanyName = companyName,
            CompanyIdentifier = companyIdentifier,
            CompanyFilePath = companyFilePath,
            CreatedDate = now.ToString("yyyy-MM-dd"),
            InitialPurchases = new SeasonMarketplace(),
            Rounds = []
        };

        await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(seasonFile, WriteOptions));
        return filePath;
    }

    internal static async Task<SeasonFile?> LoadSeasonFileAsync(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<SeasonFile>(json, ReadOptions);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"SeasonFileService failed to load season file '{filePath}': {ex.Message}");
            return null;
        }
    }

    internal static async Task<bool> SaveSeasonFileAsync(string? filePath, SeasonFile? seasonFile)
    {
        if (string.IsNullOrWhiteSpace(filePath) || seasonFile is null)
        {
            return false;
        }

        try
        {
            await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(seasonFile, WriteOptions));
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"SeasonFileService failed to save season file '{filePath}': {ex.Message}");
            return false;
        }
    }

    internal static async Task<bool> UpdateLatestRoundAsync(string? filePath, Action<SeasonRound> updater)
    {
        var seasonFile = await LoadSeasonFileAsync(filePath);
        if (seasonFile is null || seasonFile.Rounds.Count == 0) return false;

        var latest = seasonFile.Rounds
            .OrderByDescending(r => r.RoundIndex)
            .First();
        updater(latest);
        RefreshCurrentStatus(seasonFile);
        return await SaveSeasonFileAsync(filePath, seasonFile);
    }

    internal static void RefreshCurrentStatus(SeasonFile seasonFile)
    {
        var status = seasonFile.CurrentStatus;

        var unitXp = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var unitNotoriety = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var round in seasonFile.Rounds)
        {
            foreach (var ur in round.MissionResults.UnitResults)
            {
                unitXp[ur.UnitName] = unitXp.GetValueOrDefault(ur.UnitName, 0) + ur.XpGained;
            }

            if (round.Downtime.NotorietyGain != 0 &&
                !string.IsNullOrWhiteSpace(round.Downtime.ParticipantName))
            {
                var name = round.Downtime.ParticipantName;
                unitNotoriety[name] = unitNotoriety.GetValueOrDefault(name, 0) + round.Downtime.NotorietyGain;
            }
        }

        var ledger = SeasonEconomy.Ledger(seasonFile);
        status.CrEarned = ledger.CrEarned;
        status.CrSpent = ledger.CrSpent;
        status.SwcEarned = ledger.SwcEarned;
        status.SwcBought = ledger.SwcBought;
        status.SwcSpent = ledger.SwcSpent;

        foreach (var (name, xp) in unitXp)
        {
            var unit = ResolveUnitStatus(status, name);
            unit.TotalExperience = xp;
        }

        foreach (var (name, notoriety) in unitNotoriety)
        {
            var unit = ResolveUnitStatus(status, name);
            unit.Notoriety = notoriety;
        }
    }

    private static SeasonUnitStatus ResolveUnitStatus(SeasonStatus status, string name)
    {
        var unit = status.Units.FirstOrDefault(u =>
            string.Equals(u.UnitName, name, StringComparison.OrdinalIgnoreCase));
        if (unit is null)
        {
            unit = new SeasonUnitStatus { UnitName = name };
            status.Units.Add(unit);
        }
        return unit;
    }

    internal readonly record struct SeasonResources(int CreditsBalance, double SwcBalance);

    internal static SeasonResources ComputeAvailableResources(SeasonFile? seasonFile)
    {
        var ledger = SeasonEconomy.Ledger(seasonFile);
        return new SeasonResources(ledger.CreditsBalance, ledger.SwcBalance);
    }

    internal static int ComputeCompanyNotoriety(SeasonFile? seasonFile)
    {
        if (seasonFile is null) return 0;
        return seasonFile.Rounds.Sum(r => r.Downtime.NotorietyGain);
    }

    internal static int ResolveCurrentRound(SeasonFile? seasonFile)
    {
        var rounds = seasonFile?.Rounds ?? [];
        if (rounds.Count == 0)
        {
            return 0;
        }

        var highestRoundIndex = rounds.Max(round => Math.Max(0, round.RoundIndex));
        return highestRoundIndex > 0 ? highestRoundIndex : rounds.Count;
    }
}
