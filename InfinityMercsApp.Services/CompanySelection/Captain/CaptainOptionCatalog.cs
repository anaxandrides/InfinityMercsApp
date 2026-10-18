using System.Globalization;
using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Lieutenants;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.Common;

namespace InfinityMercsApp.Services.CompanySelection.Captain;

/// <summary>The weapons, skills and equipment a faction's Spec-Ops offer a captain, labelled with their XP cost.</summary>
public sealed class CaptainOptionCatalog(IArmyDataService armyData, ISpecOpsProvider specOps)
{
    private const int TagCompanyFactionId = 2003;

    /// <summary>The options for <paramref name="factionId"/>; none when the faction has none or can't be read.</summary>
    public CaptainUpgradeOptionSet Load(
        int factionId,
        bool showUnitsInInches,
        CancellationToken cancellationToken)
    {
        if (factionId <= 0)
        {
            return CaptainUpgradeOptionSet.Empty;
        }

        try
        {
            var snapshot = armyData.GetFactionSnapshot(factionId, cancellationToken);
            var filters = snapshot?.Filters;
            var skillLookup = CompanySelectionSharedUtilities.BuildIdNameLookup(filters, ArmyFilterSection.Skills);
            var equipLookup = CompanySelectionSharedUtilities.BuildIdNameLookup(filters, ArmyFilterSection.Equip);
            var weaponLookup = CompanySelectionSharedUtilities.BuildIdNameLookup(filters, ArmyFilterSection.Weapons);
            var extrasLookup = BuildExtrasLookup(filters);

            var skillRecords = specOps.GetSpecopsSkillsByFaction(factionId, cancellationToken);
            var equipRecords = specOps.GetSpecopsEquipsByFaction(factionId, cancellationToken);
            var weaponRecords = specOps.GetSpecopsWeaponsByFaction(factionId, cancellationToken);

            var skills = skillRecords
                .OrderBy(x => x.EntryOrder)
                .Select(x => ResolveSpecopsChoiceLabel(skillLookup, x.SkillId, x.Exp, "Skill", x.Extras, extrasLookup, showUnitsInInches))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            EnsureTagCompanyLieutenantSkillOption(factionId, skills);
            var equipment = equipRecords
                .OrderBy(x => x.EntryOrder)
                .Select(x => ResolveSpecopsChoiceLabel(equipLookup, x.EquipmentId, x.Exp, "Equipment", x.Extras, extrasLookup, showUnitsInInches))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var weapons = weaponRecords
                .OrderBy(x => x.EntryOrder)
                // Weapon choices have never shown their extras; x.Extras is available if they should.
                .Select(x => ResolveSpecopsChoiceLabel(weaponLookup, x.WeaponId, x.Exp, "Weapon", [], extrasLookup, showUnitsInInches))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new CaptainUpgradeOptionSet
            {
                Weapons = weapons,
                Skills = skills,
                Equipment = equipment
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Loading the captain options failed for faction {factionId}: {ex.Message}");
            return CaptainUpgradeOptionSet.Empty;
        }
    }

    private static void EnsureTagCompanyLieutenantSkillOption(int factionId, List<string> skills)
    {
        if (factionId != TagCompanyFactionId)
        {
            return;
        }

        if (skills.Any(LieutenantRules.IsLieutenantSkill))
        {
            return;
        }

        skills.Insert(0, CaptainChoiceLabel.Format(0, LieutenantRules.SkillName));
    }

    private static Dictionary<int, CaptainExtraDefinition> BuildExtrasLookup(ArmyFilterCatalog? filters)
    {
        filters ??= ArmyFilterCatalog.Empty;
        var map = filters.Extras.ToDictionary(x => x.Key, x => new CaptainExtraDefinition(x.Value.Name, x.Value.Type));
        if (map.Count > 0)
        {
            return map;
        }

        // Army filters use "extras"; keep "extra" as a backward-compatible fallback.
        foreach (var item in filters.Items("extra"))
        {
            if (!string.IsNullOrWhiteSpace(item.Name))
            {
                map[item.Id] = new CaptainExtraDefinition(item.Name, item.Type ?? string.Empty);
            }
        }

        return map;
    }

    private static string ResolveSpecopsChoiceLabel(
        IReadOnlyDictionary<int, string> lookup,
        int id,
        int points,
        string label,
        IReadOnlyList<int> extras,
        IReadOnlyDictionary<int, CaptainExtraDefinition> extrasLookup,
        bool showUnitsInInches)
    {
        var prefix = CaptainChoiceLabel.Format(points, string.Empty);
        var extrasSuffix = BuildExtrasSuffix(extras, extrasLookup, showUnitsInInches);
        if (lookup.TryGetValue(id, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return $"{prefix}{value.Trim()}{extrasSuffix}";
        }

        return $"{prefix}{label} {id}{extrasSuffix}";
    }

    private static string BuildExtrasSuffix(
        IReadOnlyList<int> extras,
        IReadOnlyDictionary<int, CaptainExtraDefinition> extrasLookup,
        bool showUnitsInInches)
    {
        var labels = extras
            .Select(extraId => extrasLookup.TryGetValue(extraId, out var resolved) && !string.IsNullOrWhiteSpace(resolved.Name)
                ? FormatExtraDisplay(resolved, showUnitsInInches)
                : extraId.ToString(CultureInfo.InvariantCulture))
            .ToList();

        return labels.Count == 0
            ? string.Empty
            : $" ({string.Join(", ", labels.Distinct(StringComparer.OrdinalIgnoreCase))})";
    }

    private static string FormatExtraDisplay(CaptainExtraDefinition definition, bool showUnitsInInches)
    {
        if (!string.Equals(definition.Type, "DISTANCE", StringComparison.OrdinalIgnoreCase))
        {
            return definition.Name;
        }

        return CompanySelectionSharedUtilities.ConvertDistanceText(definition.Name, showUnitsInInches);
    }

    private sealed record CaptainExtraDefinition(string Name, string Type);
}


