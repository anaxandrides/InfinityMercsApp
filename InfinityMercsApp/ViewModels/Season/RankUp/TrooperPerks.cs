using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Season.Troopers;
using InfinityMercsApp.ViewModels.Company.Viewer;

namespace InfinityMercsApp.ViewModels.Season.RankUp;

/// <summary>The perks a trooper counts as owning when choosing more.</summary>
public static class TrooperPerks
{
    /// <summary>What the trooper's profile already covers, and the perks it has taken.</summary>
    public static IReadOnlySet<string> Owned(CompanyViewerUnitListItem trooper, TrooperProgress progress)
    {
        var fromProfile = CompanyPerkOwnershipResolver.ResolveOwnedPerkNodeIds(
            ProfileText.Lines(trooper.SavedSkills),
            ProfileText.Lines(trooper.SavedEquipment),
            [.. ProfileText.Lines(trooper.SavedRangedWeapons), .. ProfileText.Lines(trooper.SavedCcWeapons)],
            ProfileText.Lines(trooper.SavedCharacteristics),
            includeMechaTrack: progress.IsTag);
        return new HashSet<string>([.. fromProfile, .. progress.Perks], StringComparer.OrdinalIgnoreCase);
    }
}
