// FROZEN LEGACY ORACLE — verbatim copy of production JSON rules from main@224e1e2.
// Do not edit or "fix" this file. Characterization tests run it against real Infinity Army
// payloads so replacement code can be proven to produce identical results.
// Sources: CompanySelectionSharedUtilities.DisplayText.ConvertDistanceText, CompanyUnitTraitService,
// CompanyPeripheralProfileSelectionService, ArmyDataService.ReadMoveValue (cm display mode),
// CompanyUnitDetailsShared.ReadVitality/ReadAvaAsString.
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace InfinityMercsApp.UnitTests.Legacy;

internal sealed class LegacyPeripheralSelectionResult
{
    public required string PeripheralName { get; init; }
    public required JsonElement PeripheralProfile { get; init; }
}

internal sealed class LegacyPeripheralSelectionRequest
{
    public required JsonElement ProfileGroupsRoot { get; init; }
    public required IReadOnlyDictionary<int, string> PeripheralLookup { get; init; }
    public required bool ForceLieutenant { get; init; }
    public required bool LieutenantOnlyUnits { get; init; }
    public required IReadOnlyDictionary<int, string> SkillsLookup { get; init; }
    public required Func<JsonElement, IReadOnlyDictionary<int, string>, bool> IsLieutenantOption { get; init; }
    public required LegacyDisplayNameContext.TryParseIdDelegate TryParseId { get; init; }
}

internal static class LegacyUnitDetailRules
{
    internal static string ConvertDistanceText(string distanceText, bool showUnitsInInches)
    {
        if (string.IsNullOrWhiteSpace(distanceText))
        {
            return distanceText;
        }

        var match = Regex.Match(
            distanceText,
            @"([+-]?)(\d+(?:\.\d+)?)(?:\s*(cm|""|in|inch|inches))?",
            RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return distanceText;
        }

        var sign = match.Groups[1].Value;
        var numberText = match.Groups[2].Value;
        if (!double.TryParse(
                numberText,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var parsedValue))
        {
            return distanceText;
        }

        var unitToken = match.Groups[3].Success
            ? match.Groups[3].Value.Trim().ToLowerInvariant()
            : string.Empty;
        var valueInCm = unitToken is "\"" or "in" or "inch" or "inches"
            ? parsedValue * 2.5
            : parsedValue;

        string replacement;
        if (showUnitsInInches)
        {
            var inches = (int)Math.Round(valueInCm / 2.5, MidpointRounding.AwayFromZero);
            replacement = $"{sign}{inches}\"";
        }
        else
        {
            var roundedCm = Math.Round(valueInCm, 2, MidpointRounding.AwayFromZero);
            var cmText = Math.Abs(roundedCm - Math.Round(roundedCm)) < 0.001
                ? ((int)Math.Round(roundedCm)).ToString(System.Globalization.CultureInfo.InvariantCulture)
                : roundedCm.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            replacement = $"{sign}{cmText}cm";
        }

        return distanceText.Remove(match.Index, match.Length).Insert(match.Index, replacement);
    }

