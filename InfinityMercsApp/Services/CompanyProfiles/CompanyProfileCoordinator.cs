using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Models.Army.Traits;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.CompanySelection.Units;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;

namespace InfinityMercsApp.Services.CompanyProfiles;

internal sealed class CompanyProfileCoordinator
{
    /// <summary>
    /// Builds display-ready viewer profiles for a unit's visible options.
    /// </summary>
    public IReadOnlyList<ViewerProfileItem> BuildProfiles(CompanyProfileBuildRequest request)
    {
        var unit = request.ProfileGroups;
        var names = request.EntryNames;
        var profiles = new List<ViewerProfileItem>();
        var equipUsageCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var skillUsageCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var hasControllerGroups = unit.Groups.Any(unit.IsControllerGroup);

        foreach (var group in unit.Groups)
        {
            if (hasControllerGroups && !unit.IsControllerGroup(group))
            {
                continue;
            }

            var groupName = ReadGroupName(group);

            foreach (var option in group.Options)
            {
                if (option.CostsSwc)
                {
                    continue;
                }

                var optionNameForFilter = ReadOptionNameOrGroup(option, groupName);
                if (!request.ShouldIncludeOption(group, option, optionNameForFilter))
                {
                    continue;
                }

                foreach (var name in names.OrderedUnique(
                             unit.GetEntriesWithIncludes(option, ProfileEntryKind.Equip),
                             request.EquipLookup))
                {
                    equipUsageCounts[name] = equipUsageCounts.TryGetValue(name, out var count) ? count + 1 : 1;
                }

                var optionSkillNames = CompanyProfileTextService.BuildConfigurationSkillNames(
                    names.OrderedUnique(
                        unit.GetEntriesWithIncludes(option, ProfileEntryKind.Skills),
                        request.SkillsLookup));
                foreach (var name in optionSkillNames)
                {
                    skillUsageCounts[name] = skillUsageCounts.TryGetValue(name, out var count) ? count + 1 : 1;
                }
            }
        }

        var bestConfigurationByKey = new Dictionary<string, (int Index, int PeripheralCount)>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in unit.Groups)
        {
            if (hasControllerGroups && !unit.IsControllerGroup(group))
            {
                continue;
            }

            var groupName = ReadGroupName(group);
            foreach (var option in group.Options)
            {
                var swc = option.Swc;
                if (option.CostsSwc)
                {
                    continue;
                }

                var optionName = ReadOptionNameOrGroup(option, groupName);
                if (!request.ShouldIncludeOption(group, option, optionName))
                {
                    continue;
                }

                optionName = BuildOptionDisplayName(option, optionName, request.EquipLookup, request.SkillsLookup);

                var optionWeapons = names.OrderedUnique(
                    unit.GetEntriesWithIncludes(option, ProfileEntryKind.Weapons),
                    request.WeaponsLookup);
                var rangedWeaponNames = optionWeapons.Where(x => !CompanyProfileTextService.IsMeleeWeaponName(x)).ToList();
                var meleeWeaponNames = optionWeapons.Where(CompanyProfileTextService.IsMeleeWeaponName).ToList();

                var optionEquipmentNames = names.OrderedUnique(
                        unit.GetEntriesWithIncludes(option, ProfileEntryKind.Equip),
                        request.EquipLookup)
                    .ToList();
                var uniqueEquipmentNames = optionEquipmentNames
                    .Where(x => equipUsageCounts.TryGetValue(x, out var c) && c == 1)
                    .ToList();
                if (uniqueEquipmentNames.Count == 0)
                {
                    uniqueEquipmentNames = optionEquipmentNames
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }

                var optionSkillNames = CompanyProfileTextService.BuildConfigurationSkillNames(
                    names.OrderedUnique(
                        unit.GetEntriesWithIncludes(option, ProfileEntryKind.Skills),
                        request.SkillsLookup));
                var optionCharacteristicNames = names.OrderedUnique(
                        unit.GetEntriesWithIncludes(option, ProfileEntryKind.Chars),
                        request.CharsLookup)
                    .ToList();
                var (hasRegular, hasIrregular, hasImpetuous, hasTacticalAwareness) = UnitTraits.ReadOrderTraits(option);
                var (hasCube, hasCube2, hasHackable) = UnitTraits.ReadTechTraits(
                    optionEquipmentNames.Concat(optionSkillNames).Concat(optionCharacteristicNames));
                EnsureCharacteristic(optionCharacteristicNames, hasRegular, "Regular");
                EnsureCharacteristic(optionCharacteristicNames, hasIrregular, "Irregular");
                EnsureCharacteristic(optionCharacteristicNames, hasImpetuous, "Impetuous");
                EnsureCharacteristic(optionCharacteristicNames, hasTacticalAwareness, "Tactical Awareness");
                EnsureCharacteristic(optionCharacteristicNames, hasCube, "Cube");
                EnsureCharacteristic(optionCharacteristicNames, hasCube2, "Cube 2.0");
                EnsureCharacteristic(optionCharacteristicNames, hasHackable, "Hackable");
                var uniqueSkillsNames = optionSkillNames
                    .Where(x => skillUsageCounts.TryGetValue(x, out var c) && c == 1)
                    .ToList();
                if (uniqueSkillsNames.Count == 0)
                {
                    uniqueSkillsNames = optionSkillNames
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }

                var peripheralNames = names.Counted(
                    unit.GetDisplayPeripheralEntries(group, option),
                    request.PeripheralLookup);
                var firstPeripheralName = peripheralNames.FirstOrDefault();
                var peripheralUnitName = CompanyUnitDetailsShared.ExtractFirstPeripheralName(firstPeripheralName);

                PeripheralMercsCompanyStats? peripheralStats = null;
                var hasPeripheralProfile = false;

                if (!string.IsNullOrWhiteSpace(firstPeripheralName))
                {
                    var extractedPeripheralName = CompanyUnitDetailsShared.ExtractFirstPeripheralName(firstPeripheralName);
                    var peripheralProfile = unit.FindPeripheralProfile(extractedPeripheralName);
                    if (peripheralProfile is not null)
                    {
                        hasPeripheralProfile = true;
                        peripheralStats = request.BuildPeripheralStatBlock(extractedPeripheralName, peripheralProfile);
                    }
                }

                var peripheralSkills = peripheralStats?.Skills ?? "-";
                var peripheralHasControlModifier = HasPeripheralControlModifier(peripheralNames, peripheralSkills);
                var ftMasterPeripheralName = ResolveFtMasterPeripheralName(peripheralUnitName);
                if (peripheralHasControlModifier && !string.IsNullOrWhiteSpace(ftMasterPeripheralName))
                {
                    EnsureSkill(uniqueSkillsNames, $"FT Master ({ftMasterPeripheralName})");
                }

                var cost = request.ReadAdjustedOptionCost(group, option);
                var displayPeripheralNames = peripheralNames;
                var displayCost = cost;

                if (CompanyUnitDetailsShared.TryBuildSinglePeripheralDisplay(peripheralNames, out var singlePeripheralName, out var singlePeripheralCount) &&
                    singlePeripheralCount > 1)
                {
                    displayPeripheralNames = [$"{singlePeripheralName} (1)"];

                    if (hasPeripheralProfile)
                    {
                        displayCost = UnitOptionFilter.SinglePeripheralCost(unit, cost, singlePeripheralName, singlePeripheralCount);
                    }
                }

                var normalizedPeripheralNames = peripheralNames
                    .Select(CompanyUnitDetailsShared.NormalizePeripheralNameForDedupe)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();
                var peripheralCount = CompanyUnitDetailsShared.GetPeripheralTotalCount(peripheralNames);
                var dedupeKey =
                    $"{groupName}|{optionName}|{string.Join("|", rangedWeaponNames)}|{string.Join("|", meleeWeaponNames)}|{string.Join("|", uniqueEquipmentNames)}|{string.Join("|", uniqueSkillsNames)}|{string.Join("|", normalizedPeripheralNames)}|{swc}";
                var hasExisting = bestConfigurationByKey.TryGetValue(dedupeKey, out var existingConfiguration);
                if (hasExisting && peripheralCount >= existingConfiguration.PeripheralCount)
                {
                    continue;
                }

                var isLieutenant = request.ForceLieutenant || option.IsLieutenant(request.SkillsLookup);
                var profileKey = $"{groupName}|{optionName}|{displayCost}|{swc}|lt:{(isLieutenant ? 1 : 0)}";
                var peripheralEquipment = peripheralStats?.Equipment ?? "-";
                if (peripheralHasControlModifier)
                {
                    peripheralSkills = EnsureSkillLine(peripheralSkills, "Irregular");
                }
                var hasPeripheralStats = peripheralStats is not null;
                var profileItem = new ViewerProfileItem
                {
                    GroupName = groupName,
                    Name = optionName,
                    ProfileKey = profileKey,
                    IsLieutenant = isLieutenant,
                    NameFormatted = CompanyProfileTextService.BuildNameFormatted(optionName),
                    RangedWeapons = CompanyProfileTextService.JoinOrDash(rangedWeaponNames),
                    RangedWeaponsFormatted = CompanyProfileTextService.BuildListFormattedString(rangedWeaponNames, Color.FromArgb("#EF4444")),
                    MeleeWeapons = CompanyProfileTextService.JoinOrDash(meleeWeaponNames),
                    MeleeWeaponsFormatted = CompanyProfileTextService.BuildListFormattedString(meleeWeaponNames, Color.FromArgb("#22C55E")),
                    UniqueEquipment = CompanyProfileTextService.JoinOrDash(uniqueEquipmentNames),
                    UniqueEquipmentFormatted = CompanyProfileTextService.BuildListFormattedString(uniqueEquipmentNames, Color.FromArgb("#06B6D4")),
                    UniqueSkills = CompanyProfileTextService.JoinOrDash(uniqueSkillsNames),
                    UniqueSkillsFormatted = CompanyProfileTextService.BuildListFormattedString(
                        uniqueSkillsNames,
                        Color.FromArgb("#F59E0B"),
                        highlightLieutenantPurple: request.ForceLieutenant),
                    Characteristics = CompanyProfileTextService.JoinOrDash(optionCharacteristicNames),
                    Peripherals = CompanyProfileTextService.JoinOrDash(displayPeripheralNames),
                    PeripheralsFormatted = CompanyProfileTextService.BuildListFormattedString(displayPeripheralNames, Color.FromArgb("#FACC15")),
                    HasPeripheralStatBlock = hasPeripheralStats,
                    PeripheralNameHeading = peripheralStats?.NameHeading ?? string.Empty,
                    PeripheralMov = peripheralStats is not null
                        ? request.FormatMoveValue(peripheralStats.MoveFirstCm, peripheralStats.MoveSecondCm)
                        : "-",
                    PeripheralCc = peripheralStats?.Cc ?? "-",
                    PeripheralBs = peripheralStats?.Bs ?? "-",
                    PeripheralPh = peripheralStats?.Ph ?? "-",
                    PeripheralWip = peripheralStats?.Wip ?? "-",
                    PeripheralArm = peripheralStats?.Arm ?? "-",
                    PeripheralBts = peripheralStats?.Bts ?? "-",
                    PeripheralVitalityHeader = peripheralStats?.VitalityHeader ?? Stat.Vita.Key(),
                    PeripheralVitality = peripheralStats?.Vitality ?? "-",
                    PeripheralS = peripheralStats?.S ?? "-",
                    PeripheralAva = peripheralStats?.Ava ?? "-",
                    PeripheralSubtitle = CompanyUnitDetailsShared.BuildPeripheralSubtitle(peripheralStats),
                    PeripheralEquipmentLineFormatted = CompanyProfileTextService.BuildMercsCompanyLineFormatted("Equipment", peripheralEquipment, Color.FromArgb("#06B6D4")),
                    HasPeripheralEquipmentLine = hasPeripheralStats && !string.IsNullOrWhiteSpace(peripheralEquipment) && peripheralEquipment != "-",
                    PeripheralSkillsLineFormatted = CompanyProfileTextService.BuildMercsCompanyLineFormatted("Skills", peripheralSkills, Color.FromArgb("#F59E0B")),
                    HasPeripheralSkillsLine = hasPeripheralStats && !string.IsNullOrWhiteSpace(peripheralSkills) && peripheralSkills != "-",
                    PeripheralGrantsFtMaster = peripheralHasControlModifier && !string.IsNullOrWhiteSpace(ftMasterPeripheralName),
                    PeripheralIsIrregular = peripheralHasControlModifier,
                    PeripheralUnitName = ftMasterPeripheralName,
                    Swc = swc,
                    SwcDisplay = $"SWC {swc}",
                    Cost = displayCost,
                    ShowProfileTacticalAwarenessIcon = !request.ShowTacticalAwarenessIcon &&
                                                       (hasTacticalAwareness || optionSkillNames.Any(x => x.Contains("tactical awareness", StringComparison.OrdinalIgnoreCase)))
                };

                if (hasExisting)
                {
                    profiles[existingConfiguration.Index] = profileItem;
                    bestConfigurationByKey[dedupeKey] = (existingConfiguration.Index, peripheralCount);
                    continue;
                }

                profiles.Add(profileItem);
                bestConfigurationByKey[dedupeKey] = (profiles.Count - 1, peripheralCount);
            }
        }

