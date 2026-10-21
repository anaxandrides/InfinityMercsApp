// FROZEN LEGACY ORACLE — verbatim copy of production JSON rules from main@224e1e2.
// Do not edit or "fix" this file. Characterization tests run it against real Infinity Army
// payloads so replacement code can be proven to produce identical results.
// Source: the pure (non-MAUI) private helpers of ViewModels/ViewerViewModel.{OptionEntries,OptionDisplay,Fireteams}.cs,
// which duplicate the rules in CompanyProfileOptionService/CompanyUnitFilterService.
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace InfinityMercsApp.UnitTests.Legacy;

internal static class LegacyViewerOptionRules
{
    internal readonly record struct ExtraDefinition(string Name, string Type, string? Url);

    internal static List<string> GetOrderedNames(
        JsonElement container,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup)
    {
        return GetOrderedIdNames(container, propertyName, lookup)
            .Select(x => x.Name)
            .ToList();
    }

    internal static List<(int Id, string Name)> GetOrderedIdDisplayNames(
        JsonElement container,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup,
        IReadOnlyDictionary<int, ExtraDefinition> extrasLookup,
        bool showUnitsInInches)
    {
        if (!container.TryGetProperty(propertyName, out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var entries = new List<(int Order, int Id, string Name)>();
        foreach (var entry in arr.EnumerateArray())
        {
            if (!TryParseId(entry, out var id))
            {
                continue;
            }

            var order = int.MaxValue;
            if (entry.ValueKind == JsonValueKind.Object &&
                entry.TryGetProperty("order", out var orderElement) &&
                orderElement.ValueKind == JsonValueKind.Number &&
                orderElement.TryGetInt32(out var parsedOrder))
            {
                order = parsedOrder;
            }

            var baseName = lookup.TryGetValue(id, out var resolvedName) ? resolvedName : id.ToString();
            var displayName = BuildEntryDisplayName(baseName, entry, extrasLookup, showUnitsInInches);
            entries.Add((order, id, displayName));
        }

        return entries
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => (x.Id, x.Name))
            .ToList();
    }

    internal static List<(int Id, string Name)> GetOrderedIdDisplayNamesFromEntries(
        IEnumerable<JsonElement> entriesSource,
        IReadOnlyDictionary<int, string> lookup,
        IReadOnlyDictionary<int, ExtraDefinition> extrasLookup,
        bool showUnitsInInches)
    {
        var entries = new List<(int Order, int Id, string Name)>();
        foreach (var entry in entriesSource)
        {
            if (!TryParseId(entry, out var id))
            {
                continue;
            }

            var order = int.MaxValue;
            if (entry.ValueKind == JsonValueKind.Object &&
                entry.TryGetProperty("order", out var orderElement) &&
                orderElement.ValueKind == JsonValueKind.Number &&
                orderElement.TryGetInt32(out var parsedOrder))
            {
                order = parsedOrder;
            }

            var baseName = lookup.TryGetValue(id, out var resolvedName) ? resolvedName : id.ToString();
            var displayName = BuildEntryDisplayName(baseName, entry, extrasLookup, showUnitsInInches);
            entries.Add((order, id, displayName));
        }

        return entries
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => (x.Id, x.Name))
            .ToList();
    }

    internal static List<(int Id, string Name)> GetCountedIdDisplayNamesFromEntries(
        IEnumerable<JsonElement> entriesSource,
        IReadOnlyDictionary<int, string> lookup,
        IReadOnlyDictionary<int, ExtraDefinition> extrasLookup,
        bool showUnitsInInches)
    {
        var counts = new Dictionary<string, (int Id, int Count)>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entriesSource)
        {
            if (!TryParseId(entry, out var id))
            {
                continue;
            }

            var baseName = lookup.TryGetValue(id, out var resolvedName) ? resolvedName : id.ToString();
            var displayName = BuildEntryDisplayName(baseName, entry, extrasLookup, showUnitsInInches);
            if (string.IsNullOrWhiteSpace(displayName))
            {
                continue;
            }

            var quantity = ReadEntryQuantity(entry);
            if (counts.TryGetValue(displayName, out var existing))
            {
                counts[displayName] = (existing.Id, existing.Count + quantity);
            }
            else
            {
                counts[displayName] = (id, quantity);
            }
        }

