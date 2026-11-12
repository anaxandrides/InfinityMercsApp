namespace InfinityMercsApp.Domain.Models.Leagues;

/// <summary>What an invite leads to, shown before joining.</summary>
public class LeagueInvitePreview
{
    public string LeagueId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string OrganizerName { get; set; } = string.Empty;

    public int MemberCount { get; set; }

    /// <summary>Whether the user asking already belongs to the league.</summary>
    public bool IsMember { get; set; }
}
