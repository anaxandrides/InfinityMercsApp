using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Lieutenants;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;

namespace InfinityMercsApp.Services.CompanySelection.StartCompany;

/// <summary>
/// Turns the troopers picked on a selection screen into saved company entries: their stats, their names coded
/// against the army data, the captain's upgrades, and a peripheral entry after any trooper that has one.
/// </summary>
public sealed class SavedEntryWriter(IArmyDataService armyData)
{
    /// <summary>
    /// New recruits as saved entries, numbered from <paramref name="firstEntryIndex"/>, each followed by its
    /// peripheral. They are written exactly as a founded company's troopers are.
    /// </summary>
    public IReadOnlyList<SavedCompanyEntry> RecruitEntries<TEntry>(
        IList<TEntry> recruits,
        int firstEntryIndex)
        where TEntry : class, ICompanyMercsEntry =>
        Build(recruits, "Captain", new SavedImprovedCaptainStats(), firstEntryIndex);

    /// <summary>
    /// The troopers as saved entries numbered from <paramref name="firstEntryIndex"/>, each followed by its
    /// peripheral; the lieutenant gets <paramref name="improvedCaptainStats"/>. A Servant recruited on its own is
    /// saved as its owner's peripheral, at its own cost.
    /// </summary>
    internal List<SavedCompanyEntry> Build<TEntry>(
        IList<TEntry> entries,
        string normalizedCaptainName,
        SavedImprovedCaptainStats improvedCaptainStats,
        int firstEntryIndex = 0)
        where TEntry : class, ICompanyMercsEntry
    {
        var result = new List<SavedCompanyEntry>(entries.Count * 2);
        var encoder = new SavedCodeEncoder(armyData);
        var indexes = EntryIndexes(entries, firstEntryIndex);
        var entryIndex = firstEntryIndex;

        foreach (var entry in entries)
        {
            var primaryEntryIndex = entryIndex++;
            result.Add(CreatePrimaryEntry(
                entry,
                primaryEntryIndex,
                ServantOwnerIndex(entry, indexes),
                normalizedCaptainName,
                improvedCaptainStats,
                encoder));

            if (!entry.HasPeripheralStatBlock)
            {
                continue;
            }

            result.Add(CreatePeripheralEntry(
                entry,
                entryIndex++,
                primaryEntryIndex,
                encoder));
        }

        return result;
    }

    // Where each trooper will be saved, so a Servant can point at its owner wherever the owner is in the list.
    private static Dictionary<ICompanyMercsEntry, int> EntryIndexes<TEntry>(IList<TEntry> entries, int firstEntryIndex)
        where TEntry : class, ICompanyMercsEntry
    {
        var indexes = new Dictionary<ICompanyMercsEntry, int>(ReferenceEqualityComparer.Instance);
        var entryIndex = firstEntryIndex;
        foreach (var entry in entries)
        {
            indexes[entry] = entryIndex;
            entryIndex += entry.HasPeripheralStatBlock ? 2 : 1;
        }

        return indexes;
    }

    // A Servant's owner: one saved alongside it, or one already in the company.
    private static int? ServantOwnerIndex(ICompanyMercsEntry entry, Dictionary<ICompanyMercsEntry, int> indexes) =>
        entry.ServantOwner is not { } owner ? null
        : indexes.TryGetValue(owner, out var index) ? index
        : owner.SavedEntryIndex;