        return counts
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => (x.Value.Id, $"{x.Key} ({x.Value.Count})"))
            .ToList();
    }

    internal static int ReadEntryQuantity(JsonElement entry)
    {
        if (entry.ValueKind != JsonValueKind.Object)
        {
            return 1;
        }

        if (!entry.TryGetProperty("q", out var quantityElement))
        {
            return 1;
        }

        if (quantityElement.ValueKind == JsonValueKind.Number && quantityElement.TryGetInt32(out var quantityNumber))
        {
            return Math.Max(1, quantityNumber);
        }

        if (quantityElement.ValueKind == JsonValueKind.String &&
            int.TryParse(
                quantityElement.GetString(),
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out var quantityText))
        {
            return Math.Max(1, quantityText);
        }

        return 1;
    }

    internal static IEnumerable<JsonElement> GetOptionEntriesWithIncludes(
        JsonElement profileGroupsRoot,
        JsonElement option,
        string propertyName)
    {
        var collected = new List<JsonElement>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectOptionEntriesWithIncludes(profileGroupsRoot, option, propertyName, collected, visited, null);
        return collected;
    }

    internal static void CollectOptionEntriesWithIncludes(
        JsonElement profileGroupsRoot,
        JsonElement option,
        string propertyName,
        List<JsonElement> target,
        HashSet<string> visited,
        (int GroupId, int OptionId)? includeRef)
    {
        var key = includeRef.HasValue
            ? $"{includeRef.Value.GroupId}:{includeRef.Value.OptionId}"
            : option.GetRawText().GetHashCode().ToString();
        if (!visited.Add(key))
        {
            return;
        }

        if (option.TryGetProperty(propertyName, out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in arr.EnumerateArray())
            {
                target.Add(entry);
            }
        }

        if (!option.TryGetProperty("includes", out var includesElement) || includesElement.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var include in includesElement.EnumerateArray())
        {
            if (include.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (!TryParseIncludeReference(include, out var includeGroupId, out var includeOptionId))
            {
                continue;
            }

            var includedOption = FindIncludedOption(profileGroupsRoot, includeGroupId, includeOptionId);
            if (includedOption.HasValue)
            {
                CollectOptionEntriesWithIncludes(
                    profileGroupsRoot,
                    includedOption.Value,
                    propertyName,
                    target,
                    visited,
                    (includeGroupId, includeOptionId));
            }
        }
    }

    internal static bool TryParseIncludeReference(JsonElement include, out int groupId, out int optionId)
    {
        groupId = 0;
        optionId = 0;

        if (!include.TryGetProperty("group", out var groupElement) || !TryParseId(groupElement, out groupId))
        {
            return false;
        }

        if (!include.TryGetProperty("option", out var optionElement) || !TryParseId(optionElement, out optionId))
        {
            return false;
        }

        return true;
    }

    internal static JsonElement? FindIncludedOption(JsonElement profileGroupsRoot, int groupId, int optionId)
    {
        if (profileGroupsRoot.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var group in profileGroupsRoot.EnumerateArray())
        {
            if (!group.TryGetProperty("id", out var groupIdElement) ||
                !TryParseId(groupIdElement, out var parsedGroupId) ||
                parsedGroupId != groupId)
            {
                continue;
            }

            if (!group.TryGetProperty("options", out var optionsElement) || optionsElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var option in optionsElement.EnumerateArray())
            {
                if (!option.TryGetProperty("id", out var optionIdElement) ||
                    !TryParseId(optionIdElement, out var parsedOptionId) ||
                    parsedOptionId != optionId)
                {
                    continue;
                }

                return option;
            }
        }

        return null;
    }

    internal static string BuildEntryDisplayName(
        string baseName,
        JsonElement entry,
        IReadOnlyDictionary<int, ExtraDefinition> extrasLookup,
        bool showUnitsInInches)
    {
        if (entry.ValueKind != JsonValueKind.Object)
        {
            return baseName;
        }

        if (!entry.TryGetProperty("extra", out var extraElement) || extraElement.ValueKind != JsonValueKind.Array)
        {
            return baseName;
        }

        var extras = new List<string>();
        foreach (var extraEntry in extraElement.EnumerateArray())
        {
            if (!TryParseId(extraEntry, out var extraId))
            {
                continue;
            }

            if (extrasLookup.TryGetValue(extraId, out var extraDefinition) &&
                !string.IsNullOrWhiteSpace(extraDefinition.Name))
            {
                extras.Add(FormatExtraDisplay(extraDefinition, showUnitsInInches));
            }
            else
            {
                extras.Add(extraId.ToString());
            }
        }

        var distinctExtras = extras
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return distinctExtras.Count == 0
            ? baseName
            : $"{baseName} ({string.Join(", ", distinctExtras)})";
    }

    internal static string FormatExtraDisplay(ExtraDefinition extraDefinition, bool showUnitsInInches)
    {
        if (!string.Equals(extraDefinition.Type, "DISTANCE", StringComparison.OrdinalIgnoreCase))
        {
            return extraDefinition.Name;
        }

        var valueText = showUnitsInInches
            ? ConvertDistanceTextToInches(extraDefinition.Name)
            : extraDefinition.Name;

        return AppendDistanceUnitSuffix(valueText, showUnitsInInches);
    }

    internal static string AppendDistanceUnitSuffix(string distanceText, bool showUnitsInInches)
    {
        if (string.IsNullOrWhiteSpace(distanceText))
        {
            return distanceText;
        }

        if (Regex.IsMatch(distanceText, "(\"|cm)\\s*$", RegexOptions.IgnoreCase))
        {
            return distanceText;
        }

        var match = Regex.Match(distanceText, @"([+-]?\d+(?:\.\d+)?)");
        if (!match.Success)
        {
            return distanceText;
        }

        var suffix = showUnitsInInches ? "\"" : "cm";
        return string.Concat(
            distanceText.AsSpan(0, match.Index + match.Length),
            suffix,
            distanceText.AsSpan(match.Index + match.Length));
    }

    internal static string ConvertDistanceTextToInches(string distanceText)
    {
        if (string.IsNullOrWhiteSpace(distanceText))
        {
            return distanceText;
        }

        var match = Regex.Match(distanceText, @"([+-]?)(\d+(?:\.\d+)?)");
        if (!match.Success)
        {
            return distanceText;
        }

        var signToken = match.Groups[1].Value;
        var valueToken = match.Groups[2].Value;
        if (!decimal.TryParse(valueToken, NumberStyles.Float, CultureInfo.InvariantCulture, out var valueCm))
        {
            return distanceText;
        }

        if (signToken == "-")
        {
            valueCm = -valueCm;
        }

        var valueInches = (int)Math.Round((double)(valueCm / 2.5m), MidpointRounding.AwayFromZero);
        var replacement = valueInches < 0
            ? valueInches.ToString(CultureInfo.InvariantCulture)
            : signToken == "+"
                ? $"+{valueInches}"
                : valueInches.ToString(CultureInfo.InvariantCulture);

        return string.Concat(
            distanceText.AsSpan(0, match.Index),
            replacement,
            distanceText.AsSpan(match.Index + match.Length));
    }

    internal static Dictionary<int, ExtraDefinition> BuildExtrasLookup(string? filtersJson)
    {
        var map = new Dictionary<int, ExtraDefinition>();
        if (string.IsNullOrWhiteSpace(filtersJson))
        {
            return map;
        }

        try
        {
            using var doc = JsonDocument.Parse(filtersJson);
            if (!doc.RootElement.TryGetProperty("extras", out var section) || section.ValueKind != JsonValueKind.Array)
            {
                return map;
            }

            foreach (var entry in section.EnumerateArray())
            {
                if (!entry.TryGetProperty("id", out var idElement) || !TryParseId(idElement, out var id))
                {
                    continue;
                }

                if (!entry.TryGetProperty("name", out var nameElement) || nameElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var name = nameElement.GetString();
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var type = entry.TryGetProperty("type", out var typeElement) && typeElement.ValueKind == JsonValueKind.String
                    ? typeElement.GetString() ?? string.Empty
                    : string.Empty;

                map[id] = new ExtraDefinition(name, type, TryReadLink(entry));
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"BuildExtrasLookup failed: {ex.Message}");
        }

        return map;
    }

    internal static void AddFilterOptionsFromProfilesAndOptions(
        string profileGroupsJson,
        IReadOnlyDictionary<int, string> charsLookup,
        IReadOnlyDictionary<int, string> skillsLookup,
        IReadOnlyDictionary<int, string> equipLookup,
        IReadOnlyDictionary<int, string> weaponsLookup,
        IReadOnlyDictionary<int, string> ammoLookup,
        HashSet<string> characteristics,
        HashSet<string> skills,
        HashSet<string> equipment,
        HashSet<string> weapons,
        HashSet<string> ammo,
        ref int maxPoints)
    {
        try
        {
            using var doc = JsonDocument.Parse(profileGroupsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var group in doc.RootElement.EnumerateArray())
            {
                if (group.TryGetProperty("options", out var optionsElement) && optionsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var option in optionsElement.EnumerateArray())
                    {
                        var optionCost = ParseCostValue(ReadAdjustedOptionCost(doc.RootElement, group, option));
                        maxPoints = Math.Max(maxPoints, optionCost);

                        foreach (var character in GetOrderedIdNames(option, "chars", charsLookup))
                        {
                            if (!string.IsNullOrWhiteSpace(character.Name))
                            {
                                characteristics.Add(character.Name.Trim());
                            }
                        }

                        foreach (var skill in GetOrderedIdNames(option, "skills", skillsLookup))
                        {
                            if (!string.IsNullOrWhiteSpace(skill.Name))
                            {
                                skills.Add(skill.Name.Trim());
                            }
                        }

                        foreach (var equip in GetOrderedIdNames(option, "equip", equipLookup))
                        {
                            if (!string.IsNullOrWhiteSpace(equip.Name))
                            {
                                equipment.Add(equip.Name.Trim());
                            }
                        }

                        foreach (var weapon in GetOrderedIdNames(option, "weapons", weaponsLookup))
                        {
                            if (!string.IsNullOrWhiteSpace(weapon.Name))
                            {
                                weapons.Add(weapon.Name.Trim());
                            }
                        }

                        foreach (var ammoEntry in GetOrderedIdNames(option, "ammunition", ammoLookup))
                        {
                            if (!string.IsNullOrWhiteSpace(ammoEntry.Name))
                            {
                                ammo.Add(ammoEntry.Name.Trim());
                            }
                        }

                        foreach (var ammoEntry in GetOrderedIdNames(option, "ammo", ammoLookup))
                        {
                            if (!string.IsNullOrWhiteSpace(ammoEntry.Name))
                            {
                                ammo.Add(ammoEntry.Name.Trim());
                            }
                        }
                    }
                }

                if (!group.TryGetProperty("profiles", out var profilesElement) || profilesElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var profile in profilesElement.EnumerateArray())
                {
                    foreach (var character in GetOrderedIdNames(profile, "chars", charsLookup))
                    {
                        if (!string.IsNullOrWhiteSpace(character.Name))
                        {
                            characteristics.Add(character.Name.Trim());
                        }
                    }

                    foreach (var skill in GetOrderedIdNames(profile, "skills", skillsLookup))
                    {
                        if (!string.IsNullOrWhiteSpace(skill.Name))
                        {
                            skills.Add(skill.Name.Trim());
                        }
                    }

                    foreach (var equip in GetOrderedIdNames(profile, "equip", equipLookup))
                    {
                        if (!string.IsNullOrWhiteSpace(equip.Name))
                        {
                            equipment.Add(equip.Name.Trim());
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AddFilterOptionsFromProfilesAndOptions failed: {ex.Message}");
        }
    }

    internal static int ParseCostValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        var match = Regex.Match(value, @"-?\d+");
        if (!match.Success)
        {
            return 0;
        }

        return int.TryParse(match.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
    }

    internal static string? TryReadLink(JsonElement entry)
    {
        foreach (var key in new[] { "url", "href", "link", "wiki", "web" })
        {
            if (!entry.TryGetProperty(key, out var valueElement) || valueElement.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var value = valueElement.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return value;
            }
        }

        return null;
    }

    internal static Dictionary<int, string> BuildIdLinkLookup(string? filtersJson, string sectionName)
    {
        var map = new Dictionary<int, string>();
        if (string.IsNullOrWhiteSpace(filtersJson))
        {
            return map;
        }

        try
        {
            using var doc = JsonDocument.Parse(filtersJson);
            if (!doc.RootElement.TryGetProperty(sectionName, out var section) || section.ValueKind != JsonValueKind.Array)
            {
                return map;
            }

            foreach (var entry in section.EnumerateArray())
            {
                if (!entry.TryGetProperty("id", out var idElement) || !TryParseId(idElement, out var id))
                {
                    continue;
                }

                var link = TryReadLink(entry);
                if (!string.IsNullOrWhiteSpace(link))
                {
                    map[id] = link;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"BuildIdLinkLookup failed for section '{sectionName}': {ex.Message}");
        }

        return map;
    }

    internal static List<(int Id, string Name)> GetOrderedIdNames(
        JsonElement container,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup)
    {
        if (!container.TryGetProperty(propertyName, out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var entries = new List<(int Order, int Id, string Name)>();
        foreach (var entry in arr.EnumerateArray())
        {
            if (!TryParseId(entry, out var id))
            {
                continue;
            }

            var order = int.MaxValue;
            if (entry.ValueKind == JsonValueKind.Object &&
                entry.TryGetProperty("order", out var orderElement) &&
                orderElement.ValueKind == JsonValueKind.Number &&
                orderElement.TryGetInt32(out var parsedOrder))
            {
                order = parsedOrder;
            }

            var name = lookup.TryGetValue(id, out var resolvedName) ? resolvedName : id.ToString();
            entries.Add((order, id, name));
        }

        return entries
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => (x.Id, x.Name))
            .ToList();
    }

    internal static string BuildOptionConfigurationSummary(
        JsonElement option,
        IReadOnlyDictionary<int, string> weaponsLookup,
        IReadOnlyDictionary<int, string> equipLookup,
        IReadOnlyDictionary<int, string> skillsLookup)
    {
        var weapons = GetOrderedNames(option, "weapons", weaponsLookup);
        var equip = GetOrderedNames(option, "equip", equipLookup);
        var skills = GetOrderedNames(option, "skills", skillsLookup);

        var primary = weapons.Count > 0 ? string.Join(", ", weapons) : string.Empty;
        var extras = equip.Concat(skills).ToList();

        if (string.IsNullOrWhiteSpace(primary) && extras.Count == 0)
        {
            return "-";
        }

        if (string.IsNullOrWhiteSpace(primary))
        {
            return string.Join(", ", extras);
        }

        if (extras.Count == 0)
        {
            return primary;
        }

        return $"{primary} + {string.Join(", ", extras)}";
    }

    internal static bool HasLieutenantOrder(JsonElement option)
    {
        if (!option.TryGetProperty("orders", out var ordersElement) || ordersElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var order in ordersElement.EnumerateArray())
        {
            if (!order.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var type = typeElement.GetString();
            if (string.Equals(type, "LIEUTENANT", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsLieutenantOption(JsonElement option, IReadOnlyDictionary<int, string> skillsLookup)
    {
        if (HasLieutenantOrder(option))
        {
            return true;
        }

        if (option.TryGetProperty("name", out var nameElement) &&
            nameElement.ValueKind == JsonValueKind.String &&
            nameElement.GetString()?.Contains("lieutenant", StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        foreach (var skill in GetOrderedIdNames(option, "skills", skillsLookup))
        {
            if (skill.Name.Contains("lieutenant", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool UnitHasVisibleOptionWithFilter(
        string? profileGroupsJson,
        IReadOnlyDictionary<int, string> skillsLookup,
        IReadOnlyDictionary<int, string> charsLookup,
        IReadOnlyDictionary<int, string> equipLookup,
        IReadOnlyDictionary<int, string> weaponsLookup,
        IReadOnlyDictionary<int, string> ammoLookup,
        UnitFilterCriteria criteria,
        bool requireLieutenant,
        bool requireZeroSwc,
        int? maxCost = null)
    {
        if (string.IsNullOrWhiteSpace(profileGroupsJson))
        {
            return false;
        }

        var filterQuery = criteria.ToQuery();

        try
        {
            using var doc = JsonDocument.Parse(profileGroupsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var group in doc.RootElement.EnumerateArray())
            {
                if (!group.TryGetProperty("options", out var optionsElement) || optionsElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var option in optionsElement.EnumerateArray())
                {
                    if (requireLieutenant && !IsLieutenantOption(option, skillsLookup))
                    {
                        continue;
                    }

                    if (requireZeroSwc && IsPositiveSwc(ReadOptionSwc(option)))
                    {
                        continue;
                    }

                    var optionCost = ParseCostValue(ReadAdjustedOptionCost(doc.RootElement, group, option));
                    if (maxCost.HasValue && optionCost > maxCost.Value)
                    {
                        continue;
                    }

                    if (filterQuery.MinPoints.HasValue && optionCost < filterQuery.MinPoints.Value)
                    {
                        continue;
                    }

                    if (filterQuery.MaxPoints.HasValue && optionCost > filterQuery.MaxPoints.Value)
                    {
                        continue;
                    }

                    if (!OptionMatchesUnitFilter(
                            doc.RootElement,
                            group,
                            option,
                            charsLookup,
                            skillsLookup,
                            equipLookup,
                            weaponsLookup,
                            ammoLookup,
                            filterQuery))
                    {
                        continue;
                    }

                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"UnitHasVisibleOptionWithFilter failed: {ex.Message}");
        }

        return false;
    }

    internal static bool OptionMatchesUnitFilter(
        JsonElement profileGroupsRoot,
        JsonElement profileGroup,
        JsonElement option,
        IReadOnlyDictionary<int, string> charsLookup,
        IReadOnlyDictionary<int, string> skillsLookup,
        IReadOnlyDictionary<int, string> equipLookup,
        IReadOnlyDictionary<int, string> weaponsLookup,
        IReadOnlyDictionary<int, string> ammoLookup,
        UnitFilterQuery filterQuery)
    {
        foreach (var term in filterQuery.Terms)
        {
            if (term.Field == UnitFilterField.Classification || term.Values.Count == 0)
            {
                continue;
            }

            var matches = term.Field switch
            {
                UnitFilterField.Characteristics => TermMatchesOptionOrGroup(
                    term,
                    profileGroupsRoot,
                    profileGroup,
                    option,
                    "chars",
                    charsLookup),
                UnitFilterField.Skills => TermMatchesOptionOrGroup(
                    term,
                    profileGroupsRoot,
                    profileGroup,
                    option,
                    "skills",
                    skillsLookup),
                UnitFilterField.Equipment => TermMatchesOptionOrGroup(
                    term,
                    profileGroupsRoot,
                    profileGroup,
                    option,
                    "equip",
                    equipLookup),
                UnitFilterField.Weapons => TermMatchesOptionOrGroup(
                    term,
                    profileGroupsRoot,
                    profileGroup,
                    option,
                    "weapons",
                    weaponsLookup),
                UnitFilterField.Ammo => TermMatchesOptionOnly(
                    term,
                    profileGroupsRoot,
                    option,
                    ammoLookup,
                    ["ammunition", "ammo"]),
                _ => true
            };

            if (!matches)
            {
                return false;
            }
        }

        return true;
    }

    internal static bool TermMatchesOptionOrGroup(
        UnitFilterTerm term,
        JsonElement profileGroupsRoot,
        JsonElement profileGroup,
        JsonElement option,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup)
    {
        return term.MatchMode == UnitFilterMatchMode.All
            ? term.Values.All(value => OptionOrGroupContainsLookupName(profileGroupsRoot, profileGroup, option, propertyName, lookup, value))
            : term.Values.Any(value => OptionOrGroupContainsLookupName(profileGroupsRoot, profileGroup, option, propertyName, lookup, value));
    }

    internal static bool TermMatchesOptionOnly(
        UnitFilterTerm term,
        JsonElement profileGroupsRoot,
        JsonElement option,
        IReadOnlyDictionary<int, string> lookup,
        IEnumerable<string> propertyNames)
    {
        return term.MatchMode == UnitFilterMatchMode.All
            ? term.Values.All(value => OptionContainsAnyLookupName(profileGroupsRoot, option, propertyNames, lookup, value))
            : term.Values.Any(value => OptionContainsAnyLookupName(profileGroupsRoot, option, propertyNames, lookup, value));
    }

    internal static bool OptionOrGroupContainsLookupName(
        JsonElement profileGroupsRoot,
        JsonElement profileGroup,
        JsonElement option,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup,
        string expectedValue)
    {
        return OptionContainsLookupName(profileGroupsRoot, option, propertyName, lookup, expectedValue) ||
               GroupProfilesContainLookupName(profileGroup, propertyName, lookup, expectedValue);
    }

    internal static bool OptionContainsAnyLookupName(
        JsonElement profileGroupsRoot,
        JsonElement option,
        IEnumerable<string> propertyNames,
        IReadOnlyDictionary<int, string> lookup,
        string expectedValue)
    {
        foreach (var propertyName in propertyNames)
        {
            if (OptionContainsLookupName(profileGroupsRoot, option, propertyName, lookup, expectedValue))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool OptionContainsLookupName(
        JsonElement profileGroupsRoot,
        JsonElement option,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup,
        string expectedValue)
    {
        if (lookup.Count == 0 || string.IsNullOrWhiteSpace(expectedValue))
        {
            return false;
        }

        foreach (var entry in GetOptionEntriesWithIncludes(profileGroupsRoot, option, propertyName))
        {
            if (!TryParseId(entry, out var id) || !lookup.TryGetValue(id, out var name))
            {
                continue;
            }

            if (string.Equals(name, expectedValue, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool GroupProfilesContainLookupName(
        JsonElement profileGroup,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup,
        string expectedValue)
    {
        if (lookup.Count == 0 || string.IsNullOrWhiteSpace(expectedValue))
        {
            return false;
        }

        if (!profileGroup.TryGetProperty("profiles", out var profilesElement) || profilesElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var profile in profilesElement.EnumerateArray())
        {
            if (!profile.TryGetProperty(propertyName, out var valuesElement) || valuesElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var entry in valuesElement.EnumerateArray())
            {
                if (!TryParseId(entry, out var id) || !lookup.TryGetValue(id, out var name))
                {
                    continue;
                }

                if (string.Equals(name, expectedValue, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    internal static string BuildOptionDisplayName(
        JsonElement option,
        string baseName,
        IReadOnlyDictionary<int, string> equipLookup,
        IReadOnlyDictionary<int, string> skillsLookup)
    {
        var details = new List<string>();
        var normalizedBase = baseName.ToLowerInvariant();

        foreach (var skillName in GetOrderedNames(option, "skills", skillsLookup))
        {
            if (IsNameDetailTag(skillName) && !normalizedBase.Contains(skillName.ToLowerInvariant()))
            {
                details.Add(skillName);
            }
        }

        foreach (var equipName in GetOrderedNames(option, "equip", equipLookup))
        {
            if (IsNameDetailTag(equipName) && !normalizedBase.Contains(equipName.ToLowerInvariant()))
            {
                details.Add(equipName);
            }
        }

        if (option.TryGetProperty("orders", out var ordersElement) && ordersElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var order in ordersElement.EnumerateArray())
            {
                if (!order.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var type = typeElement.GetString();
                if (string.Equals(type, "LIEUTENANT", StringComparison.OrdinalIgnoreCase) &&
                    !normalizedBase.Contains("lieutenant"))
                {
                    details.Add("Lieutenant");
                }
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

    internal static bool IsNameDetailTag(string value)
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

    internal static string ReadOptionSwc(JsonElement option)
    {
        if (!option.TryGetProperty("swc", out var swcElement))
        {
            return "-";
        }

        if (swcElement.ValueKind == JsonValueKind.String)
        {
            var swc = swcElement.GetString();
            return string.IsNullOrWhiteSpace(swc) ? "-" : swc;
        }

        if (swcElement.ValueKind == JsonValueKind.Number)
        {
            return swcElement.ToString();
        }

        return "-";
    }

    internal static string ReadOptionCost(JsonElement option)
    {
        if (!option.TryGetProperty("points", out var pointsElement))
        {
            return "-";
        }

        if (pointsElement.ValueKind == JsonValueKind.Number)
        {
            if (pointsElement.TryGetInt32(out var intCost))
            {
                return intCost.ToString();
            }

            return pointsElement.ToString();
        }

        if (pointsElement.ValueKind == JsonValueKind.String)
        {
            var points = pointsElement.GetString();
            return string.IsNullOrWhiteSpace(points) ? "-" : points;
        }

        return "-";
    }

    internal static string ReadAdjustedOptionCost(JsonElement profileGroupsRoot, JsonElement group, JsonElement option)
    {
        var baseCostText = ReadOptionCost(option);
        if (!int.TryParse(
                baseCostText,
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out var baseCost))
        {
            return baseCostText;
        }

        var totalPeripheralCount = GetDisplayPeripheralEntriesForOption(profileGroupsRoot, group, option)
            .Sum(ReadEntryQuantity);
        if (totalPeripheralCount <= 1)
        {
            return baseCostText;
        }

        var minis = ReadOptionMinis(option);
        if (minis <= 1 || minis <= totalPeripheralCount)
        {
            return baseCostText;
        }

        if (baseCost <= 0 || baseCost % minis != 0)
        {
            return baseCostText;
        }

        var removedPeripheralCount = totalPeripheralCount - 1;
        var perModelCost = baseCost / minis;
        var deduction = removedPeripheralCount * perModelCost;
        var adjustedCost = Math.Max(0, baseCost - deduction);
        return adjustedCost.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static IEnumerable<JsonElement> GetControllerPeripheralEntries(JsonElement group)
    {
        if (!group.TryGetProperty("profiles", out var profilesElement) || profilesElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var collected = new List<JsonElement>();
        foreach (var profile in profilesElement.EnumerateArray())
        {
            if (profile.TryGetProperty("peripheral", out var peripheralElement) &&
                peripheralElement.ValueKind == JsonValueKind.Array &&
                peripheralElement.GetArrayLength() > 0)
            {
                collected.AddRange(peripheralElement.EnumerateArray().ToList());
            }
        }

        return collected;
    }

    internal static HashSet<int> GetControllerPeripheralIds(JsonElement group)
    {
        var ids = new HashSet<int>();
        foreach (var entry in GetControllerPeripheralEntries(group))
        {
            if (TryParseId(entry, out var id))
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    internal static IEnumerable<JsonElement> GetFilteredOptionPeripheralEntries(
        JsonElement profileGroupsRoot,
        JsonElement group,
        JsonElement option)
    {
        var allowedIds = GetControllerPeripheralIds(group);
        var optionEntries = GetOptionEntriesWithIncludes(profileGroupsRoot, option, "peripheral").ToList();

        if (allowedIds.Count == 0)
        {
            return optionEntries;
        }

        return optionEntries
            .Where(entry => TryParseId(entry, out var id) && allowedIds.Contains(id))
            .ToList();
    }

    internal static IEnumerable<JsonElement> GetDisplayPeripheralEntriesForOption(
        JsonElement profileGroupsRoot,
        JsonElement group,
        JsonElement option)
    {
        var optionEntries = GetFilteredOptionPeripheralEntries(profileGroupsRoot, group, option).ToList();
        if (optionEntries.Count > 0)
        {
            return optionEntries;
        }

        return GetControllerPeripheralEntries(group).ToList();
    }

    internal static bool IsControllerGroup(JsonElement profileGroupsRoot, JsonElement group)
    {
        if (GetControllerPeripheralIds(group).Count > 0)
        {
            return true;
        }

        if (!group.TryGetProperty("options", out var optionsElement) || optionsElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var option in optionsElement.EnumerateArray())
        {
            if (GetOptionEntriesWithIncludes(profileGroupsRoot, option, "peripheral").Any())
            {
                return true;
            }
        }

        return false;
    }

    internal static int ReadOptionMinis(JsonElement option)
    {
        if (!option.TryGetProperty("minis", out var minisElement))
        {
            return 0;
        }

        if (minisElement.ValueKind == JsonValueKind.Number && minisElement.TryGetInt32(out var minisNumber))
        {
            return Math.Max(0, minisNumber);
        }

        if (minisElement.ValueKind == JsonValueKind.String &&
            int.TryParse(
                minisElement.GetString(),
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out var minisText))
        {
            return Math.Max(0, minisText);
        }

        return 0;
    }

    // Changed after the freeze, as a bug fix: a "+1" gives SWC rather than costing it, so it no longer hides a profile.
    internal static bool IsPositiveSwc(string swc)
    {
        if (string.IsNullOrWhiteSpace(swc) || swc == "-" || swc.TrimStart().StartsWith('+'))
        {
            return false;
        }

        return decimal.TryParse(
                   swc,
                   System.Globalization.NumberStyles.Number,
                   System.Globalization.CultureInfo.InvariantCulture,
                   out var value)
               && value > 0m;
    }

    internal static bool TryParseId(JsonElement element, out int id)
    {
        id = 0;
        if (element.ValueKind == JsonValueKind.Number)
        {
            return element.TryGetInt32(out id);
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            return int.TryParse(element.GetString(), out id);
        }

        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("id", out var idElement))
        {
            return TryParseId(idElement, out id);
        }

        return false;
    }
}
