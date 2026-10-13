namespace InfinityMercsApp.Views.Common;

internal static partial class CompanySelectionSharedUtilities
{
    internal static bool UnitHasLieutenantOption(UnitProfileGroups profileGroups, IReadOnlyDictionary<int, string> skillsLookup) =>
        profileGroups.HasLieutenantOption(skillsLookup);
}
