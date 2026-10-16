using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Company;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Season.Troopers;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanyProfiles;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.Logos;
using InfinityMercsApp.Services.Stores;
using InfinityMercsApp.ViewModels.Season.Items;
using InfinityMercsApp.Services.Season;

namespace InfinityMercsApp.ViewModels.Season;

/// <summary>
/// Builds the units a company can deploy for a mission: each saved entry's profile resolved from the
/// army data, with the captain's upgrades and the season's gear applied.
/// </summary>
public sealed partial class DeploymentRoster(
    IArmyDataService armyData,
    ISeasonFileService seasonFiles,
    StoreCatalog stores,
    FactionLogoCacheService? logos = null)
{
    internal const int TagCompanyFactionId = 2003;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// The company's units, lieutenant first then in company order; peripherals are left out.
    /// </summary>
    /// <param name="deployedEntryIndices">When given, only these entries are included.</param>
    /// <returns>No units when the company file is missing or unreadable.</returns>
    public async Task<IReadOnlyList<DeploymentUnitItem>> LoadAsync(
        string companyFilePath,
        string? seasonFilePath,
        bool showUnitsInInches,
        IReadOnlySet<int>? deployedEntryIndices = null)
    {
        SavedCompanyFile? company;
        try
        {
            company = File.Exists(companyFilePath)
                ? JsonSerializer.Deserialize<SavedCompanyFile>(await File.ReadAllTextAsync(companyFilePath), JsonOptions)
                : null;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Deployment roster failed to read '{companyFilePath}': {ex.Message}");
            return [];
        }

        if (company?.Entries is null)
        {
            return [];
        }

        var season = string.IsNullOrWhiteSpace(seasonFilePath) ? null : await seasonFiles.LoadAsync(seasonFilePath);
        var seasonGear = season?.UnitGear ?? [];
        var captain = CaptainUpgrades.From(company.ImprovedCaptainStats);
        var catalog = await stores.CatalogAsync();

        return company.Entries
            .Where(e => !e.IsPeripheralUnit)
            .Where(e => deployedEntryIndices is null || deployedEntryIndices.Contains(e.EntryIndex))
            .OrderByDescending(e => e.IsLieutenant)
            .ThenBy(e => e.EntryIndex)
            .Select(entry => BuildUnit(
                entry,
                captain,
                seasonGear.GetValueOrDefault(entry.EntryIndex),
                SeasonTroopers.ProgressOf(entry, season),
                SeasonRenown.Of(entry, season, catalog).Total,
                showUnitsInInches))
            .ToList();
    }

    private DeploymentUnitItem BuildUnit(
        SavedCompanyEntry entry,
        CaptainUpgrades captain,
        IReadOnlyList<SeasonUnitGear>? gear,
        TrooperProgress progress,
        int renown,
        bool showUnitsInInches)
    {
        var factionId = EffectiveSourceFactionId(entry);
        var filters = armyData.GetFactionSnapshot(factionId)?.Filters;
        var profile = UnitProfile.From(entry);
        profile.Skills = SavedCodeNames.JoinOrDash(SavedCodeNames.ForEntry(filters, entry, ProfileEntryKind.Skills));
        profile.Equipment = SavedCodeNames.JoinOrDash(SavedCodeNames.ForEntry(filters, entry, ProfileEntryKind.Equip));
        var weapons = SavedCodeNames.Weapons(filters, entry);
        profile.RangedWeapons = SavedCodeNames.JoinOrDash(weapons.Ranged);
        profile.MeleeWeapons = SavedCodeNames.JoinOrDash(weapons.Melee);
        var characteristics = SavedCodeNames.JoinOrDash(SavedCodeNames.ForEntry(filters, entry, ProfileEntryKind.Chars));

        if (entry.IsLieutenant)
        {
            captain.ApplyTo(profile);
        }

        ApplySeasonGear(profile, gear ?? []);

        // Injuries, perks, rank skills and services change the trooper the game sees.
        var changed = new TrooperProfile(
            MoveText(entry), profile.Cc, profile.Bs, profile.Ph, profile.Wip, profile.Arm, profile.Bts, profile.Vitality, profile.S,
            ProfileText.Lines(profile.Skills), ProfileText.Lines(characteristics))
            .With(progress.Change);
        (profile.Cc, profile.Bs, profile.Ph, profile.Wip, profile.Arm, profile.Bts, profile.Vitality, profile.S) =
            (changed.Cc, changed.Bs, changed.Ph, changed.Wip, changed.Arm, changed.Bts, changed.Vitality, changed.Silhouette);
        profile.Skills = ProfileText.JoinOrDash(changed.Skills);
        characteristics = ProfileText.JoinOrDash(changed.Characteristics);

        var baseUnitName = string.IsNullOrWhiteSpace(entry.BaseUnitName) ? entry.Name : entry.BaseUnitName;
        var logoFactionId = entry.LogoSourceFactionId > 0 ? entry.LogoSourceFactionId : entry.SourceFactionId;
        var logoUnitId = entry.LogoSourceUnitId > 0 ? entry.LogoSourceUnitId : entry.SourceUnitId;
        var experienceLevel = progress.RankLevel;

        return new DeploymentUnitItem
        {
            EntryIndex = entry.EntryIndex,
            IsChecked = entry.IsLieutenant,
            Name = string.IsNullOrWhiteSpace(entry.CustomName) ? baseUnitName : entry.CustomName.Trim(),
            BaseUnitDisplayName = BaseDisplayName(baseUnitName),
            Subtitle = entry.IsLieutenant ? "Lieutenant" : string.Empty,
            IsLieutenant = entry.IsLieutenant,
            IsIrregular = HasLine(characteristics, "Irregular", splitOnCommas: true),
            IsImpetuous = HasLine(characteristics, "Impetuous", splitOnCommas: true),
            IsTagFromTagCompany = factionId == TagCompanyFactionId &&
                                  UnitTypeCodes.IsTag(entry.UnitTypeCode),
            CaptainIconPackagedPath = entry.IsLieutenant ? "SVGCache/NonCBIcons/noun-captain-8115950.svg" : string.Empty,
            ExperienceIconPackagedPath = experienceLevel <= 0 ? string.Empty : $"SVGCache/NonCBIcons/Experience/noun-{experienceLevel}-stars.svg",
            CachedLogoPath = logos?.TryGetCachedUnitLogoPath(logoFactionId, logoUnitId),
            PackagedLogoPath = logos?.GetPackagedUnitLogoPath(logoFactionId, logoUnitId)
                               ?? $"SVGCache/units/{logoFactionId}-{logoUnitId}.svg",
            UnitMov = SeasonDisplayUnitFormatter.FormatMoveValue(changed.Move, null, null, showUnitsInInches),
            UnitCc = profile.Cc,
            UnitBs = profile.Bs,
            UnitPh = profile.Ph,
            UnitWip = profile.Wip,
            UnitArm = profile.Arm,
            UnitBts = profile.Bts,
            UnitVitality = profile.Vitality,
            UnitS = profile.S,
            Renown = renown,
            VitalityStat = StatKeys.VitalityFor(entry.UnitTypeCode),
            StartingVitality = Math.Max(1, int.TryParse(entry.CurrentVitaOrStr, out var vitality) ? vitality : 1),
            HasNwi = HasLine(profile.Skills, "No Wound Incapacitation"),
            HasRemotePresence = HasLine(profile.Skills, "Remote Presence"),
            Equipment = TextDistances.Convert(profile.Equipment, showUnitsInInches),
            Skills = TextDistances.Convert(profile.Skills, showUnitsInInches),
            RangedWeapons = TextDistances.Convert(profile.RangedWeapons, showUnitsInInches),
            MeleeWeapons = TextDistances.Convert(profile.MeleeWeapons, showUnitsInInches)
        };
    }

    /// <summary>
    /// Adds season gear: weapons to the ranged or melee list, roles to skills, and augments either
    /// as stat overrides ("BS=14") or as skills.
    /// </summary>
    internal static void ApplySeasonGear(UnitProfile profile, IEnumerable<SeasonUnitGear> gear)
    {
        foreach (var item in gear.Where(g => !string.IsNullOrWhiteSpace(g.ItemName)))
        {
            if (!GearSlots.TryParse(item.Slot, out var slot))
            {
                continue;
            }

            switch (slot)
            {
                case GearSlot.Primary or GearSlot.Secondary or GearSlot.Sidearm or GearSlot.Accessories:
                    if (CompanyProfileTextService.IsMeleeWeaponName(item.ItemName))
                    {
                        profile.MeleeWeapons = AppendKeepingText(profile.MeleeWeapons, [item.ItemName]);
                    }
                    else
                    {
                        profile.RangedWeapons = AppendKeepingText(profile.RangedWeapons, [item.ItemName]);
                    }

                    break;

                case GearSlot.Roles:
                    profile.Skills = AppendKeepingText(profile.Skills, [item.ItemName]);
                    break;

                case GearSlot.Augments:
                    if (StatOverride.TryParse(item.ItemName, out var statOverride))
                    {
                        profile.SetStat(statOverride);
                    }
                    else
                    {
                        profile.Skills = AppendKeepingText(profile.Skills, [item.ItemName]);
                    }

                    break;
            }
        }
    }

    // The movement in centimetres when the entry has both values, so it reads back unambiguously; otherwise as saved.
    private static string MoveText(SavedCompanyEntry entry) =>
        entry.CurrentMoveFirstCm is > 0 && entry.CurrentMoveSecondCm is > 0
            ? $"{entry.CurrentMoveFirstCm}cm-{entry.CurrentMoveSecondCm}cm"
            : entry.CurrentMov;

    /// <summary>TAG Company units, including its repurposed mining TAGs, take their profiles from the TAG Company faction.</summary>
    internal static int EffectiveSourceFactionId(SavedCompanyEntry entry)
    {
        if (entry.SourceFactionId == TagCompanyFactionId || entry.LogoSourceFactionId == TagCompanyFactionId)
        {
            return TagCompanyFactionId;
        }

        var baseName = entry.BaseUnitName ?? string.Empty;
        return baseName.Contains("Repurposed Mining Equipment", StringComparison.OrdinalIgnoreCase) ||
               baseName.Contains("Turtlemek", StringComparison.OrdinalIgnoreCase)
            ? TagCompanyFactionId
            : entry.SourceFactionId;
    }

    /// <summary>The unit name without bracketed qualifiers, e.g. "Fusilier (Hacker)" becomes "Fusilier".</summary>
    internal static string BaseDisplayName(string? baseUnitName)
    {
        if (string.IsNullOrWhiteSpace(baseUnitName))
        {
            return "Unit";
        }

        var name = MultipleSpacesPattern().Replace(BracketsPattern().Replace(baseUnitName, " "), " ").Trim();
        return string.IsNullOrWhiteSpace(name) ? "Unit" : name;
    }

    /// <summary>True when one line (or, optionally, comma-separated item) of the text is exactly <paramref name="value"/>.</summary>
    internal static bool HasLine(string text, string value, bool splitOnCommas = false) =>
        text.Split(splitOnCommas ? ['\r', '\n', ','] : ['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Any(line => line.Trim().Equals(value, StringComparison.OrdinalIgnoreCase));

    // Adds items to a profile section; with nothing to add, the section's text is left as it is.
    private static string AppendKeepingText(string? baseText, IReadOnlyList<string> additions) =>
        ProfileText.AppendDistinct(baseText, additions, keepTextWhenNothingAdded: true);

    [GeneratedRegex(@"\s*\([^)]*\)\s*")]
    private static partial Regex BracketsPattern();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex MultipleSpacesPattern();

    [GeneratedRegex(@"^\s*\([-+]?\d+\)\s*-\s*")]
    private static partial Regex CaptainChoiceCostPattern();

    /// <summary>A unit's stats and profile text while it is being assembled.</summary>
    internal sealed class UnitProfile
    {
        public string Cc { get; set; } = "-";
        public string Bs { get; set; } = "-";
        public string Ph { get; set; } = "-";
        public string Wip { get; set; } = "-";
        public string Arm { get; set; } = "-";
        public string Bts { get; set; } = "-";
        public string S { get; set; } = "-";
        public string Vitality { get; set; } = "-";
        public string Skills { get; set; } = "-";
        public string Equipment { get; set; } = "-";
        public string RangedWeapons { get; set; } = "-";
        public string MeleeWeapons { get; set; } = "-";

        public static UnitProfile From(SavedCompanyEntry entry) => new()
        {
            Cc = entry.CurrentCc,
            Bs = entry.CurrentBs,
            Ph = entry.CurrentPh,
            Wip = entry.CurrentWip,
            Arm = entry.CurrentArm,
            Bts = entry.CurrentBts,
            S = entry.CurrentS,
            Vitality = entry.CurrentVitaOrStr
        };

        /// <summary>Overrides a stat; stats the deployment card does not show (MOV, AVA) are ignored.</summary>
        public void SetStat(StatOverride statOverride)
        {
            var value = statOverride.Value.ToString(CultureInfo.InvariantCulture);
            switch (statOverride.Stat)
            {
                case Stat.Cc: Cc = value; break;
                case Stat.Bs: Bs = value; break;
                case Stat.Ph: Ph = value; break;
                case Stat.Wip: Wip = value; break;
                case Stat.Arm: Arm = value; break;
                case Stat.Bts: Bts = value; break;
                case Stat.S: S = value; break;
                case Stat.Vita or Stat.Str or Stat.W: Vitality = value; break;
            }
        }
    }

    /// <summary>The weapons, skills and equipment an improved captain adds to the lieutenant.</summary>
    internal sealed record CaptainUpgrades(IReadOnlyList<string> Weapons, IReadOnlyList<string> Skills, IReadOnlyList<string> Equipment)
    {
        private static readonly CaptainUpgrades None = new([], [], []);

        public static CaptainUpgrades From(SavedImprovedCaptainStats? stats) => stats is not { IsEnabled: true }
            ? None
            : new CaptainUpgrades(
                Choices(stats.WeaponChoice1, stats.WeaponChoice2, stats.WeaponChoice3),
                Choices(stats.SkillChoice1, stats.SkillChoice2, stats.SkillChoice3),
                Choices(stats.EquipmentChoice1, stats.EquipmentChoice2, stats.EquipmentChoice3));

        public void ApplyTo(UnitProfile profile)
        {
            if (ReferenceEquals(this, None))
            {
                return;
            }

            profile.RangedWeapons = AppendKeepingText(profile.RangedWeapons, Weapons.Where(w => !CompanyProfileTextService.IsMeleeWeaponName(w)).ToList());
            profile.MeleeWeapons = AppendKeepingText(profile.MeleeWeapons, Weapons.Where(CompanyProfileTextService.IsMeleeWeaponName).ToList());
            profile.Skills = AppendKeepingText(profile.Skills, Skills);
            profile.Equipment = AppendKeepingText(profile.Equipment, Equipment);
        }

        /// <summary>The chosen upgrades without their "(+N) - " cost prefix; blanks and dashes are dropped.</summary>
        internal static IReadOnlyList<string> Choices(params IEnumerable<string?> choices) => choices
            .OfType<string>()
            .Where(c => !string.IsNullOrWhiteSpace(c) && c.Trim() != "-")
            .Select(c => CaptainChoiceCostPattern().Replace(c.Trim(), string.Empty).Trim())
            .Where(c => c.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