    private SavedCompanyEntry CreatePrimaryEntry<TEntry>(
        TEntry entry,
        int entryIndex,
        int? servantOwnerIndex,
        string normalizedCaptainName,
        SavedImprovedCaptainStats improvedCaptainStats,
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

        var currentSkillNames = SplitCodes(entry.SavedSkills);
        var currentCharacteristicNames = SplitCodes(entry.SavedCharacteristics);
        var currentEquipmentNames = SplitCodes(entry.SavedEquipment);
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

        List<int> lookupFactions = entry.IsLieutenant && improvedCaptainStats.OptionFactionId > 0
            ? [entry.SourceFactionId, improvedCaptainStats.OptionFactionId]
            : [entry.SourceFactionId];

        var skills = encoder.Encode(currentSkillNames, ProfileEntryKind.Skills, lookupFactions);
        var characteristics = encoder.Encode(currentCharacteristicNames, ProfileEntryKind.Chars, lookupFactions);
        var equipment = encoder.Encode(currentEquipmentNames, ProfileEntryKind.Equip, lookupFactions);
        var weapons = encoder.Encode(currentWeaponNames, ProfileEntryKind.Weapons, lookupFactions);

        return new SavedCompanyEntry
        {
            EntryIndex = entryIndex,
            Name = entry.Name,
            BaseUnitName = string.IsNullOrWhiteSpace(entry.BaseUnitName) ? entry.Name : entry.BaseUnitName,
            CustomName = entry.IsLieutenant ? normalizedCaptainName
                : string.IsNullOrWhiteSpace(entry.CustomName) ? entry.Name
                : entry.CustomName.Trim(),
            UnitTypeCode = ResolveUnitTypeCode(entry),
            ProfileKey = entry.ProfileKey,
            SourceFactionId = entry.SourceFactionId,
            SourceUnitId = entry.SourceUnitId,
            LogoSourceFactionId = entry.LogoSourceFactionId,
            LogoSourceUnitId = entry.LogoSourceUnitId,
            IsPeripheralUnit = servantOwnerIndex is not null,
            ParentEntryIndex = servantOwnerIndex,
            Cost = entry.CostValue,
            IsLieutenant = entry.IsLieutenant,
            CarriesCaptainUpgrades = entry.IsLieutenant,
            IsLoneWolf = entry.IsLoneWolf,
            ExperiencePoints = Math.Max(0, entry.ExperiencePoints),
            Perks = CompanyPerkProgressionService.Normalize(entry.Perks),
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
            CurrentSkillCodes = skills.Codes,
            CurrentCharacteristicCodes = characteristics.Codes,
            CurrentEquipmentCodes = equipment.Codes,
            CurrentWeaponCodes = weapons.Codes,
            CustomSkills = skills.CustomNames,
            CustomCharacteristics = characteristics.CustomNames,
            CustomEquipment = equipment.CustomNames,
            CustomWeapons = weapons.CustomNames,
            SpecOpsPurchases = [.. entry.SpecOpsPurchases]
        };
    }

    private SavedCompanyEntry CreatePeripheralEntry<TEntry>(
        TEntry entry,
        int entryIndex,
        int parentEntryIndex,
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
        return new SavedCompanyEntry
        {
            EntryIndex = entryIndex,
            Name = peripheralName,
            BaseUnitName = peripheralName,
            CustomName = peripheralName,
            UnitTypeCode = PeripheralTypeCode(peripheralName, entry),
            ProfileKey = $"{entry.ProfileKey}|peripheral",
            SourceFactionId = entry.SourceFactionId,
            SourceUnitId = entry.SourceUnitId,
            LogoSourceFactionId = entry.LogoSourceFactionId,
            LogoSourceUnitId = entry.LogoSourceUnitId,
            IsPeripheralUnit = true,
            ParentEntryIndex = parentEntryIndex,
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
            CurrentSkillCodes = peripheralSkillResolution.Codes,
            CurrentCharacteristicCodes = peripheralCharacteristicResolution.Codes,
            CurrentEquipmentCodes = peripheralEquipmentResolution.Codes,
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

    internal static List<string> SplitCodes(string? line)
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
    private string PeripheralTypeCode<TEntry>(string peripheralName, TEntry entry)
        where TEntry : class, ICompanyMercsEntry
    {
        var profile = armyData.GetUnit(entry.SourceFactionId, entry.SourceUnitId)?.ProfileGroups.FindPeripheralProfile(peripheralName);
        var types = (armyData.GetFactionSnapshot(entry.SourceFactionId)?.Filters ?? ArmyFilterCatalog.Empty).Names(ArmyFilterSection.Type);
        return profile?.Type is { } type && types.TryGetValue(type, out var typeName) && !string.IsNullOrWhiteSpace(typeName)
            ? typeName.Trim().ToUpperInvariant()
            : UnitTypeCodes.Peripheral;
    }

    private string ResolveUnitTypeCode<TEntry>(TEntry entry)
        where TEntry : class, ICompanyMercsEntry
    {
        if (!string.IsNullOrWhiteSpace(entry.UnitTypeCode))
        {
            return entry.UnitTypeCode.Trim().ToUpperInvariant();
        }

        var fromSubtitle = UnitTypeCodes.FromSubtitle(entry.Subtitle);
        if (!string.IsNullOrWhiteSpace(fromSubtitle))
        {
            return fromSubtitle.Trim().ToUpperInvariant();
        }

        if (entry.SourceFactionId <= 0 || entry.SourceUnitId <= 0)
        {
            return string.Empty;
        }

        var resume = armyData
            .GetResumeByFactionMercsOnly(entry.SourceFactionId)
            .FirstOrDefault(x => x.UnitId == entry.SourceUnitId);
        if (resume?.Type is not int resumeType)
        {
            return string.Empty;
        }

        var snapshot = armyData.GetFactionSnapshot(entry.SourceFactionId);
        var typeLookup = (snapshot?.Filters ?? ArmyFilterCatalog.Empty).Names(ArmyFilterSection.Type);
        if (!typeLookup.TryGetValue(resumeType, out var typeName) || string.IsNullOrWhiteSpace(typeName))
        {
            return string.Empty;
        }

        return typeName.Trim().ToUpperInvariant();
    }
}
