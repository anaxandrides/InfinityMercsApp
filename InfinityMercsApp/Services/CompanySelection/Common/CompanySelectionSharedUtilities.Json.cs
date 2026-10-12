using InfinityMercsApp.Infrastructure.Json;

namespace InfinityMercsApp.Views.Common;

internal static partial class CompanySelectionSharedUtilities
{
    internal static IReadOnlyList<int> ParseFactionIds(string? factionsJson) =>
        ArmyJsonReader.Shared.ReadFactionIds(factionsJson);
}
