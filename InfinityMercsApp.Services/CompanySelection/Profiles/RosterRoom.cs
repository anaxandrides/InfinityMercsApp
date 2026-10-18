using InfinityMercsApp.Services.CompanySelection.StartCompany;
using TagGen = InfinityMercsApp.Infrastructure.Providers.CompanyFactions.TagCompanyFactionGenerator;

namespace InfinityMercsApp.Services.CompanySelection.Profiles;

/// <summary>What room the company has for another profile of the picked unit.</summary>
/// <param name="AvaReached">The company already has as many of the unit as its AVA allows.</param>
/// <param name="HasLieutenant">The company already has its lieutenant.</param>
public sealed record RosterRoom(int PointsRemaining, bool AvaReached, bool HasLieutenant)
{
    /// <summary>
    /// The room <paramref name="entries"/> leave for the unit (AVA as its statline shows it). AVA counts every one
    /// of the unit the company has had, from either of its factions: a unit keeps its id across factions. The TAG
    /// Company's own units are numbered apart, so they only match each other.
    /// </summary>
    public static RosterRoom For(
        IReadOnlyCollection<ICompanyMercsEntry> entries, int pointsLimit, int currentPoints, string? ava, int? unitId, int? factionId)
    {
        var inCompany = unitId is int id && factionId is int faction
            ? entries.Count(x => x.SourceUnitId == id && IsTagCompanys(x.SourceFactionId) == IsTagCompanys(faction))
            : 0;
        return new RosterRoom(
            pointsLimit - currentPoints,
            ProfileValues.ParseAvaLimit(ava) is int limit && inCompany >= limit,
            entries.Any(x => x.IsLieutenant));
    }

    private static bool IsTagCompanys(int factionId) => factionId == TagGen.TagCompanyFactionId;
}
