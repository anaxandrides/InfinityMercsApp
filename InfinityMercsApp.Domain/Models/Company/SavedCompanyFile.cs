using System.Text.Json.Serialization;
using InfinityMercsApp.Domain.Fireteams;

namespace InfinityMercsApp.Domain.Models.Company;

/// <summary>
/// A saved company, as every company type writes it and every page reads it: its type, founding points, factions
/// and troopers. A file without its name, type, identifier, factions or troopers isn't read at all, so a save from an
/// app that left them out can't wipe them.
/// </summary>
public sealed class SavedCompanyFile
{
    [JsonRequired]
    public string CompanyName { get; init; } = string.Empty;
    [JsonRequired]
    public string CompanyType { get; init; } = string.Empty;
    [JsonRequired]
    public string CompanyIdentifier { get; init; } = string.Empty;
    public int CompanyIndex { get; init; }
    public string CreatedUtc { get; set; } = string.Empty;
    public int StartSeasonPoints { get; init; }
    public int PointsLimit { get; init; }
    /// <summary>
    /// The CR spent recruiting the company when it was founded, which sets its starting funds. Later recruits are
    /// charged to the season as Additional Recruitment instead, so this never changes.
    /// </summary>
    public int CurrentPoints { get; init; }
    /// <summary>A Cohesive company's core fireteam, chosen when it was founded; empty for any other company.</summary>
    public string CoreFireteam { get; set; } = string.Empty;
    /// <summary>
    /// The places in a Cohesive company's core fireteam, wildcards included, as they stood when it was founded; empty
    /// for any other company. Who is Regular is worked out from them (see <c>CoreFireteamOrders</c>).
    /// </summary>
    public List<FireteamSlot> CoreFireteamSlots { get; set; } = [];

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SavedImprovedCaptainStats? ImprovedCaptainStats { get; init; }
    [JsonRequired]
    public List<SavedCompanyFaction> SourceFactions { get; init; } = [];
    [JsonRequired]
    public List<SavedCompanyEntry> Entries { get; init; } = [];
}
