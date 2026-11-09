namespace InfinityMercsApp.Domain.Models.Leagues;

/// <summary>Joining a league through its invite.</summary>
public class JoinLeague
{
    /// <summary>How the new member is shown to the league's other members.</summary>
    public string DisplayName { get; set; } = string.Empty;
}
