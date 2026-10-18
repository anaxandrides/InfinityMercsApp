using System.Globalization;
using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.Traits;
using InfinityMercsApp.Services.CompanyProfiles;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.CompanySelection.Units;

namespace InfinityMercsApp.Services.CompanySelection.Profiles;

/// <summary>The configurations a selection screen lists for a unit: one per distinct option without SWC.</summary>
public static class ProfileConfigurations
{
    /// <summary>
    /// A controller's options only, when the unit has controller groups. Options that differ only by how many of a
    /// peripheral they bring are listed once, with the fewest.
    /// </summary>
    public static IReadOnlyList<ProfileConfiguration> Build(ProfileConfigurationRequest request)
    {
        var unit = request.ProfileGroups;
        if (unit.IsEmpty)
        {
            return [];
        }

        var filters = request.Filters;
        var names = new ProfileEntryNames(filters.Extras, request.ShowUnitsInInches);
        var weaponsLookup = filters.Names(ArmyFilterSection.Weapons);
        var equipLookup = filters.Names(ArmyFilterSection.Equip);
        var skillsLookup = filters.Names(ArmyFilterSection.Skills);
        var charsLookup = filters.Names(ArmyFilterSection.Chars);
        var peripheralLookup = filters.Names(ArmyFilterSection.Peripheral);
        var profiles = new List<ProfileConfiguration>();
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
                if (request.FireteamOnly && !FireteamMatching.IsFtoLabel(optionNameForFilter))
                {
                    continue;
                }

                foreach (var name in names.OrderedUnique(
                             unit.GetEntriesWithIncludes(option, ProfileEntryKind.Equip),
                             equipLookup))
                {
                    equipUsageCounts[name] = equipUsageCounts.TryGetValue(name, out var count) ? count + 1 : 1;
                }

                var optionSkillNames = CompanyProfileText.BuildConfigurationSkillNames(
                    names.OrderedUnique(
                        unit.GetEntriesWithIncludes(option, ProfileEntryKind.Skills),
                        skillsLookup));
                foreach (var name in optionSkillNames)
                {
                    skillUsageCounts[name] = skillUsageCounts.TryGetValue(name, out var count) ? count + 1 : 1;
                }
            }
        }

        var bestConfigurationByKey = new Dictionary<string, ListedConfiguration>(StringComparer.OrdinalIgnoreCase);

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
                if (request.FireteamOnly && !FireteamMatching.IsFtoLabel(optionName))
                {
                    continue;
                }

                optionName = BuildOptionDisplayName(option, optionName, equipLookup, skillsLookup);

                var optionWeapons = names.OrderedUnique(
                    unit.GetEntriesWithIncludes(option, ProfileEntryKind.Weapons),
                    weaponsLookup);
                var rangedWeaponNames = optionWeapons.Where(x => !CompanyProfileText.IsMeleeWeaponName(x)).ToList();
                var meleeWeaponNames = optionWeapons.Where(CompanyProfileText.IsMeleeWeaponName).ToList();

                var optionEquipmentNames = names.OrderedUnique(
                        unit.GetEntriesWithIncludes(option, ProfileEntryKind.Equip),
                        equipLookup)
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

                var optionSkillNames = CompanyProfileText.BuildConfigurationSkillNames(
                    names.OrderedUnique(
                        unit.GetEntriesWithIncludes(option, ProfileEntryKind.Skills),
                        skillsLookup));
                var optionCharacteristicNames = names.OrderedUnique(
                        unit.GetEntriesWithIncludes(option, ProfileEntryKind.Chars),
                        charsLookup)
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
                    peripheralLookup);
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
                        peripheralStats = CompanyPeripheralStatBlockService.Build(extractedPeripheralName, peripheralProfile, filters, request.ShowUnitsInInches);
                    }
                }

                var peripheralSkills = peripheralStats?.Skills ?? "-";
                var peripheralHasControlModifier = HasPeripheralControlModifier(peripheralNames, peripheralSkills);
                var ftMasterPeripheralName = ResolveFtMasterPeripheralName(peripheralUnitName);
                if (peripheralHasControlModifier && !string.IsNullOrWhiteSpace(ftMasterPeripheralName))
                {
                    EnsureSkill(uniqueSkillsNames, PeripheralControl.FtMaster(ftMasterPeripheralName));
                }

                var adjustedCost = unit.GetAdjustedCost(group, option);
                var cost = request.TacticalAwarenessDiscount > 0 && HasTacticalAwareness(unit, group, option, skillsLookup)
                    ? Discounted(adjustedCost, request.TacticalAwarenessDiscount)
                    : adjustedCost;
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
                bestConfigurationByKey.TryGetValue(dedupeKey, out var existingConfiguration);
                if (existingConfiguration is not null && peripheralCount >= existingConfiguration.PeripheralCount)
                {
                    continue;
                }

                var isLieutenant = request.ForceLieutenant || option.IsLieutenant(skillsLookup);
                var profileKey = $"{groupName}|{optionName}|{displayCost}|{swc}|lt:{(isLieutenant ? 1 : 0)}";
                var peripheralEquipment = peripheralStats?.Equipment ?? "-";
                if (peripheralHasControlModifier)
                {
                    peripheralSkills = EnsureSkillLine(peripheralSkills, "Irregular");
                }

                var profileItem = new ProfileConfiguration(
                    groupName,
                    optionName,
                    profileKey,
                    isLieutenant,
                    request.ForceLieutenant,
                    rangedWeaponNames,
                    meleeWeaponNames,
                    uniqueEquipmentNames,
                    uniqueSkillsNames,
                    optionCharacteristicNames,
                    displayPeripheralNames,
                    peripheralStats,
                    peripheralEquipment,
                    peripheralSkills,
                    peripheralHasControlModifier,
                    ftMasterPeripheralName,
                    swc,
                    displayCost,
                    hasTacticalAwareness || optionSkillNames.Any(x => x.Contains("tactical awareness", StringComparison.OrdinalIgnoreCase)));

                if (existingConfiguration is not null)
                {
                    profiles[existingConfiguration.Index] = profileItem;
                    bestConfigurationByKey[dedupeKey] = existingConfiguration with { PeripheralCount = peripheralCount };
                    continue;
                }

                profiles.Add(profileItem);
                bestConfigurationByKey[dedupeKey] = new ListedConfiguration(profiles.Count - 1, peripheralCount);
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

    private static bool HasPeripheralControlModifier(IEnumerable<string> peripheralNames, string? peripheralSkills) =>
        peripheralNames.Any(PeripheralControl.Mentions) || PeripheralControl.Mentions(peripheralSkills);

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
        var lines = CompanyProfileText.SplitDisplayLine(skillsLine).ToList();
        if (!lines.Any(x => string.Equals(x, skill, StringComparison.OrdinalIgnoreCase)))
        {
            lines.Add(skill);
        }

        return CompanyProfileText.JoinOrDash(lines);
    }

    private static bool HasTacticalAwareness(
        UnitProfileGroups unit, ProfileGroup group, ProfileOption option, IReadOnlyDictionary<int, string> skillsLookup)
    {
        bool Named(IEnumerable<ProfileEntry> entries) => entries.Any(entry =>
            entry.Id is int id &&
            skillsLookup.TryGetValue(id, out var name) &&
            name.Contains("tactical awareness", StringComparison.OrdinalIgnoreCase));

        return Named(unit.GetEntriesWithIncludes(option, ProfileEntryKind.Skills)) || group.Profiles.Any(profile => Named(profile.Skills));
    }

    // A cost less the discount, never below 0; a cost that isn't a number stays as it is.
    private static string Discounted(string cost, int discount) =>
        int.TryParse(cost, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? Math.Max(0, parsed - discount).ToString(CultureInfo.InvariantCulture)
            : cost;

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
