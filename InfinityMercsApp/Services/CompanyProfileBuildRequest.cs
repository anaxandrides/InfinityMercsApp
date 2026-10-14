using InfinityMercsApp.Views.Common;

namespace InfinityMercsApp.Services;

internal sealed class CompanyProfileBuildRequest
{
    public required UnitProfileGroups ProfileGroups { get; init; }
    public required bool ForceLieutenant { get; init; }
    public required bool ShowTacticalAwarenessIcon { get; init; }
    public required IReadOnlyDictionary<int, string> WeaponsLookup { get; init; }
    public required IReadOnlyDictionary<int, string> EquipLookup { get; init; }
    public required IReadOnlyDictionary<int, string> SkillsLookup { get; init; }
    public required IReadOnlyDictionary<int, string> CharsLookup { get; init; }
    public required IReadOnlyDictionary<int, string> PeripheralLookup { get; init; }
    public required ProfileEntryNames EntryNames { get; init; }
    public required Func<ProfileGroup, ProfileOption, string, bool> ShouldIncludeOption { get; init; }
    public required Func<ProfileGroup, ProfileOption, string> ReadAdjustedOptionCost { get; init; }
    public required Func<string, UnitProfile, PeripheralMercsCompanyStats?> BuildPeripheralStatBlock { get; init; }
    public required Func<int?, int?, string> FormatMoveValue { get; init; }
}