    internal static (bool HasRegular, bool HasIrregular, bool HasImpetuous, bool HasTacticalAwareness) ParseUnitOrderTraits(JsonElement profileGroupsArray)
    {
        var hasRegular = false;
        var hasIrregular = false;
        var hasImpetuous = false;
        var optionsSeen = 0;
        var tacticalOptions = 0;

        if (profileGroupsArray.ValueKind != JsonValueKind.Array)
        {
            return (false, false, false, false);
        }

        foreach (var group in profileGroupsArray.EnumerateArray())
        {
            if (!group.TryGetProperty("options", out var optionsElement) || optionsElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var option in optionsElement.EnumerateArray())
            {
                optionsSeen++;
                var optionHasTactical = false;
                if (!option.TryGetProperty("orders", out var ordersElement) || ordersElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var order in ordersElement.EnumerateArray())
                {
                    if (!order.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    var type = typeElement.GetString();
                    if (string.IsNullOrWhiteSpace(type))
                    {
                        continue;
                    }

                    if (string.Equals(type, "REGULAR", StringComparison.OrdinalIgnoreCase))
                    {
                        hasRegular = true;
                    }
                    else if (string.Equals(type, "IRREGULAR", StringComparison.OrdinalIgnoreCase))
                    {
                        hasIrregular = true;
                    }
                    else if (string.Equals(type, "IMPETUOUS", StringComparison.OrdinalIgnoreCase))
                    {
                        hasImpetuous = true;
                    }
                    else if (string.Equals(type, "TACTICAL", StringComparison.OrdinalIgnoreCase))
                    {
                        optionHasTactical = true;
                    }
                }

                if (optionHasTactical)
                {
                    tacticalOptions++;
                }
            }
        }

        var hasUnitWideTacticalAwareness = optionsSeen > 0 && tacticalOptions == optionsSeen;
        return (hasRegular, hasIrregular, hasImpetuous, hasUnitWideTacticalAwareness);
    }

    internal static (bool HasCube, bool HasCube2, bool HasHackable) ParseUnitTechTraits(
        JsonElement profileGroupsArray,
        IReadOnlyDictionary<int, string> equipLookup,
        IReadOnlyDictionary<int, string> skillsLookup,
        IReadOnlyDictionary<int, string> charsLookup)
    {
        var hasCube = false;
        var hasCube2 = false;
        var hasHackable = false;

        if (profileGroupsArray.ValueKind != JsonValueKind.Array)
        {
            return (false, false, false);
        }

        foreach (var group in profileGroupsArray.EnumerateArray())
        {
            if (group.TryGetProperty("profiles", out var profilesElement) && profilesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var profile in profilesElement.EnumerateArray())
                {
                    ApplyTechTraitsFromContainer(profile, "equip", equipLookup, ref hasCube, ref hasCube2, ref hasHackable);
                    ApplyTechTraitsFromContainer(profile, "skills", skillsLookup, ref hasCube, ref hasCube2, ref hasHackable);
                    ApplyTechTraitsFromContainer(profile, "chars", charsLookup, ref hasCube, ref hasCube2, ref hasHackable);
                }
            }

            if (group.TryGetProperty("options", out var optionsElement) && optionsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var option in optionsElement.EnumerateArray())
                {
                    ApplyTechTraitsFromContainer(option, "equip", equipLookup, ref hasCube, ref hasCube2, ref hasHackable);
                    ApplyTechTraitsFromContainer(option, "skills", skillsLookup, ref hasCube, ref hasCube2, ref hasHackable);
                    ApplyTechTraitsFromContainer(option, "chars", charsLookup, ref hasCube, ref hasCube2, ref hasHackable);
                }
            }
        }

