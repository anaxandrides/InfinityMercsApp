using InfinityMercsApp.Services.CompanySelection.Common;
using InfinityMercsApp.Services.CompanySelection.StartCompany;

namespace InfinityMercsApp.Services.CompanySelection.Profiles;

/// <summary>What room the company has for another profile of the picked unit.</summary>
/// <param name="AvaReached">The company already has as many of the unit as its AVA allows.</param>
/// <param name="HasLieutenant">The company already has its lieutenant.</param>
public sealed record RosterRoom(int PointsRemaining, bool AvaReached, bool HasLieutenant)
{
    /// <summary>The room <paramref name="entries"/> leave for the unit (AVA as its statline shows it).</summary>
    public static RosterRoom For(
        IReadOnlyCollection<ICompanyMercsEntry> entries, int pointsLimit, int currentPoints, string? ava, int? unitId, int? factionId)
    {
        var inCompany = unitId is int id && factionId is int faction
            ? entries.Count(x => x.SourceUnitId == id && x.SourceFactionId == faction)
            : 0;
        return new RosterRoom(
            pointsLimit - currentPoints,
            CompanySelectionSharedUtilities.ParseAvaLimit(ava) is int limit && inCompany >= limit,
            entries.Any(x => x.IsLieutenant));
    }
}
