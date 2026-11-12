using InfinityMercsApp.Domain.Models.Metadata.Ranges;

namespace InfinityMercsApp.Infrastructure.API.InfinityArmy;

using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Infrastructure.Json;
using DomainMetadataAmmunition = InfinityMercsApp.Domain.Models.Metadata.Ammunition;
using DomainMetadataBooty = InfinityMercsApp.Domain.Models.Metadata.Booty;
using DomainMetadataDocument = InfinityMercsApp.Domain.Models.Metadata.MetadataDocument;
using DomainMetadataEquipment = InfinityMercsApp.Domain.Models.Metadata.Equipments;
using DomainMetadataFaction = InfinityMercsApp.Domain.Models.Metadata.Faction;
using DomainMetadataHackingProgram = InfinityMercsApp.Domain.Models.Metadata.HackingProgram;
using DomainMetadataMartialArt = InfinityMercsApp.Domain.Models.Metadata.MartialArt;
using DomainMetadataMetachemistry = InfinityMercsApp.Domain.Models.Metadata.Metachemistry;
using DomainMetadataSkill = InfinityMercsApp.Domain.Models.Metadata.Skill;
using DomainMetadataWeapon = InfinityMercsApp.Domain.Models.Metadata.Weapon;
using ApiMetadataDocument = InfinityMercsApp.Infrastructure.Models.API.Metadata.MetadataDocument;
using ApiMetadataWeapon = InfinityMercsApp.Infrastructure.Models.API.Metadata.Weapon;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <inheritdoc/>
public sealed class InfinityArmyAPI(IArmyPayloadSource payloads, ILogger<InfinityArmyAPI> logger, IArmyJsonReader armyJsonReader) : IInfinityArmyAPI
{
    internal const string ArmyUrlBase = CorvusBelliHttp.ArmyUrlBase;
    internal const string MetadataUrl = CorvusBelliHttp.MetadataUrl;
    private const string DebugDumpFolderName = "external-read-failures";

    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new RelaxedInt32Converter(), new RelaxedNullableInt32Converter() },
        TypeInfoResolver = MetadataJsonContext.Default
    };

    /// <inheritdoc/>
    public async Task<DomainMetadataDocument?> GetMetaDataAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Reading the metadata.");
        var payload = await payloads.GetMetadataAsync(cancellationToken);
        if (!LooksLikeJsonObject(payload))
        {
            var dumpPath = await SaveFailedPayloadAsync("metadata-non-json-payload", MetadataUrl, payload);
            logger.LogError("Metadata response was not JSON. Dump saved to: {DumpPath}", dumpPath);
            throw new InvalidDataException($"Metadata response was not JSON. Dump: {dumpPath}");
        }

        try
        {
            var apiDocument = JsonSerializer.Deserialize(payload, JsonContracts.For<ApiMetadataDocument>(jsonOptions));
            if (apiDocument is null)
            {
                var dumpPath = await SaveFailedPayloadAsync("metadata-deserialize-null", MetadataUrl, payload);
                logger.LogError("Metadata deserialization returned null. Dump saved to: {DumpPath}", dumpPath);
                return null;
            }

            var document = MapMetadataDocument(apiDocument);
            document.Fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
            return document;
        }
        catch (JsonException ex)
        {
            var dumpPath = await SaveFailedPayloadAsync("metadata-deserialize-error", MetadataUrl, payload);
            logger.LogError(ex, "Metadata deserialization failed. Dump saved to: {DumpPath}", dumpPath);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ArmyDefinition?> GetArmyDataAsync(int factionId, CancellationToken cancellationToken = default)
    {
        var fullUrl = CorvusBelliHttp.ArmyUrl(factionId);
        logger.LogInformation("Reading the army of faction {FactionId}.", factionId);

        var payload = await payloads.GetArmyAsync(factionId, cancellationToken);
        if (!LooksLikeJsonObject(payload))
        {
            var dumpPath = await SaveFailedPayloadAsync($"army-{factionId}-non-json-payload", fullUrl, payload);
            logger.LogError("Army response for faction {FactionId} was not JSON. Dump saved to: {DumpPath}", factionId, dumpPath);
            throw new InvalidDataException($"Army response for faction {factionId} was not JSON. Dump: {dumpPath}");
        }

        // The typed reader is lenient: a malformed section reads as empty instead of failing the army.
        var army = armyJsonReader.ReadArmyPayload(payload);
        if (army is null)
        {
            var dumpPath = await SaveFailedPayloadAsync($"army-{factionId}-deserialize-error", fullUrl, payload);
            logger.LogError("Army payload for faction {FactionId} could not be read. Dump saved to: {DumpPath}", factionId, dumpPath);
            throw new InvalidDataException($"Army payload for faction {factionId} could not be read. Dump: {dumpPath}");
        }

        return army;
    }

    private static DomainMetadataDocument MapMetadataDocument(ApiMetadataDocument source)
    {
        return new DomainMetadataDocument
        {
            Factions = source.Factions.Select(x => new DomainMetadataFaction
            {
                Id = x.Id,
                ParentId = x.Parent,
                Name = x.Name,
                Slug = x.Slug,
                Discontinued = x.Discontinued,
                Logo = x.Logo
            }).ToList(),
            Ammunitions = source.Ammunitions.Select(x => new DomainMetadataAmmunition
            {
                Id = x.Id,
                Name = x.Name,
                Wiki = x.Wiki
            }).ToList(),
            Weapons = source.Weapons.Select(x => new DomainMetadataWeapon
            {
                WeaponKey = BuildWeaponKey(x),
                WeaponId = x.Id,
                Name = x.Name,
                Type = x.Type,
                Mode = x.Mode,
                Wiki = x.Wiki,
                AmmunitionId = x.Ammunition,
                Burst = x.Burst,
                Damage = x.Damage,
                Saving = x.Saving,
                SavingNum = x.SavingNum,
                Profile = x.Profile,
                Properties = x.Properties ?? [],
                Distance = MapDistance(x.Distance)
            }).ToList(),
            Skills = source.Skills.Select(x => new DomainMetadataSkill
            {
                Id = x.Id,
                Name = x.Name,
                Wiki = x.Wiki
            }).ToList(),
            Equips = source.Equips.Select(x => new DomainMetadataEquipment
            {
                Id = x.Id,
                Name = x.Name,
                Wiki = x.Wiki
            }).ToList(),
            Hack = source.Hack.Select(x => new DomainMetadataHackingProgram
            {
                Name = x.Name,
                Opponent = x.Opponent,
                Special = x.Special,
                Damage = x.Damage,
                Attack = x.Attack,
                Burst = x.Burst,
                Extra = x.Extra,
                SkillTypes = x.SkillType ?? [],
                Devices = x.Devices ?? [],
                Targets = x.Target ?? []
            }).ToList(),
            MartialArts = source.MartialArts.Select(x => new DomainMetadataMartialArt
            {
                Name = x.Name,
                Opponent = x.Opponent,
                Damage = x.Damage,
                Attack = x.Attack,
                Burst = x.Burst
            }).ToList(),
            Metachemistry = source.Metachemistry.Select(x => new DomainMetadataMetachemistry
            {
                Id = x.Id,
                Name = x.Name,
                Value = x.Value
            }).ToList(),
            Booty = source.Booty.Select(x => new DomainMetadataBooty
            {
                Id = x.Id,
                Name = x.Name,
                Value = x.Value
            }).ToList()
        };
    }

    private static string BuildWeaponKey(ApiMetadataWeapon weapon)
    {
        return $"{weapon.Id}:{weapon.Name}:{weapon.Mode ?? string.Empty}";
    }

    private static WeaponDistance? MapDistance(Dictionary<string, Models.API.Metadata.WeaponDistanceBand?>? distance)
    {
        var bands = distance?
            .Select(band => band.Value is { } value ? new WeaponDistanceBand { Name = band.Key, Max = value.Max, Mod = value.Mod ?? "0" } : null)
            .OfType<WeaponDistanceBand>()
            .ToList();
        return bands is null || bands.Count == 0 ? null : new WeaponDistance { Bands = bands };
    }

    private async Task<string> SaveFailedPayloadAsync(string category, string url, string payload)
    {
        try
        {
            var directory = GetDebugDumpDirectory();
            var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
            var fileName = $"{SanitizeForFileName(category)}-{timestamp}-{Guid.NewGuid():N}.txt";
            var fullPath = Path.GetFullPath(Path.Combine(directory, fileName));

            var dump = $"UTC: {DateTimeOffset.UtcNow:O}{Environment.NewLine}URL: {url}{Environment.NewLine}{Environment.NewLine}{payload}";
            await File.WriteAllTextAsync(fullPath, dump);
            return fullPath;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write external-read failure dump.");
            return "<failed-to-write-dump>";
        }
    }

    private static string GetDebugDumpDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var directory = Path.Combine(appData, "InfinityMercsApp", "debug", DebugDumpFolderName);
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string SanitizeForFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var buffer = value.ToCharArray();
        for (var i = 0; i < buffer.Length; i++)
        {
            if (invalidChars.Contains(buffer[i]))
            {
                buffer[i] = '_';
            }
        }

        return new string(buffer);
    }

    private static bool LooksLikeJsonObject(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        foreach (var ch in payload)
        {
            if (!char.IsWhiteSpace(ch))
            {
                return ch == '{';
            }
        }

        return false;
    }
}
