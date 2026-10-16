using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Company;
using InfinityMercsApp.Domain.Lieutenants;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Troopers;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.Common;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.Logos;
using InfinityMercsApp.Services.Packaging;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;
using InfinityMercsApp.ViewModels.Components;
using InfinityMercsApp.ViewModels.Company.Viewer.Items;
using InfinityMercsApp.ViewModels.Themes;

namespace InfinityMercsApp.ViewModels.Company.Viewer;

/// <summary>
/// Turns a saved company into the tiles of its unit strip, and a selected tile into what the unit-detail
/// display shows: stats with captain upgrades, trait icons, faction colours and weapons.
/// </summary>
public sealed class CompanyRoster(IArmyDataService armyData, FactionLogoCacheService? logoCache = null)
{
    public const int TagCompanyFactionId = 2003;
    public const string TagCompanyFallbackIconPath = "SVGCache/MercsIcons/noun-battle-mech-1731140.svg";

    public static readonly Color RangedColor = Color.FromArgb("#EF4444");
    public static readonly Color MeleeColor = Color.FromArgb("#22C55E");
    public static readonly Color EquipmentColor = Color.FromArgb("#06B6D4");
    public static readonly Color SkillsColor = Color.FromArgb("#F59E0B");
    public static readonly Color PeripheralsColor = Color.FromArgb("#FACC15");

    private static readonly Color DefaultAccent = Color.FromArgb("#93C5FD");

    /// <summary>
    /// The company's troopers, lieutenant first and then in saved order, with the captain's upgrades on the
    /// lieutenant. With a season, each also carries its experience, injuries, perks and other changes so far, and
    /// troopers retired this season are left out.
    /// </summary>
    public IReadOnlyList<CompanyViewerUnitListItem> UnitsOf(SavedCompanyFile company, SeasonFile? season = null)
    {
        var captain = company.ImprovedCaptainStats ?? new SavedImprovedCaptainStats();
        var captainName = string.IsNullOrWhiteSpace(captain.CaptainName) ? "Captain" : captain.CaptainName.Trim();

        return company.Entries
            .Where(entry => season?.IsRetired(entry.EntryIndex) != true)
            .OrderByDescending(entry => entry.IsLieutenant)
            .ThenBy(entry => entry.EntryIndex)
            .Select((entry, position) => UnitOf(entry, position, captain, captainName, season))
            .ToList();
    }

    private CompanyViewerUnitListItem UnitOf(SavedCompanyEntry entry, int position, SavedImprovedCaptainStats captain, string captainName, SeasonFile? season)
    {
        var sourceFactionId = EffectiveSourceFactionId(entry);
        var baseUnitName = !string.IsNullOrWhiteSpace(entry.BaseUnitName) ? entry.BaseUnitName
            : !string.IsNullOrWhiteSpace(entry.Name) ? entry.Name
            : $"Unit {position + 1}";
        var defaultName = entry.IsLieutenant ? captainName : baseUnitName;

        var filters = sourceFactionId > 0 ? armyData.GetFactionSnapshot(sourceFactionId)?.Filters : null;
        var weapons = SavedCodeNames.Weapons(filters, entry);
        var ranged = SavedCodeNames.JoinOrDash(weapons.Ranged);
        var skills = SavedCodeNames.JoinOrDash(SavedCodeNames.ForEntry(filters, entry, ProfileEntryKind.Skills));
        var equipment = SavedCodeNames.JoinOrDash(SavedCodeNames.ForEntry(filters, entry, ProfileEntryKind.Equip));
        if (entry.IsLieutenant && captain.IsEnabled)
        {
            ranged = ProfileText.AppendDistinct(ranged, SavedUnitText.CaptainChoiceNames(captain.WeaponChoice1, captain.WeaponChoice2, captain.WeaponChoice3));
            skills = ProfileText.AppendDistinct(skills, SavedUnitText.CaptainChoiceNames(captain.SkillChoice1, captain.SkillChoice2, captain.SkillChoice3));
            equipment = ProfileText.AppendDistinct(equipment, SavedUnitText.CaptainChoiceNames(captain.EquipmentChoice1, captain.EquipmentChoice2, captain.EquipmentChoice3));
        }

        var logoFactionId = entry.LogoSourceFactionId > 0 ? entry.LogoSourceFactionId : entry.SourceFactionId;
        var logoUnitId = entry.LogoSourceUnitId > 0 ? entry.LogoSourceUnitId : entry.SourceUnitId;

        var progress = SeasonTroopers.ProgressOf(entry, season);
        var profile = new TrooperProfile(
            SavedMove(entry), entry.CurrentCc, entry.CurrentBs, entry.CurrentPh, entry.CurrentWip, entry.CurrentArm, entry.CurrentBts,
            entry.CurrentVitaOrStr, entry.CurrentS,
            ProfileText.Lines(skills),
            ProfileText.Lines(SavedCodeNames.JoinOrDash(SavedCodeNames.ForEntry(filters, entry, ProfileEntryKind.Chars))))
            .With(progress.Change);

        return new CompanyViewerUnitListItem
        {
            Name = string.IsNullOrWhiteSpace(entry.CustomName) ? defaultName : entry.CustomName.Trim(),
            EntryIndex = entry.EntryIndex,
            BaseUnitName = baseUnitName,
            BaseUnitDisplayName = SavedUnitText.BaseDisplayName(baseUnitName),
            UnitTypeCode = SavedUnitText.UnitTypeCode(entry.UnitTypeCode),
            Subtitle = entry.IsPeripheralUnit ? "Peripheral" : entry.IsLieutenant ? "Lieutenant" : string.Empty,
            SourceFactionId = sourceFactionId,
            VisualFactionId = logoFactionId,
            SourceUnitId = entry.SourceUnitId,
            ProfileKey = entry.ProfileKey,
            IsPeripheralUnit = entry.IsPeripheralUnit,
            IsLieutenant = entry.IsLieutenant,
            Cost = entry.Cost,
            SavedEquipment = equipment,
            SavedSkills = ProfileText.JoinOrDash(profile.Skills),
            SavedCharacteristics = ProfileText.JoinOrDash(profile.Characteristics),
            SavedRangedWeapons = ranged,
            SavedCcWeapons = SavedCodeNames.JoinOrDash(weapons.Melee),
            SavedPeripheralNameHeading = entry.PeripheralNameHeading,
            UnitMov = profile.Move,
            UnitCc = profile.Cc,
            UnitBs = profile.Bs,
            UnitPh = profile.Ph,
            UnitWip = profile.Wip,
            UnitArm = profile.Arm,
            UnitBts = profile.Bts,
            UnitVitality = profile.Vitality,
            UnitS = profile.Silhouette,
            ExperiencePoints = progress.TotalXp,
            Progress = progress,
            CaptainIconPackagedPath = entry.IsLieutenant ? SavedUnitText.CaptainIconPath : string.Empty,
            ExperienceIconPackagedPath = SavedUnitText.ExperienceIconPath(progress.TotalXp),
            SelectionAccentColor = AccentFor(logoFactionId),
            CachedLogoPath = logoCache?.TryGetCachedUnitLogoPath(logoFactionId, logoUnitId),
            PackagedLogoPath = logoCache?.GetPackagedUnitLogoPath(logoFactionId, logoUnitId) ?? $"SVGCache/units/{logoFactionId}-{logoUnitId}.svg",
            FallbackLogoPath = IsTagCompanyUnit(sourceFactionId, logoFactionId, baseUnitName) ? TagCompanyFallbackIconPath : null
        };
    }

