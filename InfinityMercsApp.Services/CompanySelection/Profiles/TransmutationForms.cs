using InfinityMercsApp.Domain.Models.Army.Filters;

namespace InfinityMercsApp.Services.CompanySelection.Profiles;

/// <summary>
/// Transmutation (Auto) is banned: a trooper with it is recruited in one form the player picks, uses only that
/// form's profile, and loses the skill.
/// </summary>
public static class TransmutationForms
{
    /// <summary>Whether <paramref name="skill"/> is Transmutation (Auto).</summary>
    public static bool IsAuto(string? skill) =>
        skill is not null &&
        skill.Contains("Transmutation", StringComparison.OrdinalIgnoreCase) &&
        skill.Contains("Auto", StringComparison.OrdinalIgnoreCase);

    /// <summary>The forms of a configuration's group to pick from: its profiles when there is more than one, else none.</summary>
    public static IReadOnlyList<UnitProfile> FormsOf(UnitProfileGroups profileGroups, int? groupId) =>
        profileGroups.Groups.FirstOrDefault(group => group.Id == groupId) is { Profiles.Count: > 1 } group ? group.Profiles : [];

    /// <summary>A form with its own skills, equipment and characteristics named from the army filters.</summary>
    public static TrooperForm Of(UnitProfile form, ArmyFilterCatalog filters, bool showUnitsInInches)
    {
        var names = new ProfileEntryNames(filters.Extras, showUnitsInInches);
        return new TrooperForm(
            form,
            names.OrderedUnique(form.Skills, filters.Names(ArmyFilterSection.Skills)).Where(skill => !IsAuto(skill)).ToList(),
            names.OrderedUnique(form.Equip, filters.Names(ArmyFilterSection.Equip)),
            names.OrderedUnique(form.Chars, filters.Names(ArmyFilterSection.Chars)));
    }
}
