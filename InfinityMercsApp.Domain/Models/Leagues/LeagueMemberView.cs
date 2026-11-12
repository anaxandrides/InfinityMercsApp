namespace InfinityMercsApp.Domain.Models.Leagues;

/// <summary>A league member as the other members see them.</summary>
public class LeagueMemberView
{
    /// <summary>The member's id within this league; it says nothing about their account.</summary>
    public string MemberId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool IsOrganizer { get; set; }

    /// <summary>Whether this is the user asking.</summary>
    public bool IsYou { get; set; }
}