    // The movement in centimetres when the entry has both values, so it reads back unambiguously; otherwise as saved.
    private static string SavedMove(SavedCompanyEntry entry) =>
        entry.CurrentMoveFirstCm is > 0 && entry.CurrentMoveSecondCm is > 0
            ? $"{entry.CurrentMoveFirstCm}cm-{entry.CurrentMoveSecondCm}cm"
            : entry.CurrentMov;

    /// <summary>TAG company units keep the TAG company as their source, even when saved under another faction.</summary>
    public static int EffectiveSourceFactionId(SavedCompanyEntry entry) =>
        IsTagCompanyUnit(entry.SourceFactionId, entry.LogoSourceFactionId, entry.BaseUnitName) ? TagCompanyFactionId : entry.SourceFactionId;

    private static bool IsTagCompanyUnit(int sourceFactionId, int logoFactionId, string? baseUnitName) =>
        sourceFactionId == TagCompanyFactionId ||
        logoFactionId == TagCompanyFactionId ||
        (baseUnitName ?? string.Empty).Contains("Repurposed Mining Equipment", StringComparison.OrdinalIgnoreCase) ||
        (baseUnitName ?? string.Empty).Contains("Turtlemek", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The trooper's saved loadout as a single configuration: tidied skills, the lieutenant order, and its
    /// peripheral. With <paramref name="showUnitsInInches"/>, distances in its equipment, skills and
    /// characteristics are written in that unit.
    /// </summary>
    public static ViewerProfileItem ProfileOf(CompanyViewerUnitListItem item, bool? showUnitsInInches = null)
    {
        string InUnits(string text) => showUnitsInInches is { } inches ? TextDistances.Convert(text, inches) : text;

        var ranged = SavedUnitText.Section(item.SavedRangedWeapons);
        var melee = SavedUnitText.Section(item.SavedCcWeapons);
        var equipment = InUnits(SavedUnitText.Section(item.SavedEquipment));
        var skills = SavedUnitText.Section(item.SavedSkills);
        if (item.IsLieutenant)
        {
            skills = LieutenantRules.NormalizeOrderEntries(skills);
        }

        skills = InUnits(SkillDisplay.Normalize(skills));

        return new ViewerProfileItem
        {
            Name = item.Name,
            RangedWeapons = ranged,
            RangedWeaponsFormatted = Simple(ranged, RangedColor),
            MeleeWeapons = melee,
            MeleeWeaponsFormatted = Simple(melee, MeleeColor),
            UniqueEquipment = equipment,
            UniqueEquipmentFormatted = Simple(equipment, EquipmentColor),
            UniqueSkills = skills,
            UniqueSkillsFormatted = Simple(skills, SkillsColor),
            Characteristics = InUnits(SavedUnitText.Section(item.SavedCharacteristics)),
            Peripherals = PeripheralText(item.SavedPeripheralNameHeading),
            Cost = item.Cost.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Swc = "-",
            SwcDisplay = string.Empty,
            IsLieutenant = item.IsLieutenant,
            ProfileKey = item.ProfileKey
        };
    }

    /// <summary>
    /// Shows <paramref name="item"/> on <paramref name="detail"/>: its saved stat line (with the captain's bonuses
    /// on the lieutenant), its loadout as the only configuration, and the trait icons the loadout calls for.
    /// </summary>
    public static void ShowOn(UnitDetailViewModel detail, CompanyViewerUnitListItem item, ViewerProfileItem profile, SavedImprovedCaptainStats captain)
    {
        detail.ApplySavedUnitSnapshot(item.Name, new UnitStatLine
        {
            Mov = item.UnitMov,
            Cc = item.UnitCc,
            Bs = item.UnitBs,
            Ph = item.UnitPh,
            Wip = item.UnitWip,
            Arm = item.UnitArm,
            Bts = item.UnitBts,
            VitalityHeader = StatKeys.VitalityFor(item.UnitTypeCode).Key(),
            Vitality = item.UnitVitality,
            S = item.UnitS
        });

        if (item.IsLieutenant && captain.IsEnabled)
        {
            detail.ApplyCaptainStatBonuses(captain.Bonuses);
        }

        detail.Profiles.Clear();
        detail.Profiles.Add(profile);
        detail.ApplySelectedProfileTopSummaries(profile);
        detail.Icons = UnitHeaderIcons.From(
            UnitTraitIcons.From(profile.Characteristics, profile.UniqueSkills, profile.UniqueEquipment, item.IsPeripheralUnit),
            LieutenantIconCount(profile));
    }

    /// <summary>The trooper's logo: the downloaded one, else the packaged one (in either case), else its fallback.</summary>
    public static async Task<string?> LogoSourceAsync(CompanyViewerUnitListItem item, IAppPackageFiles packageFiles)
    {
        if (!string.IsNullOrWhiteSpace(item.CachedLogoPath) && File.Exists(item.CachedLogoPath))
        {
            return item.CachedLogoPath;
        }

        if (!string.IsNullOrWhiteSpace(item.PackagedLogoPath))
        {
            var packaged = item.PackagedLogoPath.Replace('\\', '/').TrimStart('/');
            foreach (var candidate in new List<string> { packaged, packaged.ToLowerInvariant() }.Distinct())
            {
                if (await packageFiles.ExistsAsync(candidate))
                {
                    return candidate;
                }
            }
        }

        return item.FallbackLogoPath ?? item.PackagedLogoPath;
    }

    /// <summary>How many lieutenant order icons the profile earns: one for being the lieutenant, plus any bonus orders.</summary>
    public static int LieutenantIconCount(ViewerProfileItem profile) =>
        (profile.IsLieutenant ? 1 : 0) + LieutenantRules.CountBonusOrders(profile.UniqueSkills);

    /// <summary>The unit header's colours for a faction, falling back to the display's default red.</summary>
    public CompanyHeaderColors HeaderColorsFor(int factionId) =>
        CompanySelectionVisualThemeWorkflow.GetHeaderColors(ThemeFactionName(factionId));

    /// <summary>The faction's accent for a selected tile.</summary>
    public Color AccentFor(int factionId) =>
        CompanySelectionVisualThemeWorkflow.GetFactionTheme(ThemeFactionName(factionId), DefaultAccent, Color.FromArgb("#4B5563")).Primary;

    private string? ThemeFactionName(int factionId)
    {
        if (factionId <= 0)
        {
            return null;
        }

        var metadataName = armyData.GetMetadataFactionById(factionId)?.Name;
        return !string.IsNullOrWhiteSpace(metadataName) && CompanySelectionSharedUtilities.IsThemeFactionName(metadataName)
            ? metadataName
            : CompanySelectionVisualThemeWorkflow.InferThemeFactionNameFromFactionId(factionId);
    }

    public static FormattedString Simple(string? text, Color color)
    {
        var formatted = new FormattedString();
        formatted.Spans.Add(new Span { Text = string.IsNullOrWhiteSpace(text) ? "-" : text, TextColor = color });
        return formatted;
    }

    private static string PeripheralText(string? peripheralHeading)
    {
        var name = string.IsNullOrWhiteSpace(peripheralHeading)
            ? string.Empty
            : CompanySelectionSharedUtilities.ExtractFirstPeripheralName(peripheralHeading);
        return string.IsNullOrWhiteSpace(name) ? "-" : $"{name} (1)";
    }
}
