namespace InfinityMercsApp.Infrastructure.Models.Database.Army.Fireteams;

using SQLite;

/// <summary>A fireteam chart <c>spec</c> entry, e.g. CORE = 1.</summary>
[Table("fireteam_chart_specs")]
public class FireteamChartSpecRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int FactionId { get; set; }

    public int Position { get; set; }

    public string Key { get; set; } = string.Empty;

    public int? Value { get; set; }
}
