using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Lieutenants;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.Captain;
using InfinityMercsApp.Services.CompanySelection.Common;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.CompanySelection.SpecOps;
using InfinityMercsApp.Services.CompanySelection.Units;
using TagGen = InfinityMercsApp.Infrastructure.Providers.CompanyFactions.TagCompanyFactionGenerator;

namespace InfinityMercsApp.Services.CompanySelection.StartCompany;

internal static class CompanyStartSaveWorkflow
{
    internal static async Task RunAsync<TFaction, TEntry>(CompanyStartSaveRequest<TFaction, TEntry> request)
        where TFaction : class, ICompanySourceFaction
        where TEntry : class, ICompanyMercsEntry
    {
        var entries = request.MercsCompanyEntries as IList<TEntry> ?? request.MercsCompanyEntries.ToList();
        var factions = request.Factions as IList<TFaction> ?? request.Factions.ToList();
        var captainEntry = entries.FirstOrDefault(x => x.IsLieutenant) ?? entries.FirstOrDefault();
        if (captainEntry is null)
        {
            await request.DisplayAlertAsync("Save Failed", "Add at least one unit before starting a company.", "OK");
            return;
        }

        var captainDialog = request.CaptainDialog
            ?? throw new InvalidOperationException("Starting a company needs a dialog for the captain popup.");
        var sourceFactions = CompanyUnitDetailsShared.BuildUnitSourceFactions(
            request.ShowRightSelectionBox,
            request.LeftSlotFaction,
            request.RightSlotFaction,
            faction => faction.Id);
        var firstSourceFactionId = sourceFactions.FirstOrDefault()?.Id;
        var captainPopupSkillLines = SplitCodes(captainEntry.SavedSkills);
        if (captainEntry.IsLieutenant)
        {
            LieutenantRules.AddSkillIfMissing(captainPopupSkillLines);
        }

        var captainPopupSkills = captainPopupSkillLines.Count == 0
            ? "-"
            : string.Join(Environment.NewLine, captainPopupSkillLines);

        var isTagCompanyWorkflow =
            string.Equals(request.CompanyType, "TAG Company", StringComparison.OrdinalIgnoreCase);
        Func<int, int?> resolveParentFactionId = factionId =>
            factions.FirstOrDefault(x => x.Id == factionId)?.ParentId
            ?? request.ArmyDataService.GetMetadataFactionById(factionId)?.ParentId;

        SavedImprovedCaptainStats? improvedCaptainStats;
        if (isTagCompanyWorkflow)
        {
            var preferredOtherFactionId = sourceFactions
                .Select(x => x.Id)
                .FirstOrDefault(id => id != TagGen.TagCompanyFactionId);

            improvedCaptainStats = await CompanySpecOpsWorkflowService.ShowTagSpecOpsConfigurationAsync(
                new CompanySpecOpsWorkflowRequest
                {
                    CaptainDialog = captainDialog,
                    PreferredOtherFactionId = preferredOtherFactionId > 0 ? preferredOtherFactionId : null,
                    FallbackSourceFactionId = captainEntry.SourceFactionId,
                    FirstSourceFactionId = firstSourceFactionId,
                    UnitName = captainEntry.Name,
                    UnitCost = captainEntry.CostValue,
                    UnitStatline = captainEntry.Subtitle ?? "-",
                    UnitRangedWeapons = captainEntry.SavedRangedWeapons,
                    UnitCcWeapons = captainEntry.SavedCcWeapons,
                    UnitSkills = captainPopupSkills,
                    UnitEquipment = captainEntry.SavedEquipment,
                    UnitCachedLogoPath = captainEntry.CachedLogoPath,
                    UnitPackagedLogoPath = captainEntry.PackagedLogoPath,
                    TryGetParentFactionId = resolveParentFactionId,
                    TryGetFactionName = factionId => factions.FirstOrDefault(x => x.Id == factionId)?.Name,
                    TryGetMetadataFactionName = request.TryGetMetadataFactionName,
                    ArmyDataService = request.ArmyDataService,
                    SpecOpsProvider = request.SpecOpsProvider,
                    ShowUnitsInInches = request.ShowUnitsInInches,
                    IsLieutenant = captainEntry.IsLieutenant,
                    BaseExperience = 20
                });
        }
        else
        {
            improvedCaptainStats = await CompanyCaptainWorkflowService.ShowCaptainConfigurationAsync(
                new CompanyCaptainWorkflowRequest
                {
                    CaptainDialog = captainDialog,
                    FallbackSourceFactionId = captainEntry.SourceFactionId,
                    FirstSourceFactionId = firstSourceFactionId,
                    UnitName = captainEntry.Name,
                    IsSpecOps = captainEntry.IsSpecOps,
                    UnitCost = captainEntry.CostValue,
                    UnitStatline = captainEntry.Subtitle ?? "-",
                    UnitRangedWeapons = captainEntry.SavedRangedWeapons,
                    UnitCcWeapons = captainEntry.SavedCcWeapons,
                    UnitSkills = captainPopupSkills,
                    UnitEquipment = captainEntry.SavedEquipment,
                    UnitCachedLogoPath = captainEntry.CachedLogoPath,
                    UnitPackagedLogoPath = captainEntry.PackagedLogoPath,
                    TryGetParentFactionId = resolveParentFactionId,
                    TryGetFactionName = factionId => factions.FirstOrDefault(x => x.Id == factionId)?.Name,
                    TryGetMetadataFactionName = request.TryGetMetadataFactionName,
                    ArmyDataService = request.ArmyDataService,
                    SpecOpsProvider = request.SpecOpsProvider,
                    ShowUnitsInInches = request.ShowUnitsInInches
                });
        }
        if (improvedCaptainStats is null)
        {
            return;
        }

        var saveDir = request.CompaniesDirectory;
        var filePath = Path.Combine(saveDir, NextCompanyFileName(saveDir, request.CompanyName));
        await WriteCompanyFileAsync(request, improvedCaptainStats, filePath);
        await request.NavigateToCompanyViewerAsync(filePath);
    }

