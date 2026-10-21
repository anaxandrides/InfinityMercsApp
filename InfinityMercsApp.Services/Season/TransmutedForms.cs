using System.Globalization;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Season.Game;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Services.CompanySelection.Profiles;

namespace InfinityMercsApp.Services.Season;

/// <summary>
/// Finds the second profile of a Transmutation (X) trooper in the army data. Only the first profile is saved, since
/// perks, armour and other bonuses apply to it alone.
/// </summary>
public sealed class TransmutedForms(IArmyDataService armyData)
{
    // A saved profile key reads "group|option|cost|swc|lt:N".
    private const char ProfileKeySeparator = '|';

    /// <summary>
    /// The form the trooper becomes, or null when it has no Transmutation (X), or wears Symbio-Resonance.
    /// </summary>
    /// <param name="vitality">The trooper's VITA or STR now, which raises the threshold above its profile's.</param>
    public TransmutedForm? For(SavedCompanyEntry entry, int factionId, string? armorName, int vitality, bool showUnitsInInches)
    {
        if (Transmutation.IsSymbioResonance(armorName) ||
            armyData.GetUnit(factionId, entry.SourceUnitId) is not { } unit ||
            armyData.GetFactionSnapshot(factionId)?.Filters is not { } filters)
        {
            return null;
        }

        var groupName = entry.ProfileKey.Split(ProfileKeySeparator)[0];
        var groups = unit.ProfileGroups.Groups.Where(group => group.Profiles.Count > 1).ToList();
        foreach (var group in groups.OrderByDescending(group => string.Equals(group.Isc, groupName, StringComparison.OrdinalIgnoreCase)))
        {
            var first = TransmutationForms.Of(group.Profiles[0], filters, showUnitsInInches);
            if (first.Skills.Select(Transmutation.Threshold).FirstOrDefault(threshold => threshold is not null) is not { } threshold)
            {
                continue;
            }

            var second = group.Profiles[1];
            var form = TransmutationForms.Of(second, filters, showUnitsInInches);
            var profileVitality = int.TryParse(group.Profiles[0].Vitality.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var printed)
                ? printed
                : vitality;
            return new TransmutedForm(
                second.Name ?? string.Empty,
                Transmutation.Raised(threshold, profileVitality, vitality),
                [
                    new(Stat.Mov, Movement.Format(second.Movement, showUnitsInInches)),
                    new(Stat.Cc, UnitProfile.StatText(second.Cc)),
                    new(Stat.Bs, UnitProfile.StatText(second.Bs)),
                    new(Stat.Ph, UnitProfile.StatText(second.Ph)),
                    new(Stat.Wip, UnitProfile.StatText(second.Wip)),
                    new(Stat.Arm, UnitProfile.StatText(second.Arm)),
                    new(Stat.Bts, UnitProfile.StatText(second.Bts)),
                    new(second.Vitality.Stat, vitality.ToString(CultureInfo.InvariantCulture)),
                    new(Stat.S, UnitProfile.StatText(second.S))
                ],
                form.Skills,
                form.Equipment);
        }

        return null;
    }
}
