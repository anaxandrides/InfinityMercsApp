namespace InfinityMercsApp.Infrastructure.Models.Database.Army;

using SQLite;

[Table("fireteam_chart_team_units")]
public class FireteamChartTeamUnitRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int FactionId { get; set; }

    public int TeamIndex { get; set; }

    public int Position { get; set; }

    public string? Name { get; set; }

    public string? Slug { get; set; }

    public string? Comment { get; set; }

    public int? Min { get; set; }

    public int? Max { get; set; }

    public bool MinIsAsterisk { get; set; }

    public bool? Required { get; set; }
}
