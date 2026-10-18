using InfinityMercsApp.Domain.Lieutenants;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Services.CompanyProfiles;
using InfinityMercsApp.Services.CompanySelection.Captain;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.CompanySelection.StartCompany;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Company.Selection.Items;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;
using TagGen = InfinityMercsApp.Infrastructure.Providers.CompanyFactions.TagCompanyFactionGenerator;

namespace InfinityMercsApp.ViewModels.Company.Selection;

/// <summary>
/// A TAG company: a sectorial in the left slot and the TAG company's own units in the fixed right slot.
/// Every TAG added gets its Spec Ops upgrades chosen straight away.
/// </summary>
public sealed class TagCompanySelectionViewModel(CompanySelectionServices services, ArmySourceSelectionMode mode)
    : StandardCompanySelectionViewModel(services, mode)
{
    public const string TagCompanyName = CompanyFounder.TagCompanyType;
    public const string TagCompanyLogoPath = "SVGCache/MercsIcons/noun-battle-mech-1731140.svg";

    /// <summary>A TAG's starting experience, spent in its Spec Ops popup.</summary>
    public const int TagExperience = 20;

    public override string Heading => "Choose your sectorial";

    public override string CompanyTypeLabel => TagCompanyName;

    protected override int? FixedFactionSlot => 0;

    protected override bool CanPickRightSlot => false;

    /// <summary>The TAG company itself goes in the right slot.</summary>
    protected override void OnFirstAppearing()
    {
        if (ShowRightSlot)
        {
            RightSlotFaction = new ArmyFactionSelectionItem
            {
                Id = TagGen.TagCompanyFactionId,
                ParentId = TagGen.TagCompanyFactionId,
                Name = TagCompanyName,
                PackagedLogoPath = TagCompanyLogoPath
            };
            RightSlotLogoSource = TagCompanyLogoPath;
        }
    }

    /// <summary>The TAG company's own units are all listed, not only its Mercs ones.</summary>
    protected override IReadOnlyList<InfinityMercsApp.Domain.Models.Army.Resume> ResumesFor(int factionId, CancellationToken cancellationToken) =>
        factionId == TagGen.TagCompanyFactionId
            ? Services.ArmyData.GetResumeByFaction(factionId, cancellationToken)
            : base.ResumesFor(factionId, cancellationToken);

    protected override bool IsHighlightedUnit(int factionId, int unitId) => factionId == TagGen.TagCompanyFactionId && unitId == 1;

    /// <summary>A TAG trooper (not the lieutenant) gets its Spec Ops chosen; backing out removes it again.</summary>
    protected override async Task AfterEntryAddedAsync(ArmyUnitSelectionItem unit, ViewerProfileItem profile, MercsCompanyEntry entry)
    {
        if (profile.IsLieutenant ||
            !(UnitTypeCodes.IsTag(entry.UnitTypeCode) || UnitTypeCodes.IsTag(CompanyStartSharedState.ExtractUnitTypeCode(unit.Subtitle))))
        {
            return;
        }

        SavedImprovedCaptainStats? stats;
        try
        {
            stats = await ConfigureSpecOpsAsync(entry);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"The TAG Spec Ops popup failed: {ex}");
            return;
        }

        if (stats is null)
        {
            RemoveMercsCompanyEntryCommand.Execute(entry);
            return;
        }

        ReplaceMercsCompanyEntry(entry, TagFinalizedEntry.From(entry, stats));
    }

    private Task<SavedImprovedCaptainStats?> ConfigureSpecOpsAsync(MercsCompanyEntry entry)
    {
        var sourceFactionId = TagWorkflowFactionId(entry.SourceFactionId);
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
            Factions.ToList(),
            sourceFactionId > 0 ? sourceFactionId : null);
        return Services.Captains.ConfigureTagSpecOpsAsync(unit, factions, entry.IsLieutenant, TagExperience, Detail.ShowUnitsInInches);
    }

    // The chosen sectorial; the TAG company itself has no Spec Ops options of its own.
    private int TagWorkflowFactionId(int fallbackFactionId)
    {
        var chosen = new List<int> { LeftSlotFaction?.Id ?? 0, RightSlotFaction?.Id ?? 0 }
            .FirstOrDefault(id => id > 0 && id != TagGen.TagCompanyFactionId);
        if (chosen > 0)
        {
            return chosen;
        }

        return fallbackFactionId != TagGen.TagCompanyFactionId ? fallbackFactionId : 0;
    }
}
