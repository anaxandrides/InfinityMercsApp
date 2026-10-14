using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanyProfiles;
using InfinityMercsApp.Services.CompanySelection.Common;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Views.Controls;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;

namespace InfinityMercsApp.Services.CompanySelection.Visuals;

internal static class CompanySelectionVisualThemeWorkflow
{
    internal static string? ResolveThemeFactionName(
        ArmySourceSelectionMode mode,
        IArmyDataService armyDataService,
        int sourceFactionId,
        IReadOnlyList<int>? unitFactionIds,
        CancellationToken cancellationToken)
    {
        if (mode == ArmySourceSelectionMode.Sectorials)
        {
            return ResolveVanillaFactionName(armyDataService, sourceFactionId, cancellationToken);
        }

        return ResolveUnitVanillaFactionName(armyDataService, sourceFactionId, unitFactionIds, cancellationToken);
    }

    internal static string? ResolveUnitVanillaFactionName(
        IArmyDataService armyDataService,
        int sourceFactionId,
        IReadOnlyList<int>? unitFactionIds,
        CancellationToken cancellationToken)
    {
        foreach (var factionId in unitFactionIds ?? [])
        {
            var candidateName = ResolveVanillaFactionName(armyDataService, factionId, cancellationToken);
            if (CompanySelectionSharedUtilities.IsThemeFactionName(candidateName))
            {
                return candidateName;
            }
        }

        return ResolveVanillaFactionName(armyDataService, sourceFactionId, cancellationToken);
    }

    internal static string? ResolveVanillaFactionName(
        IArmyDataService armyDataService,
        int sourceFactionId,
        CancellationToken cancellationToken)
    {
        if (sourceFactionId <= 0)
        {
            return null;
        }

        var source = armyDataService.GetMetadataFactionById(sourceFactionId);
        FactionRecord? current = source is null
            ? null
            : new FactionRecord
            {
                Id = source.Id,
                ParentId = source.ParentId,
                Name = source.Name,
                Slug = source.Slug,
                Discontinued = source.Discontinued,
                Logo = source.Logo
            };

        var safety = 0;
        while (current is not null && safety < 8)
        {
            if (CompanySelectionSharedUtilities.IsThemeFactionName(current.Name))
            {
                return current.Name;
            }

            if (current.ParentId <= 0)
            {
                break;
            }

            var parentRecord = armyDataService.GetMetadataFactionById(current.ParentId);
            FactionRecord? parent = parentRecord is null
                ? null
                : new FactionRecord
                {
                    Id = parentRecord.Id,
                    ParentId = parentRecord.ParentId,
                    Name = parentRecord.Name,
                    Slug = parentRecord.Slug,
                    Discontinued = parentRecord.Discontinued,
                    Logo = parentRecord.Logo
                };

            if (parent is null || parent.Id == current.Id)
            {
                break;
            }

            current = parent;
            safety++;
        }

        var inferredThemeName = InferThemeFactionNameFromFactionId(sourceFactionId)
            ?? (current is not null ? InferThemeFactionNameFromFactionId(current.Id) : null);
        if (!string.IsNullOrWhiteSpace(inferredThemeName))
        {
            return inferredThemeName;
        }

        return current?.Name;
    }

    internal static CompanyHeaderColors GetHeaderColors(string? vanillaFactionName, Color defaultPrimary, Color defaultSecondary)
    {
        var (primary, secondary) = GetFactionTheme(vanillaFactionName, defaultPrimary, defaultSecondary);
        var primaryText = CompanySelectionSharedUtilities.IsLightColor(primary) ? Colors.Black : Colors.White;
        var secondaryText = CompanySelectionSharedUtilities.IsLightColor(secondary) ? Colors.Black : Colors.White;
        return new CompanyHeaderColors(primary, secondary, primaryText, secondaryText);
    }

    internal static UnitSummaries BuildSummaryFormatted(
        UnitDisplayConfigurationsView unitDisplayConfigurationsView,
        Color secondaryBackgroundColor,
        bool highlightLieutenant)
    {
        var accents = GetSummaryAccentColorsForSecondaryBackground(secondaryBackgroundColor);
        var equipmentSummary = CompanyProfileTextService.BuildNamedSummaryFormatted(
            "Equipment",
            unitDisplayConfigurationsView.SelectedUnitCommonEquipment,
            accents.Equipment);
        var skillsSummary = CompanyProfileTextService.BuildNamedSummaryFormatted(
            "Special Skills",
            unitDisplayConfigurationsView.SelectedUnitCommonSkills,
            accents.Skills,
            highlightLieutenantPurple: highlightLieutenant);
        return new UnitSummaries(equipmentSummary, skillsSummary);
    }

    internal static SummaryAccents GetSummaryAccentColorsForSecondaryBackground(Color secondaryBackground)
    {
        return CompanySelectionSharedUtilities.IsLightColor(secondaryBackground)
            ? new SummaryAccents(UnitDisplayConfigurationsView.EquipmentAccentOnLightSecondary, UnitDisplayConfigurationsView.SkillsAccentOnLightSecondary)
            : new SummaryAccents(UnitDisplayConfigurationsView.EquipmentAccentOnDarkSecondary, UnitDisplayConfigurationsView.SkillsAccentOnDarkSecondary);
    }

    // Faction colours by normalized vanilla faction name.
    private static readonly IReadOnlyDictionary<string, FactionTheme> FactionThemes = new Dictionary<string, FactionTheme>
    {
        ["panoceania"] = new(Color.FromArgb("#239ac2"), Color.FromArgb("#006a91")),
        ["yujing"] = new(Color.FromArgb("#ff9000"), Color.FromArgb("#995601")),
        ["ariadna"] = new(Color.FromArgb("#007d27"), Color.FromArgb("#005825")),
        ["haqqislam"] = new(Color.FromArgb("#e6da9b"), Color.FromArgb("#8a835d")),
        ["nomads"] = new(Color.FromArgb("#ce181e"), Color.FromArgb("#7c0e13")),
        ["combinedarmy"] = new(Color.FromArgb("#400b5f"), Color.FromArgb("#260739")),
        ["aleph"] = new(Color.FromArgb("#aea6bb"), Color.FromArgb("#696471")),
        ["tohaa"] = new(Color.FromArgb("#3b3b3b"), Color.FromArgb("#252525")),
        ["nonalignedarmy"] = new(Color.FromArgb("#728868"), Color.FromArgb("#728868")),
        ["o12"] = new(Color.FromArgb("#005470"), Color.FromArgb("#dead33")),
        ["jsa"] = new(Color.FromArgb("#a6112b"), Color.FromArgb("#757575"))
    };

    /// <summary>The faction's colours, or the defaults for factions without a theme.</summary>
    internal static FactionTheme GetFactionTheme(string? factionName, Color defaultPrimary, Color defaultSecondary) =>
        FactionThemes.TryGetValue(CompanySelectionSharedUtilities.NormalizeFactionName(factionName), out var theme)
            ? theme
            : new FactionTheme(defaultPrimary, defaultSecondary);

    internal static string? InferThemeFactionNameFromFactionId(int factionId)
    {
        if (factionId <= 0)
        {
            return null;
        }

        var family = factionId / 100;
        return family switch
        {
            1 => "PanOceania",
            2 => "Yu Jing",
            3 => "Ariadna",
            4 => "Haqqislam",
            5 => "Nomads",
            6 => "Combined Army",
            7 => "Aleph",
            8 => "Tohaa",
            9 => "Non-Aligned Armies",
            10 => "O-12",
            11 => "JSA",
            _ => null
        };
    }
}
