using System.Globalization;
using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;

namespace InfinityMercsApp.Services.Season.Peripherals;

/// <summary>
/// The peripherals the Peripheral (Controller) augment can add to a company: any peripheral in the Tohaa army list
/// that isn't a Servant (or Ancillary), mercenaries left out. Each joins the roster as a trooper of its own.
/// </summary>
public sealed partial class TohaaPeripherals(IArmyDataService armyData)
{
    public const int TohaaFactionId = 801;

    /// <summary>The augment that adds one, as the Greengrocer sells it.</summary>
    public const string AugmentName = "Peripheral (Controller)";

    // Mercenaries in an army list have ids from 10000.
    private const int FirstMercenaryUnitId = 10000;

    /// <summary>The peripherals on offer, by unit and then option.</summary>
    public IReadOnlyList<PeripheralOffer> Offers()
    {
        var filters = armyData.GetFactionSnapshot(TohaaFactionId)?.Filters ?? ArmyFilterCatalog.Empty;
        var offers = new List<PeripheralOffer>();
        foreach (var resume in armyData.GetResumeByFaction(TohaaFactionId).Where(resume => resume.UnitId < FirstMercenaryUnitId))
        {
            if (armyData.GetUnit(TohaaFactionId, resume.UnitId) is not { } unit)
            {
                continue;
            }

            foreach (var group in unit.ProfileGroups.Groups.Where(group => group.Id is not null && IsOnOffer(group, filters)))
            {
                foreach (var option in group.Options.Where(option => option.Id is not null && !option.Disabled))
                {
                    var loadout = string.Join(", ", new List<ProfileEntryKind> { ProfileEntryKind.Weapons, ProfileEntryKind.Equip, ProfileEntryKind.Skills }
                        .SelectMany(kind => SavedCodeNames.Resolve(
                            filters, unit.ProfileGroups.GetEntriesWithIncludes(option, kind).Where(entry => entry.Id is not null).Select(Code), kind)));
                    offers.Add(new PeripheralOffer(
                        unit.UnitId, unit.Name.Trim(), group.Id ?? 0, option.Id ?? 0, NameOf(option.Name ?? group.Profiles[0].Name ?? string.Empty), option.CostValue, loadout));
                }
            }
        }

        return offers;
    }

    /// <summary>
    /// The roster entry for <paramref name="offer"/>: its profile and option's stats, skills and gear, connected to
    /// <paramref name="controllerEntryIndex"/>; null when the army data no longer has it.
    /// </summary>
    public SavedCompanyEntry? EntryFor(PeripheralOffer offer, int entryIndex, int controllerEntryIndex)
    {
        if (armyData.GetUnit(TohaaFactionId, offer.UnitId) is not { } unit ||
            unit.ProfileGroups.Groups.FirstOrDefault(group => group.Id == offer.GroupId) is not { Profiles.Count: > 0 } group ||
            group.Options.FirstOrDefault(option => option.Id == offer.OptionId) is not { } option)
        {
            return null;
        }

        var profile = group.Profiles[0];
        var types = armyData.GetFactionSnapshot(TohaaFactionId)?.Filters.Names(ArmyFilterSection.Type);
        List<CompanySavedCodeRef> Codes(ProfileEntryKind kind) =>
            [.. profile.Entries(kind).Concat(unit.ProfileGroups.GetEntriesWithIncludes(option, kind)).Where(entry => entry.Id is not null).Select(Code)];

        return new SavedCompanyEntry
        {
            EntryIndex = entryIndex,
            Name = offer.Name,
            BaseUnitName = offer.Name,
            CustomName = offer.Name,
            UnitTypeCode = profile.Type is { } type && types?.GetValueOrDefault(type) is { Length: > 0 } typeName
                ? typeName.Trim().ToUpperInvariant()
                : UnitTypeCodes.Peripheral,
            ProfileKey = $"{TohaaFactionId}:{offer.UnitId}:{offer.GroupId}:{offer.OptionId}",
            SourceFactionId = TohaaFactionId,
            SourceUnitId = offer.UnitId,
            LogoSourceFactionId = TohaaFactionId,
            LogoSourceUnitId = offer.UnitId,
            IsPeripheralUnit = true,
            ParentEntryIndex = controllerEntryIndex,
            FromControllerAugment = true,
            Cost = offer.Cost,
            CurrentMov = profile.Move,
            CurrentMoveFirstCm = profile.FirstMoveCm,
            CurrentMoveSecondCm = profile.SecondMoveCm,
            CurrentCc = UnitProfile.StatText(profile.Cc),
            CurrentBs = UnitProfile.StatText(profile.Bs),
            CurrentPh = UnitProfile.StatText(profile.Ph),
            CurrentWip = UnitProfile.StatText(profile.Wip),
            CurrentArm = UnitProfile.StatText(profile.Arm),
            CurrentBts = UnitProfile.StatText(profile.Bts),
            CurrentVitaOrStr = profile.Vitality.Value,
            CurrentS = UnitProfile.StatText(profile.S),
            CurrentCharacteristicCodes = Codes(ProfileEntryKind.Chars),
            CurrentSkillCodes = Codes(ProfileEntryKind.Skills),
            CurrentEquipmentCodes = Codes(ProfileEntryKind.Equip),
            CurrentWeaponCodes = Codes(ProfileEntryKind.Weapons)
        };
    }

    // A peripheral's own group: its profile has the Peripheral skill, of a kind other than Servant or Ancillary.
    private static bool IsOnOffer(ProfileGroup group, ArmyFilterCatalog filters)
    {
        if (group.Profiles.Count == 0)
        {
            return false;
        }

        var skills = SavedCodeNames.Resolve(filters, group.Profiles[0].Skills.Where(entry => entry.Id is not null).Select(Code), ProfileEntryKind.Skills);
        return skills.Any(skill => PeripheralSkill().Match(skill) is { Success: true } peripheral &&
                                   !peripheral.Groups["kind"].Value.Contains("Servant", StringComparison.OrdinalIgnoreCase) &&
                                   !peripheral.Groups["kind"].Value.Contains("Ancillary", StringComparison.OrdinalIgnoreCase) &&
                                   !peripheral.Groups["kind"].Value.Contains("Control", StringComparison.OrdinalIgnoreCase));
    }

    private static CompanySavedCodeRef Code(ProfileEntry entry) =>
        new() { Id = entry.Id ?? 0, Extra = entry.Extras.Count > 0 ? [.. entry.Extras] : null };

    // "CHAKSA_1 HELPER" is the Chaksa Helper.
    private static string NameOf(string optionName) =>
        CultureInfo.InvariantCulture.TextInfo.ToTitleCase(OptionNumber().Replace(optionName.Trim(), string.Empty).ToLowerInvariant());

    [GeneratedRegex(@"^\s*Peripheral\s*\((?<kind>[^)]*)\)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex PeripheralSkill();

    [GeneratedRegex(@"_\d+")]
    private static partial Regex OptionNumber();
}
