using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Company;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.CompanySelection.SpecOps;
using InfinityMercsApp.Services.Dialogs;

namespace InfinityMercsApp.Services.CompanySelection.Captain;

/// <summary>Shows the popups that upgrade a new company's captain and a TAG company's TAGs with Spec-Ops.</summary>
public sealed class CaptainSetup(ICaptainConfigurationDialog dialog, CaptainOptionCatalog options, IArmyDataService armyData)
{
    /// <summary>
    /// The captain popup, with the Spec-Ops options of the source faction's vanilla parent (or the faction itself
    /// when the parent offers none). A Spec-Ops captain uses the Spec-Ops stat chart. A Proxy Pack's Lone Wolf has
    /// 10 more Spec-Ops XP to spend, whatever it costs.
    /// </summary>
    /// <returns>The chosen upgrades, or null when the player backed out.</returns>
    public async Task<SavedImprovedCaptainStats?> ConfigureCaptainAsync(
        CaptainUnitPopupInfo unit,
        CaptainFactions factions,
        bool isSpecOps,
        bool isLoneWolf,
        bool showUnitsInInches,
        CancellationToken cancellationToken = default)
    {
        var sourceFactionId = SourceFactionId(factions);
        var optionFactionId = OptionFactionId(sourceFactionId, ParentOf(sourceFactionId, factions));
        var choices = options.Load(optionFactionId, showUnitsInInches, cancellationToken);
        if (choices.IsEmpty && optionFactionId != sourceFactionId)
        {
            choices = options.Load(sourceFactionId, showUnitsInInches, cancellationToken);
            optionFactionId = sourceFactionId;
        }

        return await dialog.ShowAsync(new CaptainUpgradePopupContext
        {
            Unit = unit,
            OptionFactionId = optionFactionId,
            OptionFactionName = OptionFactionName(sourceFactionId, optionFactionId, factions),
            WeaponOptions = choices.Weapons,
            SkillOptions = choices.Skills,
            EquipmentOptions = choices.Equipment,
            StatChart = isSpecOps ? CaptainStatChart.SpecOps : CaptainStatChart.Mercenaries,
            PopupTitle = isLoneWolf ? "Lone Wolf Configuration" : "Captain Configuration",
            DefaultUnitCustomName = isLoneWolf ? "Lone Wolf" : "Captain",
            BaseExperienceOverride = isLoneWolf
                ? CaptainUpgradeRules.StartingExperience(unit.Cost) + ProxyPack.ExtraSpecOpsXp
                : null
        });
    }

    /// <summary>
    /// A TAG's Spec-Ops popup: the options of its source faction and every parent above it (and the vanilla
    /// faction of a reinforcement), without those a TAG can't take.
    /// </summary>
    /// <returns>The chosen upgrades, or null when the player backed out.</returns>
    public async Task<SavedImprovedCaptainStats?> ConfigureTagSpecOpsAsync(
        CaptainUnitPopupInfo unit,
        CaptainFactions factions,
        bool isLieutenant,
        int baseExperience,
        bool showUnitsInInches,
        CancellationToken cancellationToken = default)
    {
        var sourceFactionId = TagSpecOps.SourceFactionId(factions);
        var candidates = FactionChain(sourceFactionId, factions);
        if (TagSpecOps.VanillaOfReinforcement(sourceFactionId) is int vanilla && !candidates.Contains(vanilla))
        {
            candidates.Add(vanilla);
        }

        var weapons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var skills = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var equipment = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var optionFactionId = sourceFactionId;
        foreach (var candidate in candidates)
        {
            var choices = options.Load(candidate, showUnitsInInches, cancellationToken);
            if (!choices.IsEmpty && weapons.Count == 0 && skills.Count == 0 && equipment.Count == 0)
            {
                optionFactionId = candidate;
            }

            weapons.UnionWith(choices.Weapons);
            skills.UnionWith(choices.Skills);
            equipment.UnionWith(choices.Equipment);
        }

        var allowed = TagSpecOps.WithoutDisallowed(new CaptainUpgradeOptionSet
        {
            Weapons = weapons.ToList(),
            Skills = skills.ToList(),
            Equipment = equipment.ToList()
        });

        return await dialog.ShowAsync(new CaptainUpgradePopupContext
        {
            Unit = unit,
            OptionFactionId = optionFactionId,
            OptionFactionName = OptionFactionName(sourceFactionId, optionFactionId, factions),
            WeaponOptions = allowed.Weapons,
            SkillOptions = allowed.Skills,
            EquipmentOptions = allowed.Equipment,
            PopupTitle = isLieutenant ? "TAG Lieutenant Spec Ops" : "TAG Spec Ops",
            ConfirmButtonText = "Finalize Tag",
            DefaultUnitCustomName = isLieutenant ? "Lieutenant" : "TAG",
            BaseExperienceOverride = Math.Max(0, baseExperience),
            StatChart = CaptainStatChart.SpecOps,
            LockedStats = TagRestrictions.LockedStats
        });
    }

    /// <summary>The trooper's own faction, else the company's first source faction.</summary>
    internal static int SourceFactionId(CaptainFactions factions) =>
        factions.TrooperFactionId > 0 ? factions.TrooperFactionId : factions.FirstSourceFactionId ?? factions.TrooperFactionId;

    // Spec-Ops options come from the vanilla parent of a sectorial.
    private static int OptionFactionId(int sourceFactionId, int? parentId) =>
        sourceFactionId > 0 && parentId.GetValueOrDefault() > 0 ? parentId.GetValueOrDefault() : sourceFactionId;

    private int? ParentOf(int factionId, CaptainFactions factions) =>
        factions.Known.FirstOrDefault(faction => faction.Id == factionId)?.ParentId
        ?? armyData.GetMetadataFactionById(factionId)?.ParentId;

    // The faction and each parent above it, stopping at a loop or after 16 steps.
    private List<int> FactionChain(int factionId, CaptainFactions factions)
    {
        var chain = new List<int>();
        var current = factionId;
        while (current > 0 && !chain.Contains(current) && chain.Count < 16)
        {
            chain.Add(current);
            if (ParentOf(current, factions) is not int parent || parent <= 0 || parent == current)
            {
                break;
            }

            current = parent;
        }

        return chain;
    }

    // The screen's name for the source or option faction, else the metadata's, else "Faction N".
    private string OptionFactionName(int sourceFactionId, int optionFactionId, CaptainFactions factions)
    {
        string? Known(int id) => id > 0 ? factions.Known.FirstOrDefault(faction => faction.Id == id)?.Name : null;
        string? Metadata(int id) => id > 0 ? armyData.GetMetadataFactionById(id)?.Name : null;

        return new List<string?> { Known(sourceFactionId), Known(optionFactionId), Metadata(sourceFactionId), Metadata(optionFactionId) }
                   .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name))
               ?? (optionFactionId > 0 ? $"Faction {optionFactionId}"
                   : sourceFactionId > 0 ? $"Faction {sourceFactionId}"
                   : "Faction");
    }
}