        return (hasCube, hasCube2, hasHackable);
    }

    internal static void ApplyTechTraitsFromContainer(
        JsonElement container,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup,
        ref bool hasCube,
        ref bool hasCube2,
        ref bool hasHackable)
    {
        if (!container.TryGetProperty(propertyName, out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var entry in arr.EnumerateArray())
        {
            if (!LegacySharedUtilities.TryParseId(entry, out var id) || !lookup.TryGetValue(id, out var name))
            {
                continue;
            }

            ApplyTechTraitName(name, ref hasCube, ref hasCube2, ref hasHackable);
        }
    }

    internal static void ApplyTechTraitName(string name, ref bool hasCube, ref bool hasCube2, ref bool hasHackable)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var normalized = NormalizeTokenText(name);

        if (Regex.IsMatch(normalized, @"\bhackable\b", RegexOptions.IgnoreCase))
        {
            hasHackable = true;
        }

        var hasNegativeCube = Regex.IsMatch(
            normalized,
            @"\b(no[\s-]*cube|without[\s-]*cube|cube[\s-]*none)\b",
            RegexOptions.IgnoreCase);

        if (hasNegativeCube)
        {
            return;
        }

        var isCube2 = Regex.IsMatch(
            normalized,
            @"\bcube\s*2(?:\.0)?\b|\bcube2(?:\.0)?\b",
            RegexOptions.IgnoreCase);

        if (isCube2)
        {
            hasCube2 = true;
            return;
        }

        if (Regex.IsMatch(normalized, @"\bcube\b", RegexOptions.IgnoreCase))
        {
            hasCube = true;
        }
    }

    internal static string NormalizeTokenText(string value)
    {
        var lowered = value.ToLowerInvariant();
        var sb = new StringBuilder(lowered.Length);
        foreach (var c in lowered)
        {
            if (char.IsLetterOrDigit(c) || c == '.')
            {
                sb.Append(c);
            }
            else
            {
                sb.Append(' ');
            }
        }

        return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
    }

    internal static LegacyPeripheralSelectionResult? FindFirstVisiblePeripheralProfile(
        LegacyPeripheralSelectionRequest request)
    {
        if (request.ProfileGroupsRoot.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var hasControllerGroups = request.ProfileGroupsRoot.EnumerateArray()
            .Any(group => LegacyCompanyProfileOptionService.IsControllerGroup(request.ProfileGroupsRoot, group));

        foreach (var group in request.ProfileGroupsRoot.EnumerateArray())
        {
            if (hasControllerGroups && !LegacyCompanyProfileOptionService.IsControllerGroup(request.ProfileGroupsRoot, group))
            {
                continue;
            }

            if (!group.TryGetProperty("options", out var optionsElement) || optionsElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var option in optionsElement.EnumerateArray())
            {
                if (LegacySharedUtilities.IsPositiveSwc(LegacyCompanyProfileOptionService.ReadOptionSwc(option)))
                {
                    continue;
                }

                if (!request.ForceLieutenant && request.LieutenantOnlyUnits && !request.IsLieutenantOption(option, request.SkillsLookup))
                {
                    continue;
                }

                foreach (var entry in LegacyCompanyProfileOptionService.GetDisplayPeripheralEntriesForOption(request.ProfileGroupsRoot, group, option))
                {
                    if (!request.TryParseId(entry, out var peripheralId))
                    {
                        continue;
                    }

                    var peripheralName = request.PeripheralLookup.TryGetValue(peripheralId, out var resolvedName)
                        ? resolvedName
                        : peripheralId.ToString(CultureInfo.InvariantCulture);

                    if (!TryFindPeripheralStatElement(request.ProfileGroupsRoot, peripheralName, out var peripheralProfile))
                    {
                        continue;
                    }

                    return new LegacyPeripheralSelectionResult
                    {
                        PeripheralName = peripheralName,
                        PeripheralProfile = peripheralProfile
                    };
                }
            }
        }

        return null;
    }

    internal static bool TryFindPeripheralStatElement(
        JsonElement profileGroupsRoot,
        string peripheralName,
        out JsonElement profile)
    {
        profile = default;
        var expected = NormalizeComparisonToken(peripheralName);
        if (string.IsNullOrWhiteSpace(expected) || profileGroupsRoot.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var group in profileGroupsRoot.EnumerateArray())
        {
            var groupIsc = group.TryGetProperty("isc", out var groupIscElement) && groupIscElement.ValueKind == JsonValueKind.String
                ? groupIscElement.GetString() ?? string.Empty
                : string.Empty;
            var normalizedGroupIsc = NormalizeComparisonToken(groupIsc);

            if (group.TryGetProperty("profiles", out var profilesElement) && profilesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var candidate in profilesElement.EnumerateArray())
                {
                    var profileName = candidate.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String
                        ? nameElement.GetString() ?? string.Empty
                        : string.Empty;
                    var normalizedProfileName = NormalizeComparisonToken(profileName);
                    if (normalizedProfileName == expected || normalizedGroupIsc == expected)
                    {
                        profile = candidate;
                        return true;
                    }
                }
            }

            if (group.TryGetProperty("options", out var optionsElement) && optionsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var candidateOption in optionsElement.EnumerateArray())
                {
                    var optionName = candidateOption.TryGetProperty("name", out var optionNameElement) && optionNameElement.ValueKind == JsonValueKind.String
                        ? optionNameElement.GetString() ?? string.Empty
                        : string.Empty;
                    var normalizedOptionName = NormalizeComparisonToken(optionName);
                    if (normalizedOptionName == expected)
                    {
                        if (group.TryGetProperty("profiles", out var optionMatchedProfiles) &&
                            optionMatchedProfiles.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var optionMatchedProfile in optionMatchedProfiles.EnumerateArray())
                            {
                                if (HasStatFields(optionMatchedProfile))
                                {
                                    profile = optionMatchedProfile;
                                    return true;
                                }
                            }
                        }

                        if (HasStatFields(candidateOption))
                        {
                            profile = candidateOption;
                            return true;
                        }
                    }
                }
            }

            if (normalizedGroupIsc == expected &&
                group.TryGetProperty("profiles", out var groupProfilesElement) &&
                groupProfilesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var fallbackProfile in groupProfilesElement.EnumerateArray())
                {
                    profile = fallbackProfile;
                    return true;
                }
            }
        }

        return false;
    }

    internal static string NormalizeComparisonToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return Regex.Replace(value, @"[^a-z0-9]", string.Empty, RegexOptions.IgnoreCase).ToLowerInvariant();
    }

    internal static bool HasStatFields(JsonElement element)
    {
        return element.TryGetProperty("cc", out _) ||
               element.TryGetProperty("bs", out _) ||
               element.TryGetProperty("ph", out _) ||
               element.TryGetProperty("wip", out _) ||
               element.TryGetProperty("arm", out _) ||
               element.TryGetProperty("bts", out _) ||
               element.TryGetProperty("w", out _) ||
               element.TryGetProperty("str", out _);
    }

    internal static (int? FirstCm, int? SecondCm, string DisplayValue) ReadMoveValue(JsonElement element)
    {
        if (!ArmyDataTryGetPropertyFlexible(element, "move", out var moveElement) &&
            !ArmyDataTryGetPropertyFlexible(element, "mov", out moveElement))
        {
            return (null, null, "-");
        }

        int? firstCm = null;
        int? secondCm = null;

        if (moveElement.ValueKind == JsonValueKind.String)
        {
            var parts = (moveElement.GetString() ?? string.Empty)
                .Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.TryParse(x, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? (int?)parsed : null)
                .OfType<int>()
                .ToList();

            if (parts.Count >= 2)
            {
                firstCm = parts[0];
                secondCm = parts[1];
            }
        }
        else if (moveElement.ValueKind == JsonValueKind.Array)
        {
            var values = moveElement.EnumerateArray()
                .Select(TryParseInt)
                .OfType<int>()
                .ToList();

            if (values.Count >= 2)
            {
                firstCm = values[0];
                secondCm = values[1];
            }
        }

        return (firstCm, secondCm, firstCm is null || secondCm is null || firstCm <= 0 || secondCm <= 0 ? "-" : $"{firstCm.Value}-{secondCm.Value}" /* FormatMoveValue in cm mode */);
    }

    internal static bool ArmyDataTryGetPropertyFlexible(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.TryGetProperty(propertyName, out value))
        {
            return true;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            value = default;
            return false;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    internal static int? TryParseInt(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String &&
            int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var fromText))
        {
            return fromText;
        }

        return null;
    }

    internal static (string Header, string Value) ReadVitality(JsonElement option)
    {
        var str = LegacySharedUtilities.ReadIntAsString(option, "str");
        if (str != "-")
        {
            return ("STR", str);
        }

        var w = LegacySharedUtilities.ReadIntAsString(option, "w");
        if (w != "-")
        {
            return ("VITA", w);
        }

        return ("VITA", LegacySharedUtilities.ReadIntAsString(option, "vita"));
    }

    internal static string ReadAvaAsString(JsonElement option)
    {
        if (!LegacySharedUtilities.TryGetPropertyFlexible(option, "ava", out var avaElement))
        {
            return "-";
        }

        int value;
        if (avaElement.ValueKind == JsonValueKind.Number && avaElement.TryGetInt32(out value))
        {
            return value switch
            {
                < 0 => "-",
                255 => "T",
                _ => value.ToString(CultureInfo.InvariantCulture)
            };
        }

        if (avaElement.ValueKind == JsonValueKind.String && int.TryParse(avaElement.GetString(), out value))
        {
            return value switch
            {
                < 0 => "-",
                255 => "T",
                _ => value.ToString(CultureInfo.InvariantCulture)
            };
        }

        return "-";
    }
}
