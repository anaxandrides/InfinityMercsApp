namespace InfinityMercsApp.Infrastructure.Models.Database.Army.Fireteams;

using SQLite;

[Table("fireteam_chart_team_types")]
public class FireteamChartTeamTypeRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int FactionId { get; set; }

    public int TeamIndex { get; set; }

    public int Position { get; set; }

    public string Type { get; set; } = string.Empty;
}
