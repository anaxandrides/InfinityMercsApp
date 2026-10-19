using InfinityMercsApp.Domain.Lieutenants;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanyProfiles;
using InfinityMercsApp.Services.CompanySelection.Captain;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.CompanySelection.StartCompany;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Company.Selection.Items;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;
using TagGen = InfinityMercsApp.Infrastructure.Providers.CompanyFactions.TagCompanyFactionGenerator;

namespace InfinityMercsApp.ViewModels.Company.Selection.Rules;

/// <summary>
/// A TAG company: a sectorial in the left slot and the TAG company's own units in the fixed right slot.
/// Every TAG added gets its Spec Ops upgrades chosen straight away.
/// </summary>
public sealed class TagCompanyRules(ArmySourceSelectionMode mode) : StandardCompanyRules(mode)
{
    public const string TagCompanyName = CompanyFounder.TagCompanyType;
    public const string TagCompanyLogoPath = "SVGCache/MercsIcons/noun-battle-mech-1731140.svg";

    /// <summary>A TAG's starting experience, spent in its Spec Ops popup.</summary>
    public const int TagExperience = 20;

    public override string Heading => "Choose your sectorial";

    public override string CompanyTypeLabel => TagCompanyName;

    public override int? FixedFactionSlot => 0;

    public override bool CanPickRightSlot => false;

    /// <summary>The TAG company itself goes in the right slot.</summary>
    public override void Prepare(CompanySelectionViewModel screen)
    {
        if (HasRightSlot)
        {
            screen.Slots.Set(1, new ArmyFactionSelectionItem
            {
                Id = TagGen.TagCompanyFactionId,
                ParentId = TagGen.TagCompanyFactionId,
                Name = TagCompanyName,
                PackagedLogoPath = TagCompanyLogoPath
            });
        }
    }

    /// <summary>The TAG company's own units are all listed, not only its Mercs ones.</summary>
    public override IReadOnlyList<ArmyResumeRecord> ResumesFor(IArmyDataService armyData, int factionId, CancellationToken cancellationToken) =>
        factionId == TagGen.TagCompanyFactionId
            ? armyData.GetResumeByFaction(factionId, cancellationToken)
            : base.ResumesFor(armyData, factionId, cancellationToken);

    public override bool IsHighlightedUnit(int factionId, int unitId) => factionId == TagGen.TagCompanyFactionId && unitId == 1;

    /// <summary>A TAG trooper (not the lieutenant) gets its Spec Ops chosen; backing out removes it again.</summary>
    public override async Task AfterEntryAddedAsync(
        CompanySelectionViewModel screen, ArmyUnitSelectionItem unit, ViewerProfileItem profile, MercsCompanyEntry entry)
    {
        if (profile.IsLieutenant ||
            !(UnitTypeCodes.IsTag(entry.UnitTypeCode) || UnitTypeCodes.IsTag(UnitTypeCodes.FromSubtitle(unit.Subtitle))))
        {
            return;
        }

        SavedImprovedCaptainStats? stats;
        try
        {
            stats = await ConfigureSpecOpsAsync(screen, entry);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"The TAG Spec Ops popup failed: {ex}");
            return;
        }

        if (stats is null)
        {
            screen.RemoveMercsCompanyEntryCommand.Execute(entry);
            return;
        }

        screen.ReplaceEntry(entry, TagFinalizedEntry.From(entry, stats));
    }

    private static Task<SavedImprovedCaptainStats?> ConfigureSpecOpsAsync(CompanySelectionViewModel screen, MercsCompanyEntry entry)
    {
        var sourceFactionId = TagWorkflowFactionId(screen.Slots, entry.SourceFactionId);
        var skillLines = CompanyProfileText.SplitDisplayLine(entry.SavedSkills);
        if (entry.IsLieutenant)
        {
            LieutenantRules.AddSkillIfMissing(skillLines);
        }

        var unit = new CaptainUnitPopupInfo
        {
            Name = entry.Name,
            Cost = entry.CostValue,
            Statline = entry.Subtitle ?? "-",
            RangedWeapons = entry.SavedRangedWeapons,
            CcWeapons = entry.SavedCcWeapons,
            Skills = skillLines.Count == 0 ? "-" : string.Join(Environment.NewLine, skillLines),
            Equipment = entry.SavedEquipment,
            CachedLogoPath = entry.CachedLogoPath,
            PackagedLogoPath = entry.PackagedLogoPath
        };
        var factions = new CaptainFactions(
            sourceFactionId > 0 ? sourceFactionId : entry.SourceFactionId,
            sourceFactionId > 0 ? sourceFactionId : null,
            screen.Slots.Factions.ToList(),
            sourceFactionId > 0 ? sourceFactionId : null);
        return screen.Services.Captains.ConfigureTagSpecOpsAsync(unit, factions, entry.IsLieutenant, TagExperience, screen.Preview.Detail.ShowUnitsInInches);
    }

    // The chosen sectorial; the TAG company itself has no Spec Ops options of its own.
    private static int TagWorkflowFactionId(FactionSlotsViewModel slots, int fallbackFactionId)
    {
        var chosen = new List<int> { slots.Left?.Id ?? 0, slots.Right?.Id ?? 0 }
            .FirstOrDefault(id => id > 0 && id != TagGen.TagCompanyFactionId);
        if (chosen > 0)
        {
            return chosen;
        }

        return fallbackFactionId != TagGen.TagCompanyFactionId ? fallbackFactionId : 0;
    }
}
