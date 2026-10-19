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
    /// of the unit the company has had, from any of its factions: a unit keeps its id across factions. Units are
    /// matched by the army unit they come from (the logo source), since the Airborne and Inspiring factions number
    /// their units afresh. The TAG Company's own units are numbered apart, so they only match each other.
    /// </summary>
    /// <param name="unitId">The army unit the picked unit comes from.</param>
    /// <param name="factionId">The faction of that army unit.</param>
    public static RosterRoom For(
        IReadOnlyCollection<ICompanyMercsEntry> entries, int pointsLimit, int currentPoints, string? ava, int? unitId, int? factionId)
    {
        var inCompany = unitId is int id && factionId is int faction
            ? entries.Count(x => ArmyUnitId(x) == id && IsTagCompanys(ArmyFactionId(x)) == IsTagCompanys(faction))
            : 0;
        return new RosterRoom(
            pointsLimit - currentPoints,
            ProfileValues.ParseAvaLimit(ava) is int limit && inCompany >= limit,
            entries.Any(x => x.IsLieutenant));
    }

    private static int ArmyUnitId(ICompanyMercsEntry entry) => entry.LogoSourceUnitId > 0 ? entry.LogoSourceUnitId : entry.SourceUnitId;

    private static int ArmyFactionId(ICompanyMercsEntry entry) =>
        entry.LogoSourceFactionId > 0 ? entry.LogoSourceFactionId : entry.SourceFactionId;

    private static bool IsTagCompanys(int factionId) => factionId == TagGen.TagCompanyFactionId;
}
