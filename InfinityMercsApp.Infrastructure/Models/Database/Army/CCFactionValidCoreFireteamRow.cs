namespace InfinityMercsApp.Infrastructure.Models.Database.Army;

using SQLite;

/// <summary>A CORE fireteam name found valid for a cohesive-company faction filter.</summary>
[Table("cc_faction_valid_core_fireteams")]
public class CCFactionValidCoreFireteamRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public string CacheKey { get; set; } = string.Empty;

    public int Position { get; set; }

    public string Name { get; set; } = string.Empty;
}
