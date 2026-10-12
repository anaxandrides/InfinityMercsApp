using InfinityMercsApp.Infrastructure.Json;

namespace InfinityMercsApp.Views.Common;

internal static partial class CompanySelectionSharedUtilities
{
    internal static bool UnitHasLieutenantOption(string? profileGroupsJson, IReadOnlyDictionary<int, string> skillsLookup) =>
        ArmyJsonReader.Shared.ReadProfileGroups(profileGroupsJson).HasLieutenantOption(skillsLookup);
}
