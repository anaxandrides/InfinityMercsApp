namespace InfinityMercsApp.Services.CompanySelection.Models.Saved;

/// <summary>One of the factions a company recruits from.</summary>
public sealed class SavedCompanyFaction
{
    public int FactionId { get; init; }
    public string FactionName { get; init; } = string.Empty;
}
