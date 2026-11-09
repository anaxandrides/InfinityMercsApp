namespace InfinityMercsApp.Api.Leagues;

/// <summary>
/// A league as Cosmos DB keeps it: one document, in its own partition, holding its members, the company seasons
/// entered and the games recorded, so a league is read and changed in one go.
/// </summary>
/// <param name="Id">The document id, which is the league's id.</param>
/// <param name="LeagueId">The league's id, which is also the partition key.</param>
/// <param name="ArmySnapshotId">The army data the league plays with; null for whatever is current.</param>
/// <param name="CurrentSeason">The league season now being played, from 1.</param>
/// <param name="InviteCode">The code that lets people join; null while joining is closed.</param>
/// <param name="MemberUserIds">The members' accounts, kept apart so a user's leagues can be found by query.</param>
public sealed record LeagueDocument(
    string Id,
    string LeagueId,
    string Name,
    string OrganizerUserId,
    string? ArmySnapshotId,
    int CurrentSeason,
    string? InviteCode,
    IReadOnlyList<string> MemberUserIds,
    IReadOnlyList<LeagueMember> Members,
    IReadOnlyList<LeagueEntry> Entries,
    IReadOnlyList<LeagueGame> Games,
    DateTimeOffset CreatedAt)
{
    /// <summary>The user's membership, or null when they don't belong to the league.</summary>
    public LeagueMember? MemberFor(string userId) => Members.FirstOrDefault(member => member.UserId == userId);

    public bool IsOrganizer(string userId) => OrganizerUserId == userId;

    /// <summary>The league with <paramref name="members"/>, keeping <see cref="MemberUserIds"/> in step.</summary>
    public LeagueDocument WithMembers(IReadOnlyList<LeagueMember> members) =>
        this with { Members = members, MemberUserIds = members.Select(member => member.UserId).ToList() };
}
