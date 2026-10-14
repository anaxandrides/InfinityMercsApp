using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Services;

namespace InfinityMercsApp.Views.Common;

/// <summary>A unit's weapons split into ranged and close-combat lists.</summary>
public sealed record WeaponLists(IReadOnlyList<string> Ranged, IReadOnlyList<string> Melee)
{
    public static WeaponLists Split(IEnumerable<string> weapons)
    {
        var list = weapons.ToList();
        return new WeaponLists(
            list.Where(name => !CompanyProfileTextService.IsMeleeWeaponName(name)).ToList(),
            list.Where(CompanyProfileTextService.IsMeleeWeaponName).ToList());
    }
}

/// <summary>Turns the codes saved on a company entry back into display names using an army's filters.</summary>
public static class SavedCodeNames
{
    /// <summary>The name of each known code, with its extras in brackets ("Combi Rifle (+1B)"); unknown codes are skipped.</summary>
    public static List<string> Resolve(ArmyFilterCatalog? filters, IEnumerable<CompanySavedCodeRef?>? codes, ProfileEntryKind kind)
    {
        var codeList = (codes ?? []).Where(code => code is { Id: > 0 }).Cast<CompanySavedCodeRef>().ToList();
        if (codeList.Count == 0)
        {
            return [];
        }

        var catalog = filters ?? ArmyFilterCatalog.Empty;
        var names = catalog.Names(ArmyFilterSection.For(kind));
        var extraNames = catalog.Names(ArmyFilterSection.Extras);
        var resolved = new List<string>();
        foreach (var code in codeList)
        {
            if (!names.TryGetValue(code.Id, out var name) || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var extras = (code.Extra ?? [])
                .Distinct()
                .Select(id => extraNames.TryGetValue(id, out var extra) ? extra?.Trim() : null)
                .Where(extra => !string.IsNullOrWhiteSpace(extra))
                .ToList();
            resolved.Add(extras.Count > 0 ? $"{name.Trim()} ({string.Join(", ", extras)})" : name.Trim());
        }

        return resolved;
    }

    /// <summary>The entry's coded names followed by its custom (uncoded) names for one kind.</summary>
    public static List<string> ForEntry(ArmyFilterCatalog? filters, CompanySavedCompanyEntryBase entry, ProfileEntryKind kind)
    {
        var names = Resolve(filters, entry.CurrentCodes(kind), kind);
        names.AddRange(entry.CustomNames(kind));
        return names;
    }

    /// <summary>The entry's weapons, coded and custom, without blanks, dashes or repeats, split into ranged and melee.</summary>
    public static WeaponLists Weapons(ArmyFilterCatalog? filters, CompanySavedCompanyEntryBase entry) =>
        WeaponLists.Split(ForEntry(filters, entry, ProfileEntryKind.Weapons)
            .Where(name => !string.IsNullOrWhiteSpace(name) && name.Trim() != "-")
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase));

    /// <summary>Names one per line, trimmed, without blanks, dashes or repeats; "-" when none are left.</summary>
    public static string JoinOrDash(IEnumerable<string> values)
    {
        var lines = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Where(value => value != "-")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return lines.Count == 0 ? "-" : string.Join(Environment.NewLine, lines);
    }
}
