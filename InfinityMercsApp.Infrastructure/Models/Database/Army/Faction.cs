namespace InfinityMercsApp.Infrastructure.Models.Database.Army;

using SQLite;

[Table("armies")]
public class Faction
{
    private const int JsaFactionId = 1101;
    private const int JsaShindenbutaiFactionId = 1102;
    private const int JsaObanFactionId = 1103;

    [PrimaryKey]
    public int FactionId { get; set; }

    public string Version { get; set; } = string.Empty;

    public long ImportedAtUnixSeconds { get; set; }

    /// <summary>The army that provides this army's reinforcements, when it has one.</summary>
    public int? ReinforcementsFactionId { get; set; }

    public bool IsJsaFaction => DetermineIsJsaFaction(FactionId);

    private static bool DetermineIsJsaFaction(int factionId)
    {
        return factionId is JsaFactionId or JsaShindenbutaiFactionId or JsaObanFactionId;
    }
}
