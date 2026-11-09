using InfinityMercsApp.Domain.Models.Leagues;

namespace InfinityMercsApp.Presentation.Leagues;

/// <summary>A member as a league's page lists them.</summary>
/// <param name="Note">"organizer", "you", both, or empty.</param>
public sealed record LeagueMemberRow(string Name, string Note)
{
    public static LeagueMemberRow For(LeagueMemberView member) => new(
        member.DisplayName,
        string.Join(", ", new List<string> { member.IsOrganizer ? "organizer" : string.Empty, member.IsYou ? "you" : string.Empty }.Where(note => note.Length > 0)));
}
