using System.Text.Json.Serialization;

namespace InfinityMercsApp.Services.CompanySelection.Models.Saved;

public abstract class CompanySavedCompanyFileBase<TFaction, TEntry>
    where TFaction : CompanySavedCompanyFactionBase, new()
    where TEntry : CompanySavedCompanyEntryBase, new()
{
    public string CompanyName { get; init; } = string.Empty;
    public string CompanyType { get; init; } = string.Empty;
    public string CompanyIdentifier { get; init; } = string.Empty;
    public int CompanyIndex { get; init; }
    public string CreatedUtc { get; init; } = string.Empty;
    public int StartSeasonPoints { get; init; }
    public int PointsLimit { get; init; }
    /// <summary>
    /// The CR spent recruiting the company when it was founded, which sets its starting funds. Later recruits are
    /// charged to the season as Additional Recruitment instead, so this never changes.
    /// </summary>
    public int CurrentPoints { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SavedImprovedCaptainStats? ImprovedCaptainStats { get; init; }
    public List<TFaction> SourceFactions { get; init; } = [];
    public List<TEntry> Entries { get; init; } = [];
}