    /// <summary>The next free <c>{name}-{index:D4}.json</c> file name for a company in <paramref name="saveDir"/>.</summary>
    internal static string NextCompanyFileName(string saveDir, string companyName)
    {
        var trimmedName = companyName.Trim();
        var safeFileName = Regex.Replace(trimmedName.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            safeFileName = "company";
        }

        Directory.CreateDirectory(saveDir);
        var companyIndex = CompanySelectionSharedUtilities.GetNextCompanyIndex(saveDir, trimmedName, safeFileName);
        return $"{safeFileName}-{companyIndex:D4}.json";
    }

    /// <summary>Writes the company to <paramref name="filePath"/> with the captain stats already chosen.</summary>
    internal static async Task WriteCompanyFileAsync<TFaction, TEntry>(
        CompanyStartSaveRequest<TFaction, TEntry> request,
        SavedImprovedCaptainStats improvedCaptainStats,
        string filePath)
        where TFaction : class, ICompanySourceFaction
        where TEntry : class, ICompanyMercsEntry
    {
        var entries = request.MercsCompanyEntries as IList<TEntry> ?? request.MercsCompanyEntries.ToList();
        if (entries.Count == 0)
        {
            throw new InvalidOperationException("Add at least one unit before starting a company.");
        }

        var sourceFactions = CompanyUnitDetailsShared.BuildUnitSourceFactions(
            request.ShowRightSelectionBox,
            request.LeftSlotFaction,
            request.RightSlotFaction,
            faction => faction.Id);
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
            CompanyIdentifier = CompanySelectionSharedUtilities.ComputeCompanyIdentifier(fileName),
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
            Entries = BuildSerializedEntries(entries, normalizedCaptainName, improvedCaptainStats, request.ArmyDataService)
        };