        return profiles;
    }

    /// <summary>
    /// Appends detail tags (like lieutenant/FO/hacker) to a base option display name.
    /// </summary>
    private static string BuildOptionDisplayName(
        ProfileOption option,
        string baseName,
        IReadOnlyDictionary<int, string> equipLookup,
        IReadOnlyDictionary<int, string> skillsLookup)
    {
        var details = new List<string>();
        var normalizedBase = baseName.ToLowerInvariant();

        foreach (var skillName in GetOrderedNames(option.Skills, skillsLookup))
        {
            if (IsNameDetailTag(skillName) && !normalizedBase.Contains(skillName.ToLowerInvariant()))
            {
                details.Add(skillName);
            }
        }

        foreach (var equipName in GetOrderedNames(option.Equip, equipLookup))
        {
            if (IsNameDetailTag(equipName) && !normalizedBase.Contains(equipName.ToLowerInvariant()))
            {
                details.Add(equipName);
            }
        }

        foreach (var order in option.Orders)
        {
            if (order.IsLieutenant && !normalizedBase.Contains("lieutenant"))
            {
                details.Add("Lieutenant");
            }
        }

        var distinctDetails = details
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (distinctDetails.Count == 0)
        {
            return baseName;
        }

        return $"{baseName} ({string.Join(", ", distinctDetails)})";
    }

    /// <summary>
    /// Resolves an option's own entries (without includes) to known names, distinct and sorted.
    /// </summary>
    private static List<string> GetOrderedNames(
        IEnumerable<ProfileEntry> entries,
        IReadOnlyDictionary<int, string> lookup)
    {
        var names = new List<string>();
        foreach (var entry in entries)
        {
            if (entry.Id is int id && lookup.TryGetValue(id, out var resolvedName) && !string.IsNullOrWhiteSpace(resolvedName))
            {
                names.Add(resolvedName);
            }
        }

        return names
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Returns true when a name qualifies as a role/detail tag for display names.
    /// </summary>
    private static bool IsNameDetailTag(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Contains("forward observer", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("hacker", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("hacking device", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("specialist", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("paramedic", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("doctor", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("engineer", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("lieutenant", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("nco", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("chain of command", StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsureCharacteristic(ICollection<string> characteristics, bool condition, string name)
    {
        if (!condition || string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        if (characteristics.Any(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        characteristics.Add(name);
    }

    private static bool HasPeripheralControlModifier(IEnumerable<string> peripheralNames, string? peripheralSkills)
    {
        foreach (var peripheralName in peripheralNames)
        {
            if (string.IsNullOrWhiteSpace(peripheralName))
            {
                continue;
            }

            if (peripheralName.Contains("synchronized", StringComparison.OrdinalIgnoreCase) ||
                peripheralName.Contains("control", StringComparison.OrdinalIgnoreCase) ||
                peripheralName.Contains("cyberplug", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        if (!string.IsNullOrWhiteSpace(peripheralSkills) &&
            (peripheralSkills.Contains("synchronized", StringComparison.OrdinalIgnoreCase) ||
             peripheralSkills.Contains("control", StringComparison.OrdinalIgnoreCase) ||
             peripheralSkills.Contains("cyberplug", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    private static string ResolveFtMasterPeripheralName(string? peripheralUnitName)
    {
        if (string.IsNullOrWhiteSpace(peripheralUnitName))
        {
            return string.Empty;
        }

        var trimmed = peripheralUnitName.Trim();
        trimmed = Regex.Replace(trimmed, @"\s*\(\d+\)\s*$", string.Empty);
        trimmed = Regex.Replace(trimmed, @"\s*\((?:synchronized|control|cyberplug)\)\s*$", string.Empty, RegexOptions.IgnoreCase);
        return trimmed.Trim();
    }

    private static void EnsureSkill(List<string> skills, string skill)
    {
        if (skills.Any(x => string.Equals(x, skill, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        skills.Add(skill);
    }

    private static string EnsureSkillLine(string? skillsLine, string skill)
    {
        var lines = CompanyProfileTextService.SplitDisplayLine(skillsLine).ToList();
        if (!lines.Any(x => string.Equals(x, skill, StringComparison.OrdinalIgnoreCase)))
        {
            lines.Add(skill);
        }

        return CompanyProfileTextService.JoinOrDash(lines);
    }

    /// <summary>
    /// Reads the profile group display name.
    /// </summary>
    private static string ReadGroupName(ProfileGroup group)
    {
        return group.Isc ?? string.Empty;
    }

    /// <summary>
    /// Reads option name, falling back to group name when blank.
    /// </summary>
    private static string ReadOptionNameOrGroup(ProfileOption option, string groupName)
    {
        return string.IsNullOrWhiteSpace(option.Name) ? groupName : option.Name;
    }
}
