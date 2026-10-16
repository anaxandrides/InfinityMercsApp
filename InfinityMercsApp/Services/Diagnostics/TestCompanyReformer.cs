using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Lieutenants;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanyProfiles;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.CompanySelection.StartCompany;
using InfinityMercsApp.Services.CompanySelection.Units;
using InfinityMercsApp.Services.Packaging;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;
using InfinityMercsApp.ViewModels.Company.Selection.Workflows;

namespace InfinityMercsApp.Services.Diagnostics;

/// <summary>
/// Debug tooling: rebuilds the bundled test companies (and any extra test files already on the device)
/// from current army data, so their saved profiles match the latest unit definitions.
/// </summary>
public sealed class TestCompanyReformer(
    IArmyDataService armyData,
    ISpecOpsProvider specOps,
    IAppPackageFiles packageFiles,
    RecordPaths paths)
{
    public const string ManifestFileName = "DebugTestCompanyManifest.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Test companies that live only on the device, reformed from their saved copy.</summary>
    internal static readonly IReadOnlyList<string> AdditionalCompanyFileNames =
    [
        "test-correg-tag-0001.json",
        "test-nw-tag-lt-0001.json"
    ];

    /// <summary>Reforms every manifest company and returns one summary line per company.</summary>
    public async Task<IReadOnlyList<string>> ReformAllAsync()
    {
        var companies = (await LoadManifestAsync())?.Companies ?? [];
        foreach (var additionalFile in AdditionalCompanyFileNames)
        {
            if (!companies.Any(x => string.Equals(x.FileName?.Trim(), additionalFile, StringComparison.OrdinalIgnoreCase)))
            {
                companies.Add(new DebugTestCompanyManifestEntry { FileName = additionalFile });
            }
        }

        var summaryLines = new List<string>(companies.Count);
        foreach (var manifestCompany in companies)
        {
            summaryLines.Add(await ReformAsync(manifestCompany));
        }

        return summaryLines;
    }

    private async Task<string> ReformAsync(DebugTestCompanyManifestEntry manifestCompany)
    {
        var fileName = manifestCompany.FileName?.Trim();
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "manifest entry: skipped (missing file name)";
        }

        var filePath = Path.Combine(paths.CompaniesDirectory, fileName);
        var source = manifestCompany.CompanyData ?? await TryLoadSavedCompanyFileAsync(filePath);
        if (source is null)
        {
            return $"{fileName}: skipped (missing company data)";
        }

        try
        {
            var sourceFactions = BuildSourceFactions(source);
            var reformEntries = BuildReformEntries(source);
            if (reformEntries.Count == 0)
            {
                return $"{fileName}: skipped (no primary entries)";
            }

            var request = new CompanyStartSaveRequest<DebugSourceFaction, DebugMercsEntry>
            {
                CompanyName = source.CompanyName,
                CompanyType = source.CompanyType,
                MercsCompanyEntries = reformEntries,
                ShowRightSelectionBox = sourceFactions.Count > 1,
                LeftSlotFaction = sourceFactions.Count > 0 ? sourceFactions[0] : null,
                RightSlotFaction = sourceFactions.Count > 1 ? sourceFactions[1] : null,
                Factions = sourceFactions,
                ArmyDataService = armyData,
                SpecOpsProvider = specOps,
                ShowUnitsInInches = false,
                SelectedStartSeasonPoints = source.StartSeasonPoints.ToString(),
                SeasonPointsCapText = source.CurrentPoints.ToString()
            };

            await CompanyStartSaveWorkflow.WriteCompanyFileAsync(
                request,
                source.ImprovedCaptainStats ?? new SavedImprovedCaptainStats(),
                filePath);
            return $"{fileName}: reformed";
        }
        catch (Exception ex)
        {
            return $"{fileName}: failed ({ex.Message})";
        }
    }

    private List<DebugSourceFaction> BuildSourceFactions(DebugSavedCompanyFile source)
    {
        var factions = source.SourceFactions
            .Where(x => x.FactionId > 0)
            .Select(x => new DebugSourceFaction
            {
                Id = x.FactionId,
                ParentId = 0,
                Name = string.IsNullOrWhiteSpace(x.FactionName) ? $"Faction {x.FactionId}" : x.FactionName
            })
            .ToList();

        if (factions.Count > 0)
        {
            return factions;
        }

        return source.Entries
            .Where(x => !x.IsPeripheralUnit && x.SourceFactionId > 0)
            .Select(x => x.SourceFactionId)
            .Distinct()
            .Select(id => new DebugSourceFaction
            {
                Id = id,
                ParentId = 0,
                Name = $"Faction {id}"
            })
            .ToList();
    }

    private List<DebugMercsEntry> BuildReformEntries(
        DebugSavedCompanyFile source)
    {
        var sourcePeripheralByParentIndex = source.Entries
            .Where(x => x.IsPeripheralUnit)
            .Select(x => (Entry: x, Parent: x.ParentEntryIndex))
            .Where(x => x.Parent.HasValue)
            .GroupBy(x => x.Parent.GetValueOrDefault())
            .ToDictionary(g => g.Key, g => g.First().Entry);

        var primaryEntries = source.Entries
            .Where(x => !x.IsPeripheralUnit)
            .OrderBy(x => x.EntryIndex)
            .ToList();
        var result = new List<DebugMercsEntry>(primaryEntries.Count);

        foreach (var entry in primaryEntries)
        {
            var factionId = entry.SourceFactionId > 0 ? entry.SourceFactionId : entry.FactionId;
            var sourceUnitId = entry.SourceUnitId > 0 ? entry.SourceUnitId : entry.ProfileId;
            if (!TryBuildDbDerivedProfilePayload(
                    factionId,
                    sourceUnitId,
                    entry.ProfileKey,
                    entry.IsLieutenant || entry.IsCaptain,
                    out var dbPayload))
            {
                continue;
            }

            var baseUnitName = string.IsNullOrWhiteSpace(entry.BaseUnitName)
                ? (string.IsNullOrWhiteSpace(entry.BaseProfileHumanReadable) ? entry.Name : entry.BaseProfileHumanReadable)
                : entry.BaseUnitName;
            var subtitle = BuildSubtitleFromBaseStats(entry);
            var hasSourcePeripheral = sourcePeripheralByParentIndex.TryGetValue(entry.EntryIndex, out var sourcePeripheralEntry);

            var peripheralNameHeading = dbPayload.PeripheralNameHeading;
            var peripheralMov = dbPayload.PeripheralMov;
            var peripheralCc = dbPayload.PeripheralCc;
            var peripheralBs = dbPayload.PeripheralBs;
            var peripheralPh = dbPayload.PeripheralPh;
            var peripheralWip = dbPayload.PeripheralWip;
            var peripheralArm = dbPayload.PeripheralArm;
            var peripheralBts = dbPayload.PeripheralBts;
            var peripheralVitalityHeader = dbPayload.PeripheralVitalityHeader;
            var peripheralVitality = dbPayload.PeripheralVitality;
            var peripheralS = dbPayload.PeripheralS;
            var peripheralAva = dbPayload.PeripheralAva;
            var savedPeripheralEquipment = dbPayload.SavedPeripheralEquipment;
            var savedPeripheralSkills = dbPayload.SavedPeripheralSkills;
            var savedPeripheralCharacteristics = dbPayload.SavedPeripheralCharacteristics;
            var savedPeripheralRangedWeapons = dbPayload.SavedPeripheralRangedWeapons;
            var savedPeripheralCcWeapons = dbPayload.SavedPeripheralCcWeapons;

            if (sourcePeripheralEntry is not null)
            {
                var sourcePeripheralName = !string.IsNullOrWhiteSpace(sourcePeripheralEntry.CustomName)
                    ? sourcePeripheralEntry.CustomName.Trim()
                    : (!string.IsNullOrWhiteSpace(sourcePeripheralEntry.BaseUnitName)
                        ? sourcePeripheralEntry.BaseUnitName.Trim()
                        : sourcePeripheralEntry.Name.Trim());

                peripheralNameHeading = string.IsNullOrWhiteSpace(sourcePeripheralName)
                    ? peripheralNameHeading
                    : $"Peripheral: {sourcePeripheralName}";
                peripheralMov = NormalizeStat(sourcePeripheralEntry.CurrentMov);
                peripheralCc = NormalizeStat(sourcePeripheralEntry.CurrentCc);
                peripheralBs = NormalizeStat(sourcePeripheralEntry.CurrentBs);
                peripheralPh = NormalizeStat(sourcePeripheralEntry.CurrentPh);
                peripheralWip = NormalizeStat(sourcePeripheralEntry.CurrentWip);
                peripheralArm = NormalizeStat(sourcePeripheralEntry.CurrentArm);
                peripheralBts = NormalizeStat(sourcePeripheralEntry.CurrentBts);
                peripheralVitality = NormalizeStat(sourcePeripheralEntry.CurrentVitaOrStr);
                peripheralS = NormalizeStat(sourcePeripheralEntry.CurrentS);
                peripheralVitalityHeader = StatKeys.VitalityFor(sourcePeripheralEntry.UnitTypeCode).Key();
                var peripheralFilters = FiltersFor(sourcePeripheralEntry);
                savedPeripheralEquipment = SavedCodeNames.JoinOrDash(SavedCodeNames.ForEntry(peripheralFilters, sourcePeripheralEntry, ProfileEntryKind.Equip));
                savedPeripheralSkills = SavedCodeNames.JoinOrDash(SavedCodeNames.ForEntry(peripheralFilters, sourcePeripheralEntry, ProfileEntryKind.Skills));
                savedPeripheralCharacteristics = SavedCodeNames.JoinOrDash(SavedCodeNames.ForEntry(peripheralFilters, sourcePeripheralEntry, ProfileEntryKind.Chars));
                var peripheralWeapons = SavedCodeNames.Weapons(peripheralFilters, sourcePeripheralEntry);
                savedPeripheralRangedWeapons = SavedCodeNames.JoinOrDash(peripheralWeapons.Ranged);
                savedPeripheralCcWeapons = SavedCodeNames.JoinOrDash(peripheralWeapons.Melee);
            }

            result.Add(new DebugMercsEntry
            {
                Name = string.IsNullOrWhiteSpace(entry.Name) ? baseUnitName : entry.Name,
                BaseUnitName = baseUnitName,
                CostValue = entry.CalculatedCr > 0 ? entry.CalculatedCr : entry.BaseCr,
                IsLieutenant = entry.IsLieutenant || entry.IsCaptain,
                UnitTypeCode = entry.UnitTypeCode,
                ProfileKey = entry.ProfileKey,
                SourceFactionId = factionId,
                SourceUnitId = sourceUnitId,
                LogoSourceFactionId = entry.LogoSourceFactionId > 0 ? entry.LogoSourceFactionId : factionId,
                LogoSourceUnitId = entry.LogoSourceUnitId > 0 ? entry.LogoSourceUnitId : sourceUnitId,
                SavedEquipment = dbPayload.SavedEquipment,
                SavedSkills = dbPayload.SavedSkills,
                SavedCharacteristics = dbPayload.SavedCharacteristics,
                SavedRangedWeapons = dbPayload.SavedRangedWeapons,
                SavedCcWeapons = dbPayload.SavedCcWeapons,
                HasPeripheralStatBlock = dbPayload.HasPeripheralStatBlock || hasSourcePeripheral,
                PeripheralNameHeading = peripheralNameHeading,
                PeripheralMov = peripheralMov,
                PeripheralCc = peripheralCc,
                PeripheralBs = peripheralBs,
                PeripheralPh = peripheralPh,
                PeripheralWip = peripheralWip,
                PeripheralArm = peripheralArm,
                PeripheralBts = peripheralBts,
                PeripheralVitalityHeader = peripheralVitalityHeader,
                PeripheralVitality = peripheralVitality,
                PeripheralS = peripheralS,
                PeripheralAva = peripheralAva,
                SavedPeripheralEquipment = savedPeripheralEquipment,
                SavedPeripheralSkills = savedPeripheralSkills,
                SavedPeripheralCharacteristics = savedPeripheralCharacteristics,
                SavedPeripheralRangedWeapons = savedPeripheralRangedWeapons,
                SavedPeripheralCcWeapons = savedPeripheralCcWeapons,
                ExperiencePoints = Math.Max(0, entry.ExperiencePoints),
                UnitMoveDisplay = NormalizeStat(entry.BaseMov),
                Subtitle = subtitle
            });
        }

        return result;
    }

    private async Task<DebugTestCompanyManifest?> LoadManifestAsync()
    {
        async Task<string?> TryReadAssetAsync(string assetPath)
        {
            try
            {
                return await packageFiles.ReadTextAsync(assetPath);
            }
            catch
            {
                return null;
            }
        }

        var rawJson = await TryReadAssetAsync(ManifestFileName)
                      ?? await TryReadAssetAsync($"Resources/Raw/{ManifestFileName}");
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return null;
        }

        return JsonSerializer.Deserialize<DebugTestCompanyManifest>(rawJson, JsonOptions);
    }

    private static async Task<DebugSavedCompanyFile?> TryLoadSavedCompanyFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<DebugSavedCompanyFile>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private bool TryBuildDbDerivedProfilePayload(
        int factionId,
        int sourceUnitId,
        string? savedProfileKey,
        bool isLieutenant,
        out DbDerivedProfilePayload payload)
    {
        payload = new DbDerivedProfilePayload();
        if (factionId <= 0 || sourceUnitId <= 0)
        {
            return false;
        }

        var unit = armyData.GetUnit(factionId, sourceUnitId);
        if (unit is null || unit.ProfileGroups is not { IsEmpty: false })
        {
            return false;
        }

        var snapshot = armyData.GetFactionSnapshot(factionId);
        var filters = snapshot?.Filters;
        filters ??= ArmyFilterCatalog.Empty;
        var equipLookup = filters.Names(ArmyFilterSection.Equip);
        var skillsLookup = filters.Names(ArmyFilterSection.Skills);
        var entryNames = new ProfileEntryNames(filters.Extras, showUnitsInInches: false);

        var profileGroups = unit.ProfileGroups;
        var options = profileGroups.AllOptions()
            .Select(x => x.Option)
            .Where(option => !option.CostsSwc)
            .ToList();

        var stableEquip = CompanyUnitDetailsWorkflowService.CommonNames(profileGroups, options, ProfileEntryKind.Equip, equipLookup, entryNames);
        var stableSkills = CompanyUnitDetailsWorkflowService.CommonNames(profileGroups, options, ProfileEntryKind.Skills, skillsLookup, entryNames)
            .Where(x => !LieutenantRules.Mentions(x))
            .ToList();

        var profiles = CompanyProfilePopulationWorkflowService.BuildProfiles(
            new CompanyProfilePopulationRequest
            {
                ProfileGroups = profileGroups,
                Filters = filters ?? ArmyFilterCatalog.Empty,
                ForceLieutenant = false,
                ShowTacticalAwarenessIcon = false,
                ShowUnitsInInches = false,
                ShouldIncludeOption = (_, _, _) => true,
                FormatMoveValue = armyData.FormatMoveValue
            });

        if (profiles.Count == 0)
        {
            return false;
        }

        var selectedProfile = profiles.FirstOrDefault(x =>
            !string.IsNullOrWhiteSpace(savedProfileKey) &&
            string.Equals(x.ProfileKey, savedProfileKey, StringComparison.OrdinalIgnoreCase));
        selectedProfile ??= TryFindProfileByLooseKeyMatch(profiles, savedProfileKey);
        selectedProfile ??= profiles.FirstOrDefault(x => x.IsLieutenant == isLieutenant);
        selectedProfile ??= profiles.FirstOrDefault();
        if (selectedProfile is null)
        {
            return false;
        }

        var combinedEquipment = CompanyProfileTextService.MergeCommonAndUnique(stableEquip, selectedProfile.UniqueEquipment);
        var combinedSkills = CompanyProfileTextService.MergeCommonAndUnique(stableSkills, selectedProfile.UniqueSkills);
        var combinedCharacteristics = CompanyProfileTextService.SplitDisplayLine(selectedProfile.Characteristics).ToList();

        var peripheralStats = CompanySelectionRosterWorkflow.BuildMercsCompanyPeripheralStats<PeripheralMercsCompanyStats>(
            selectedProfile,
            unit.ProfileGroups,
            filters,
            (peripheralName, peripheralProfile, selectedFiltersJson) => BuildPeripheralStatBlock(peripheralName, peripheralProfile, selectedFiltersJson));

        payload = new DbDerivedProfilePayload
        {
            SavedEquipment = CompanyProfileTextService.JoinOrDash(combinedEquipment),
            SavedSkills = CompanyProfileTextService.JoinOrDash(combinedSkills),
            SavedCharacteristics = CompanyProfileTextService.JoinOrDash(combinedCharacteristics),
            SavedRangedWeapons = selectedProfile.RangedWeapons,
            SavedCcWeapons = selectedProfile.MeleeWeapons,
            HasPeripheralStatBlock = selectedProfile.HasPeripheralStatBlock || peripheralStats is not null,
            PeripheralNameHeading = peripheralStats?.NameHeading ?? selectedProfile.PeripheralNameHeading,
            PeripheralMov = peripheralStats?.Mov ?? selectedProfile.PeripheralMov,
            PeripheralCc = peripheralStats?.Cc ?? selectedProfile.PeripheralCc,
            PeripheralBs = peripheralStats?.Bs ?? selectedProfile.PeripheralBs,
            PeripheralPh = peripheralStats?.Ph ?? selectedProfile.PeripheralPh,
            PeripheralWip = peripheralStats?.Wip ?? selectedProfile.PeripheralWip,
            PeripheralArm = peripheralStats?.Arm ?? selectedProfile.PeripheralArm,
            PeripheralBts = peripheralStats?.Bts ?? selectedProfile.PeripheralBts,
            PeripheralVitalityHeader = peripheralStats?.VitalityHeader ?? selectedProfile.PeripheralVitalityHeader,
            PeripheralVitality = peripheralStats?.Vitality ?? selectedProfile.PeripheralVitality,
            PeripheralS = peripheralStats?.S ?? selectedProfile.PeripheralS,
            PeripheralAva = peripheralStats?.Ava ?? selectedProfile.PeripheralAva,
            SavedPeripheralEquipment = peripheralStats?.Equipment ?? "-",
            SavedPeripheralSkills = peripheralStats?.Skills ?? "-",
            SavedPeripheralCharacteristics = peripheralStats?.Characteristics ?? "-",
            SavedPeripheralRangedWeapons = peripheralStats?.RangedWeapons ?? "-",
            SavedPeripheralCcWeapons = peripheralStats?.CcWeapons ?? "-"
        };
        return true;
    }

    private PeripheralMercsCompanyStats? BuildPeripheralStatBlock(string peripheralName, UnitProfile peripheralProfile, ArmyFilterCatalog? filters)
    {
        return CompanyUnitDetailsShared.BuildPeripheralStatBlock(peripheralName, peripheralProfile, filters, showUnitsInInches: false);
    }

    private static string BuildSubtitleFromBaseStats(DebugSavedEntry entry)
    {
        return
            $"MOV {NormalizeStat(entry.BaseMov)} | CC {NormalizeStat(entry.BaseCc)} | BS {NormalizeStat(entry.BaseBs)} | PH {NormalizeStat(entry.BasePh)} | WIP {NormalizeStat(entry.BaseWip)} | ARM {NormalizeStat(entry.BaseArm)} | BTS {NormalizeStat(entry.BaseBts)} | VITA {NormalizeStat(entry.BaseVitaOrStr)} | S {NormalizeStat(entry.BaseS)}";
    }

    private static string NormalizeStat(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }

    // The army filters for the faction an entry came from; none when the entry names no faction.
    private ArmyFilterCatalog? FiltersFor(DebugSavedEntry entry)
    {
        var factionId = entry.SourceFactionId > 0 ? entry.SourceFactionId : entry.FactionId;
        return factionId > 0 ? armyData.GetFactionSnapshot(factionId)?.Filters : null;
    }

    private static ViewerProfileItem? TryFindProfileByLooseKeyMatch(
        IEnumerable<ViewerProfileItem> profiles,
        string? savedProfileKey)
    {
        if (string.IsNullOrWhiteSpace(savedProfileKey))
        {
            return null;
        }

        var match = Regex.Match(
            savedProfileKey.Trim(),
            @"^\s*(?:[^|]*\|)?(?<name>[^|]+)\|(?<cost>\d+)\|[^|]*\|lt:(?<lt>[01])\s*$",
            RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return null;
        }

        var optionName = match.Groups["name"].Value.Trim();
        if (string.IsNullOrWhiteSpace(optionName))
        {
            return null;
        }

        var hasCost = int.TryParse(match.Groups["cost"].Value, out var cost);
        var hasLt = int.TryParse(match.Groups["lt"].Value, out var ltValue);
        var isLieutenant = hasLt && ltValue > 0;

        return profiles.FirstOrDefault(profile =>
        {
            if (!string.Equals(profile.Name?.Trim(), optionName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (hasLt && profile.IsLieutenant != isLieutenant)
            {
                return false;
            }

            if (!hasCost)
            {
                return true;
            }

            return TryParseFirstInteger(profile.Cost, out var parsedCost) && parsedCost == cost;
        });
    }

    private static bool TryParseFirstInteger(string? value, out int parsed)
    {
        parsed = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var match = Regex.Match(value, @"\d+");
        return match.Success && int.TryParse(match.Value, out parsed);
    }

    private sealed class DbDerivedProfilePayload
    {
        public string SavedEquipment { get; init; } = "-";
        public string SavedSkills { get; init; } = "-";
        public string SavedCharacteristics { get; init; } = "-";
        public string SavedRangedWeapons { get; init; } = "-";
        public string SavedCcWeapons { get; init; } = "-";
        public bool HasPeripheralStatBlock { get; init; }
        public string PeripheralNameHeading { get; init; } = string.Empty;
        public string PeripheralMov { get; init; } = "-";
        public string PeripheralCc { get; init; } = "-";
        public string PeripheralBs { get; init; } = "-";
        public string PeripheralPh { get; init; } = "-";
        public string PeripheralWip { get; init; } = "-";
        public string PeripheralArm { get; init; } = "-";
        public string PeripheralBts { get; init; } = "-";
        public string PeripheralVitalityHeader { get; init; } = Stat.Vita.Key();
        public string PeripheralVitality { get; init; } = "-";
        public string PeripheralS { get; init; } = "-";
        public string PeripheralAva { get; init; } = "-";
        public string SavedPeripheralEquipment { get; init; } = "-";
        public string SavedPeripheralSkills { get; init; } = "-";
        public string SavedPeripheralCharacteristics { get; init; } = "-";
        public string SavedPeripheralRangedWeapons { get; init; } = "-";
        public string SavedPeripheralCcWeapons { get; init; } = "-";
    }

    private sealed class DebugSourceFaction : ICompanySourceFaction
    {
        public int Id { get; init; }
        public int ParentId { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    private sealed class DebugMercsEntry : ICompanyMercsEntry
    {
        public string Name { get; init; } = string.Empty;
        public string BaseUnitName { get; init; } = string.Empty;
        public int CostValue { get; init; }
        public bool IsLieutenant { get; init; }
        public bool IsSpecOps { get; init; }
        public string UnitTypeCode { get; init; } = string.Empty;
        public string ProfileKey { get; init; } = string.Empty;
        public int SourceFactionId { get; init; }
        public int SourceUnitId { get; init; }
        public int LogoSourceFactionId { get; init; }
        public int LogoSourceUnitId { get; init; }
        public string SavedEquipment { get; init; } = "-";
        public string SavedSkills { get; init; } = "-";
        public string SavedCharacteristics { get; init; } = "-";
        public string SavedRangedWeapons { get; init; } = "-";
        public string SavedCcWeapons { get; init; } = "-";
        public bool HasPeripheralStatBlock { get; init; }
        public string PeripheralNameHeading { get; init; } = string.Empty;
        public string PeripheralMov { get; set; } = "-";
        public string PeripheralCc { get; init; } = "-";
        public string PeripheralBs { get; init; } = "-";
        public string PeripheralPh { get; init; } = "-";
        public string PeripheralWip { get; init; } = "-";
        public string PeripheralArm { get; init; } = "-";
        public string PeripheralBts { get; init; } = "-";
        public string PeripheralVitalityHeader { get; init; } = Stat.Vita.Key();
        public string PeripheralVitality { get; init; } = "-";
        public string PeripheralS { get; init; } = "-";
        public string PeripheralAva { get; init; } = "-";
        public string SavedPeripheralEquipment { get; init; } = "-";
        public string SavedPeripheralSkills { get; init; } = "-";
        public string SavedPeripheralCharacteristics { get; init; } = "-";
        public string SavedPeripheralRangedWeapons { get; init; } = "-";
        public string SavedPeripheralCcWeapons { get; init; } = "-";
        public int ExperiencePoints { get; init; }
        public List<CompanyTrooperPerkState> Perks { get; init; } = [];
        public int? UnitMoveFirstCm { get; init; }
        public int? UnitMoveSecondCm { get; init; }
        public string UnitMoveDisplay { get; set; } = "-";
        public string? Subtitle { get; set; } = string.Empty;
        public int? PeripheralMoveFirstCm { get; init; }
        public int? PeripheralMoveSecondCm { get; init; }
        public string? CachedLogoPath { get; init; }
        public string? PackagedLogoPath { get; init; }
    }

    private sealed class DebugSavedCompanyFile : CompanySavedCompanyFileBase<DebugSavedCompanyFaction, DebugSavedEntry>
    {
    }

    private sealed class DebugTestCompanyManifest
    {
        [JsonPropertyName("generatedUtc")]
        public string? GeneratedUtc { get; init; }

        [JsonPropertyName("source")]
        public string? Source { get; init; }

        [JsonPropertyName("companies")]
        public List<DebugTestCompanyManifestEntry> Companies { get; init; } = [];
    }

    private sealed class DebugTestCompanyManifestEntry
    {
        [JsonPropertyName("fileName")]
        public string? FileName { get; init; }

        [JsonPropertyName("companyData")]
        public DebugSavedCompanyFile? CompanyData { get; init; }
    }

    private sealed class DebugSavedCompanyFaction : CompanySavedCompanyFactionBase
    {
    }

    private sealed class DebugSavedEntry : CompanySavedCompanyEntryBase
    {
        [JsonPropertyName("Base Profile (Human Readable)")]
        public string BaseProfileHumanReadable { get; init; } = string.Empty;
        [JsonPropertyName("IsCaptain")]
        public bool IsCaptain { get; init; }
        [JsonPropertyName("FactionId")]
        public int FactionId { get; init; }
        [JsonPropertyName("ProfileId")]
        public int ProfileId { get; init; }
        [JsonPropertyName("Base CR")]
        public int BaseCr { get; init; }
        [JsonPropertyName("Calculated CR")]
        public int CalculatedCr { get; init; }
        [JsonPropertyName("Base MOV")]
        public string BaseMov { get; init; } = "-";
        [JsonPropertyName("Base CC")]
        public string BaseCc { get; init; } = "-";
        [JsonPropertyName("Base BS")]
        public string BaseBs { get; init; } = "-";
        [JsonPropertyName("Base PH")]
        public string BasePh { get; init; } = "-";
        [JsonPropertyName("Base WIP")]
        public string BaseWip { get; init; } = "-";
        [JsonPropertyName("Base ARM")]
        public string BaseArm { get; init; } = "-";
        [JsonPropertyName("Base BTS")]
        public string BaseBts { get; init; } = "-";
        [JsonPropertyName("Base VITA/STR")]
        public string BaseVitaOrStr { get; init; } = "-";
        [JsonPropertyName("Base S")]
        public string BaseS { get; init; } = "-";
    }
}
