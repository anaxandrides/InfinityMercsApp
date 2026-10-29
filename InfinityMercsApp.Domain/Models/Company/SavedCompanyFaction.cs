namespace InfinityMercsApp.Domain.Models.Company;

/// <summary>One of the factions a company recruits from.</summary>
public sealed class SavedCompanyFaction
{
    public int FactionId { get; init; }
    public string FactionName { get; set; } = string.Empty;
}
