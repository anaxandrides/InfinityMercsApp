using InfinityMercsApp.Domain.Models.Leagues;

namespace InfinityMercsApp.Presentation.Leagues;

/// <summary>A company entered in the league, offered as a side when recording a game.</summary>
public sealed record LeagueEntryChoice(string SeasonId, string Text)
{
    public static LeagueEntryChoice For(LeagueEntryView entry, LeagueView league) =>
        new(entry.SeasonId, entry.IsYours ? entry.CompanyName : $"{entry.CompanyName} ({LeagueNames.PlayerOf(league, entry.MemberId)})");

    public override string ToString() => Text;
}
