using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services.ArmyData;

namespace InfinityMercsApp.Views.Common;

internal sealed class CompanyStartSaveRequest<TFaction, TEntry>
    where TFaction : class, ICompanySourceFaction
    where TEntry : class, ICompanyMercsEntry
{
    public required string CompanyName { get; init; }
    public required string CompanyType { get; init; }
    public required IEnumerable<TEntry> MercsCompanyEntries { get; init; }
    public required bool ShowRightSelectionBox { get; init; }
    public required TFaction? LeftSlotFaction { get; init; }
    public required TFaction? RightSlotFaction { get; init; }
    public required IEnumerable<TFaction> Factions { get; init; }
    public required IArmyDataService ArmyDataService { get; init; }
    public required ISpecOpsProvider SpecOpsProvider { get; init; }
    /// <summary>Hosts the captain popup; only <see cref="CompanyStartSaveWorkflow.RunAsync{TFaction, TEntry}"/> needs it.</summary>
    public INavigation? Navigation { get; init; }
    public required bool ShowUnitsInInches { get; init; }
    public required string SelectedStartSeasonPoints { get; init; }
    public required string SeasonPointsCapText { get; init; }
    public Func<int, string?> TryGetMetadataFactionName { get; init; } = _ => null;
    public Func<string, string, string, Task> DisplayAlertAsync { get; init; } = (_, _, _) => Task.CompletedTask;
    public Func<string, Task> NavigateToCompanyViewerAsync { get; init; } = _ => Task.CompletedTask;
}