        await File.WriteAllTextAsync(
            filePath,
            JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            }));
    }

    /// <summary>
    /// New recruits as saved entries, numbered from <paramref name="firstEntryIndex"/>, each followed by its
    /// peripheral. They are written exactly as a founded company's troopers are.
    /// </summary>
    internal static IReadOnlyList<SavedCompanyEntry> RecruitEntries<TEntry>(
        IList<TEntry> recruits,
        int firstEntryIndex,
        IArmyDataService armyDataService)
        where TEntry : class, ICompanyMercsEntry
    {
        var serialized = BuildSerializedEntries(recruits, "Captain", new SavedImprovedCaptainStats(), armyDataService, firstEntryIndex);
        var json = JsonSerializer.Serialize(serialized);
        return JsonSerializer.Deserialize<List<SavedCompanyEntry>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    }

    private static List<SerializedCompanyEntry> BuildSerializedEntries<TEntry>(
        IList<TEntry> entries,
        string normalizedCaptainName,
        SavedImprovedCaptainStats improvedCaptainStats,
        IArmyDataService armyDataService,
        int firstEntryIndex = 0)
        where TEntry : class, ICompanyMercsEntry
    {
        var result = new List<SerializedCompanyEntry>(entries.Count * 2);
        var encoder = new SavedCodeEncoder(armyDataService);
        var entryIndex = firstEntryIndex;

        foreach (var entry in entries)
        {
            var primaryEntryIndex = entryIndex++;
            result.Add(CreatePrimaryEntry(
                entry,
                primaryEntryIndex,
                normalizedCaptainName,
                improvedCaptainStats,
                armyDataService,
                encoder));

            if (!entry.HasPeripheralStatBlock)
            {
                continue;
            }

            result.Add(CreatePeripheralEntry(
                entry,
                entryIndex++,
                primaryEntryIndex,
                armyDataService,
                encoder));
        }

        return result;
    }

    private static SerializedCompanyEntry CreatePrimaryEntry<TEntry>(
        TEntry entry,
        int entryIndex,
        string normalizedCaptainName,
        SavedImprovedCaptainStats improvedCaptainStats,
        IArmyDataService armyDataService,
        SavedCodeEncoder encoder)
        where TEntry : class, ICompanyMercsEntry
    {
        var baseMov = ReadStatValue(entry.Subtitle, Stat.Mov, entry.UnitMoveDisplay);
        var baseCc = ReadStatValue(entry.Subtitle, Stat.Cc);
        var baseBs = ReadStatValue(entry.Subtitle, Stat.Bs);
        var basePh = ReadStatValue(entry.Subtitle, Stat.Ph);
        var baseWip = ReadStatValue(entry.Subtitle, Stat.Wip);
        var baseArm = ReadStatValue(entry.Subtitle, Stat.Arm);
        var baseBts = ReadStatValue(entry.Subtitle, Stat.Bts);
        var baseVitaOrStr = ReadVitaOrStrValue(entry.Subtitle);
        var baseS = ReadStatValue(entry.Subtitle, Stat.S);

        var currentCc = baseCc;
        var currentBs = baseBs;
        var currentPh = basePh;
        var currentWip = baseWip;
        var currentArm = baseArm;
        var currentBts = baseBts;
        var currentVitaOrStr = baseVitaOrStr;

        var baseSkillNames = SplitCodes(entry.SavedSkills);
        var currentSkillNames = SplitCodes(entry.SavedSkills);
        var baseCharacteristicNames = SplitCodes(entry.SavedCharacteristics);
        var currentCharacteristicNames = SplitCodes(entry.SavedCharacteristics);
        var baseEquipmentNames = SplitCodes(entry.SavedEquipment);
        var currentEquipmentNames = SplitCodes(entry.SavedEquipment);
        var baseWeaponNames = SplitCodes(string.Join(Environment.NewLine, [entry.SavedRangedWeapons, entry.SavedCcWeapons]));
        var currentWeaponNames = SplitCodes(string.Join(Environment.NewLine, [entry.SavedRangedWeapons, entry.SavedCcWeapons]));

        // Persist Lieutenant explicitly for founded companies when this entry is the Lieutenant.
        if (entry.IsLieutenant &&
            !currentSkillNames.Any(x => string.Equals(x?.Trim(), LieutenantRules.SkillName, StringComparison.OrdinalIgnoreCase)))
        {
            currentSkillNames.Add(LieutenantRules.SkillName);
        }

        if (entry.IsLieutenant && improvedCaptainStats.IsEnabled)
        {
            currentCc = CaptainStatBonuses.Apply(baseCc, improvedCaptainStats.CcBonus);
            currentBs = CaptainStatBonuses.Apply(baseBs, improvedCaptainStats.BsBonus);
            currentPh = CaptainStatBonuses.Apply(basePh, improvedCaptainStats.PhBonus);
            currentWip = CaptainStatBonuses.Apply(baseWip, improvedCaptainStats.WipBonus);
            currentArm = CaptainStatBonuses.Apply(baseArm, improvedCaptainStats.ArmBonus);
            currentBts = CaptainStatBonuses.Apply(baseBts, improvedCaptainStats.BtsBonus);
            currentVitaOrStr = CaptainStatBonuses.Apply(baseVitaOrStr, improvedCaptainStats.VitalityBonus);

            currentWeaponNames = ProfileText.AppendChoices(
                currentWeaponNames,
                improvedCaptainStats.WeaponChoice1,
                improvedCaptainStats.WeaponChoice2,
                improvedCaptainStats.WeaponChoice3);
            currentSkillNames = ProfileText.AppendChoices(
                currentSkillNames,
                improvedCaptainStats.SkillChoice1,
                improvedCaptainStats.SkillChoice2,
                improvedCaptainStats.SkillChoice3);
            currentEquipmentNames = ProfileText.AppendChoices(
                currentEquipmentNames,
                improvedCaptainStats.EquipmentChoice1,
                improvedCaptainStats.EquipmentChoice2,
                improvedCaptainStats.EquipmentChoice3);
        }

        List<int> baseLookupFactions = [entry.SourceFactionId];
        List<int> currentLookupFactions = entry.IsLieutenant && improvedCaptainStats.OptionFactionId > 0
            ? [entry.SourceFactionId, improvedCaptainStats.OptionFactionId]
            : [entry.SourceFactionId];

        var baseSkillResolution = encoder.Encode(baseSkillNames, ProfileEntryKind.Skills, baseLookupFactions);
        var currentSkillResolution = encoder.Encode(currentSkillNames, ProfileEntryKind.Skills, currentLookupFactions);
        var baseCharacteristicResolution = encoder.Encode(baseCharacteristicNames, ProfileEntryKind.Chars, baseLookupFactions);
        var currentCharacteristicResolution = encoder.Encode(currentCharacteristicNames, ProfileEntryKind.Chars, currentLookupFactions);
        var baseEquipmentResolution = encoder.Encode(baseEquipmentNames, ProfileEntryKind.Equip, baseLookupFactions);
        var currentEquipmentResolution = encoder.Encode(currentEquipmentNames, ProfileEntryKind.Equip, currentLookupFactions);
        var baseWeaponResolution = encoder.Encode(baseWeaponNames, ProfileEntryKind.Weapons, baseLookupFactions);
        var currentWeaponResolution = encoder.Encode(currentWeaponNames, ProfileEntryKind.Weapons, currentLookupFactions);
        var resolvedUnitTypeCode = ResolveUnitTypeCode(entry, armyDataService);

        var customSkills = baseSkillResolution.CustomNames
            .Concat(currentSkillResolution.CustomNames)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var customCharacteristics = baseCharacteristicResolution.CustomNames
            .Concat(currentCharacteristicResolution.CustomNames)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var customEquipment = baseEquipmentResolution.CustomNames
            .Concat(currentEquipmentResolution.CustomNames)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var customWeapons = baseWeaponResolution.CustomNames
            .Concat(currentWeaponResolution.CustomNames)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new SerializedCompanyEntry
        {
            EntryIndex = entryIndex,
            Name = entry.Name,
            BaseUnitName = string.IsNullOrWhiteSpace(entry.BaseUnitName) ? entry.Name : entry.BaseUnitName,
            CustomName = entry.IsLieutenant ? normalizedCaptainName : entry.Name,
            UnitTypeCode = resolvedUnitTypeCode,
            ProfileKey = entry.ProfileKey,
            SourceFactionId = entry.SourceFactionId,
            SourceUnitId = entry.SourceUnitId,
            LogoSourceFactionId = entry.LogoSourceFactionId,
            LogoSourceUnitId = entry.LogoSourceUnitId,
            IsPeripheralUnit = false,
            ParentEntryIndex = null,
            Cost = entry.CostValue,
            IsLieutenant = entry.IsLieutenant,
            // Persist peripherals as dedicated child entries only.
            HasPeripheralStatBlock = false,
            PeripheralNameHeading = string.Empty,
            PeripheralMov = "-",
            PeripheralCc = "-",
            PeripheralBs = "-",
            PeripheralPh = "-",
            PeripheralWip = "-",
            PeripheralArm = "-",
            PeripheralBts = "-",
            PeripheralVitalityHeader = Stat.Vita.Key(),
            PeripheralVitality = "-",
            PeripheralS = "-",
            PeripheralAva = "-",
            SavedPeripheralEquipment = "-",
            SavedPeripheralSkills = "-",
            ExperiencePoints = Math.Max(0, entry.ExperiencePoints),
            Perks = CompanyPerkProgressionService.Normalize(entry.Perks),
            BaseProfileHumanReadable = string.IsNullOrWhiteSpace(entry.BaseUnitName) ? entry.Name : entry.BaseUnitName,
            IsCaptain = entry.IsLieutenant,
            FactionId = entry.SourceFactionId,
            ProfileId = entry.SourceUnitId,
            Renown = 0,
            BaseCr = entry.CostValue,
            CalculatedCr = entry.CostValue,
            BaseMov = baseMov,
            BaseCc = baseCc,
            BaseBs = baseBs,
            BasePh = basePh,
            BaseWip = baseWip,
            BaseArm = baseArm,
            BaseBts = baseBts,
            BaseVitaOrStr = baseVitaOrStr,
            BaseS = baseS,
            CurrentMov = baseMov,
            CurrentMoveFirstCm = entry.UnitMoveFirstCm,
            CurrentMoveSecondCm = entry.UnitMoveSecondCm,
            CurrentCc = currentCc,
            CurrentBs = currentBs,
            CurrentPh = currentPh,
            CurrentWip = currentWip,
            CurrentArm = currentArm,
            CurrentBts = currentBts,
            CurrentVitaOrStr = currentVitaOrStr,
            CurrentS = baseS,
            Xp = Math.Max(0, entry.ExperiencePoints),
            BaseSkillCodes = baseSkillResolution.Codes,
            CurrentSkillCodes = currentSkillResolution.Codes,
            BaseCharacteristicCodes = baseCharacteristicResolution.Codes,
            CurrentCharacteristicCodes = currentCharacteristicResolution.Codes,
            BaseEquipmentCodes = baseEquipmentResolution.Codes,
            CurrentEquipmentCodes = currentEquipmentResolution.Codes,
            BaseWeaponCodes = baseWeaponResolution.Codes,
            CurrentWeaponCodes = currentWeaponResolution.Codes,
            CustomSkills = customSkills,
            CustomCharacteristics = customCharacteristics,
            CustomEquipment = customEquipment,
            CustomWeapons = customWeapons,
            SpecOpsPurchases = [.. entry.SpecOpsPurchases]
        };
    }

    private static SerializedCompanyEntry CreatePeripheralEntry<TEntry>(
        TEntry entry,
        int entryIndex,
        int parentEntryIndex,
        IArmyDataService armyDataService,
        SavedCodeEncoder encoder)
        where TEntry : class, ICompanyMercsEntry
    {
        var peripheralName = ResolvePeripheralEntryName(entry.PeripheralNameHeading, entry.Name);
        var peripheralSkillResolution = encoder.Encode(
            SplitCodes(entry.SavedPeripheralSkills),
            ProfileEntryKind.Skills,
            [entry.SourceFactionId]);
        var peripheralEquipmentResolution = encoder.Encode(
            SplitCodes(entry.SavedPeripheralEquipment),
            ProfileEntryKind.Equip,
            [entry.SourceFactionId]);
        var peripheralCharacteristicResolution = encoder.Encode(
            SplitCodes(entry.SavedPeripheralCharacteristics),
            ProfileEntryKind.Chars,
            [entry.SourceFactionId]);
        var peripheralWeaponNames = SplitCodes(entry.SavedPeripheralRangedWeapons)
            .Concat(SplitCodes(entry.SavedPeripheralCcWeapons))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var peripheralWeaponResolution = encoder.Encode(
            peripheralWeaponNames,
            ProfileEntryKind.Weapons,
            [entry.SourceFactionId]);
        return new SerializedCompanyEntry
        {
            EntryIndex = entryIndex,
            Name = peripheralName,
            BaseUnitName = peripheralName,
            CustomName = peripheralName,
            UnitTypeCode = PeripheralTypeCode(peripheralName, entry, armyDataService),
            ProfileKey = $"{entry.ProfileKey}|peripheral",
            SourceFactionId = entry.SourceFactionId,
            SourceUnitId = entry.SourceUnitId,
            LogoSourceFactionId = entry.LogoSourceFactionId,
            LogoSourceUnitId = entry.LogoSourceUnitId,
            IsPeripheralUnit = true,
            ParentEntryIndex = parentEntryIndex,
            Cost = 0,
            IsLieutenant = false,
            HasPeripheralStatBlock = false,
            PeripheralNameHeading = string.Empty,
            PeripheralMov = "-",
            PeripheralCc = "-",
            PeripheralBs = "-",
            PeripheralPh = "-",
            PeripheralWip = "-",
            PeripheralArm = "-",
            PeripheralBts = "-",
            PeripheralVitalityHeader = Stat.Vita.Key(),
            PeripheralVitality = "-",
            PeripheralS = "-",
            PeripheralAva = "-",
            SavedPeripheralEquipment = "-",
            SavedPeripheralSkills = "-",
            ExperiencePoints = 0,
            Perks = [],
            BaseProfileHumanReadable = peripheralName,
            IsCaptain = false,
            FactionId = entry.SourceFactionId,
            ProfileId = entry.SourceUnitId,
            Renown = 0,
            BaseCr = 0,
            CalculatedCr = 0,
            BaseMov = entry.PeripheralMov,
            BaseCc = entry.PeripheralCc,
            BaseBs = entry.PeripheralBs,
            BasePh = entry.PeripheralPh,
            BaseWip = entry.PeripheralWip,
            BaseArm = entry.PeripheralArm,
            BaseBts = entry.PeripheralBts,
            BaseVitaOrStr = entry.PeripheralVitality,
            BaseS = entry.PeripheralS,
            CurrentMov = entry.PeripheralMov,
            CurrentMoveFirstCm = entry.PeripheralMoveFirstCm,
            CurrentMoveSecondCm = entry.PeripheralMoveSecondCm,
            CurrentCc = entry.PeripheralCc,
            CurrentBs = entry.PeripheralBs,
            CurrentPh = entry.PeripheralPh,
            CurrentWip = entry.PeripheralWip,
            CurrentArm = entry.PeripheralArm,
            CurrentBts = entry.PeripheralBts,
            CurrentVitaOrStr = entry.PeripheralVitality,
            CurrentS = entry.PeripheralS,
            Xp = 0,
            BaseSkillCodes = peripheralSkillResolution.Codes,
            CurrentSkillCodes = peripheralSkillResolution.Codes,
            BaseCharacteristicCodes = peripheralCharacteristicResolution.Codes,
            CurrentCharacteristicCodes = peripheralCharacteristicResolution.Codes,
            BaseEquipmentCodes = peripheralEquipmentResolution.Codes,
            CurrentEquipmentCodes = peripheralEquipmentResolution.Codes,
            BaseWeaponCodes = peripheralWeaponResolution.Codes,
            CurrentWeaponCodes = peripheralWeaponResolution.Codes,
            CustomSkills = peripheralSkillResolution.CustomNames,
            CustomCharacteristics = peripheralCharacteristicResolution.CustomNames,
            CustomEquipment = peripheralEquipmentResolution.CustomNames,
            CustomWeapons = peripheralWeaponResolution.CustomNames
        };
    }

    private static string ResolvePeripheralEntryName(string? peripheralHeading, string fallbackUnitName)
    {
        if (!string.IsNullOrWhiteSpace(peripheralHeading))
        {
            var heading = peripheralHeading.Trim();
            const string prefix = "Peripheral:";
            if (heading.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                heading = heading[prefix.Length..].Trim();
            }

            if (!string.IsNullOrWhiteSpace(heading))
            {
                return heading;
            }
        }

        return $"{fallbackUnitName} Peripheral";
    }

    private static string ReadStatValue(string? subtitle, Stat stat, string fallback = "-")
    {
        var label = stat.Key();
        if (string.IsNullOrWhiteSpace(subtitle))
        {
            return NormalizeStatValue(fallback);
        }

        var parts = subtitle.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            if (!part.StartsWith(label + " ", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = part[(label.Length + 1)..].Trim();
            return NormalizeStatValue(value);
        }

        return NormalizeStatValue(fallback);
    }

    private static string ReadVitaOrStrValue(string? subtitle)
    {
        var vita = ReadStatValue(subtitle, Stat.Vita, string.Empty);
        if (!string.IsNullOrWhiteSpace(vita) && vita != "-")
        {
            return vita;
        }

        var str = ReadStatValue(subtitle, Stat.Str, string.Empty);
        return NormalizeStatValue(str);
    }

    private static string NormalizeStatValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "-";
        }

        return value.Trim();
    }

    private static List<string> SplitCodes(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return [];
        }

        return line
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x) && x != "-")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // The peripheral's own troop type from its profile in the army data ("REM", "LI"), so it shows VITA or STR as
    // the profile does; PERIPHERAL when the army data doesn't say.
    private static string PeripheralTypeCode<TEntry>(string peripheralName, TEntry entry, IArmyDataService armyDataService)
        where TEntry : class, ICompanyMercsEntry
    {
        var profile = armyDataService.GetUnit(entry.SourceFactionId, entry.SourceUnitId)?.ProfileGroups.FindPeripheralProfile(peripheralName);
        var types = CompanyUnitDetailsShared.BuildIdNameLookup(armyDataService.GetFactionSnapshot(entry.SourceFactionId)?.Filters, ArmyFilterSection.Type);
        return profile?.Type is { } type && types.TryGetValue(type, out var typeName) && !string.IsNullOrWhiteSpace(typeName)
            ? typeName.Trim().ToUpperInvariant()
            : UnitTypeCodes.Peripheral;
    }

    private static string ResolveUnitTypeCode<TEntry>(TEntry entry, IArmyDataService armyDataService)
        where TEntry : class, ICompanyMercsEntry
    {
        if (!string.IsNullOrWhiteSpace(entry.UnitTypeCode))
        {
            return entry.UnitTypeCode.Trim().ToUpperInvariant();
        }

        var fromSubtitle = CompanyStartSharedState.ExtractUnitTypeCode(entry.Subtitle);
        if (!string.IsNullOrWhiteSpace(fromSubtitle))
        {
            return fromSubtitle.Trim().ToUpperInvariant();
        }

        if (entry.SourceFactionId <= 0 || entry.SourceUnitId <= 0)
        {
            return string.Empty;
        }

        var resume = armyDataService
            .GetResumeByFactionMercsOnly(entry.SourceFactionId)
            .FirstOrDefault(x => x.UnitId == entry.SourceUnitId);
        if (resume?.Type is not int resumeType)
        {
            return string.Empty;
        }

        var snapshot = armyDataService.GetFactionSnapshot(entry.SourceFactionId);
        var typeLookup = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot?.Filters, ArmyFilterSection.Type);
        if (!typeLookup.TryGetValue(resumeType, out var typeName) || string.IsNullOrWhiteSpace(typeName))
        {
            return string.Empty;
        }

        return typeName.Trim().ToUpperInvariant();
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

    private sealed class SerializedCompanyEntry
    {
        public int EntryIndex { get; init; }
        [JsonPropertyName("Name")]
        public string Name { get; init; } = string.Empty;
        [JsonPropertyName("Base Profile (Human Readable)")]
        public string BaseProfileHumanReadable { get; init; } = string.Empty;
        [JsonPropertyName("IsCaptain")]
        public bool IsCaptain { get; init; }
        [JsonPropertyName("FactionId")]
        public int FactionId { get; init; }
        [JsonPropertyName("ProfileId")]
        public int ProfileId { get; init; }
        [JsonPropertyName("Renown")]
        public int Renown { get; init; }
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
        [JsonPropertyName("Current MOV")]
        public string CurrentMov { get; init; } = "-";
        [JsonPropertyName("CurrentMoveFirstCm")]
        public int? CurrentMoveFirstCm { get; init; }
        [JsonPropertyName("CurrentMoveSecondCm")]
        public int? CurrentMoveSecondCm { get; init; }
        [JsonPropertyName("Current CC")]
        public string CurrentCc { get; init; } = "-";
        [JsonPropertyName("Current BS")]
        public string CurrentBs { get; init; } = "-";
        [JsonPropertyName("Current PH")]
        public string CurrentPh { get; init; } = "-";
        [JsonPropertyName("Current WIP")]
        public string CurrentWip { get; init; } = "-";
        [JsonPropertyName("Current ARM")]
        public string CurrentArm { get; init; } = "-";
        [JsonPropertyName("Current BTS")]
        public string CurrentBts { get; init; } = "-";
        [JsonPropertyName("Current VITA/STR")]
        public string CurrentVitaOrStr { get; init; } = "-";
        [JsonPropertyName("Current S")]
        public string CurrentS { get; init; } = "-";
        [JsonPropertyName("Perks")]
        public List<CompanyTrooperPerkState> Perks { get; init; } = [];
        [JsonPropertyName("XP")]
        public int Xp { get; init; }
        [JsonPropertyName("BaseSkillCodes")]
        public List<CompanySavedCodeRef> BaseSkillCodes { get; init; } = [];
        [JsonPropertyName("CurrentSkillCodes")]
        public List<CompanySavedCodeRef> CurrentSkillCodes { get; init; } = [];
        [JsonPropertyName("BaseCharacteristicCodes")]
        public List<CompanySavedCodeRef> BaseCharacteristicCodes { get; init; } = [];
        [JsonPropertyName("CurrentCharacteristicCodes")]
        public List<CompanySavedCodeRef> CurrentCharacteristicCodes { get; init; } = [];
        [JsonPropertyName("BaseEquipmentCodes")]
        public List<CompanySavedCodeRef> BaseEquipmentCodes { get; init; } = [];
        [JsonPropertyName("CurrentEquipmentCodes")]
        public List<CompanySavedCodeRef> CurrentEquipmentCodes { get; init; } = [];
        [JsonPropertyName("BaseWeaponCodes")]
        public List<CompanySavedCodeRef> BaseWeaponCodes { get; init; } = [];
        [JsonPropertyName("CurrentWeaponCodes")]
        public List<CompanySavedCodeRef> CurrentWeaponCodes { get; init; } = [];
        [JsonPropertyName("Custom Weapons")]
        public List<string> CustomWeapons { get; init; } = [];
        [JsonPropertyName("SpecOpsPurchases")]
        public List<string> SpecOpsPurchases { get; init; } = [];
        [JsonPropertyName("Custom Skills")]
        public List<string> CustomSkills { get; init; } = [];
        [JsonPropertyName("Custom Characteristics")]
        public List<string> CustomCharacteristics { get; init; } = [];
        [JsonPropertyName("Custom Equipment")]
        public List<string> CustomEquipment { get; init; } = [];
        [JsonPropertyName("Rank1Skill")]
        public string Rank1Skill { get; init; } = string.Empty;
        [JsonPropertyName("Rank2Skill")]
        public string Rank2Skill { get; init; } = string.Empty;
        [JsonPropertyName("Rank3Skill")]
        public string Rank3Skill { get; init; } = string.Empty;
        [JsonPropertyName("Rank4Skill")]
        public string Rank4Skill { get; init; } = string.Empty;
        [JsonPropertyName("Rank5Skill")]
        public string Rank5Skill { get; init; } = string.Empty;
        [JsonPropertyName("Rank6Skill")]
        public string Rank6Skill { get; init; } = string.Empty;
        [JsonPropertyName("Rank7Skill")]
        public string Rank7Skill { get; init; } = string.Empty;
        [JsonPropertyName("EquippedPrimary")]
        public string EquippedPrimary { get; init; } = string.Empty;
        [JsonPropertyName("EquippedSecondary")]
        public string EquippedSecondary { get; init; } = string.Empty;
        [JsonPropertyName("EquippedSideArm")]
        public string EquippedSideArm { get; init; } = string.Empty;
        [JsonPropertyName("EquippedAccessories")]
        public string EquippedAccessories { get; init; } = string.Empty;
        [JsonPropertyName("EquippedRoles")]
        public string EquippedRoles { get; init; } = string.Empty;
        [JsonPropertyName("EquippedArmor")]
        public string EquippedArmor { get; init; } = string.Empty;
        [JsonPropertyName("EquippedAugment")]
        public string EquippedAugment { get; init; } = string.Empty;
        [JsonPropertyName("EquippedOther")]
        public string EquippedOther { get; init; } = string.Empty;

        public string BaseUnitName { get; init; } = string.Empty;
        public string CustomName { get; init; } = string.Empty;
        public string UnitTypeCode { get; init; } = string.Empty;
        public string ProfileKey { get; init; } = string.Empty;
        public int SourceFactionId { get; init; }
        public int SourceUnitId { get; init; }
        public int LogoSourceFactionId { get; init; }
        public int LogoSourceUnitId { get; init; }
        public bool IsPeripheralUnit { get; init; }
        public int? ParentEntryIndex { get; init; }
        public int Cost { get; init; }
        public bool IsLieutenant { get; init; }
        [JsonIgnore]
        public bool HasPeripheralStatBlock { get; init; }
        [JsonIgnore]
        public string PeripheralNameHeading { get; init; } = string.Empty;
        [JsonIgnore]
        public string PeripheralMov { get; init; } = "-";
        [JsonIgnore]
        public string PeripheralCc { get; init; } = "-";
        [JsonIgnore]
        public string PeripheralBs { get; init; } = "-";
        [JsonIgnore]
        public string PeripheralPh { get; init; } = "-";
        [JsonIgnore]
        public string PeripheralWip { get; init; } = "-";
        [JsonIgnore]
        public string PeripheralArm { get; init; } = "-";
        [JsonIgnore]
        public string PeripheralBts { get; init; } = "-";
        [JsonIgnore]
        public string PeripheralVitalityHeader { get; init; } = Stat.Vita.Key();
        [JsonIgnore]
        public string PeripheralVitality { get; init; } = "-";
        [JsonIgnore]
        public string PeripheralS { get; init; } = "-";
        [JsonIgnore]
        public string PeripheralAva { get; init; } = "-";
        [JsonIgnore]
        public string SavedPeripheralEquipment { get; init; } = "-";
        [JsonIgnore]
        public string SavedPeripheralSkills { get; init; } = "-";
        public int ExperiencePoints { get; init; }
    }
}
