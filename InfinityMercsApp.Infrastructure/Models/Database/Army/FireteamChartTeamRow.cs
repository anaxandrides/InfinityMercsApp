namespace InfinityMercsApp.Infrastructure.Models.Database.Army;

using SQLite;

[Table("fireteam_chart_teams")]
public class FireteamChartTeamRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int FactionId { get; set; }

    public int TeamIndex { get; set; }

    public string? Name { get; set; }

    /// <summary>True when the payload sends <c>type</c> as an array (its items are in the team types table).</summary>
    public bool HasTypeList { get; set; }

    /// <summary>The <c>type</c> when the payload sends a single string.</summary>
    public string? TypeText { get; set; }
}
