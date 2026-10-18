namespace InfinityMercsApp.Services.CompanySelection.StartCompany;

/// <summary>A company as the selection screen has it when the player starts it.</summary>
public sealed class CompanyFoundingRequest<TFaction, TEntry>
    where TFaction : class, ICompanySourceFaction
    where TEntry : class, ICompanyMercsEntry
{
    public required string CompanyName { get; init; }
    public required string CompanyType { get; init; }
    public required IEnumerable<TEntry> MercsCompanyEntries { get; init; }
    public required bool ShowRightSelectionBox { get; init; }
    public required TFaction? LeftSlotFaction { get; init; }
    public required TFaction? RightSlotFaction { get; init; }

    /// <summary>The factions the screen offers, for the captain popup's faction names and parents.</summary>
    public required IEnumerable<TFaction> Factions { get; init; }

    public required bool ShowUnitsInInches { get; init; }
    public required string SelectedStartSeasonPoints { get; init; }
    public required string SeasonPointsCapText { get; init; }
}
