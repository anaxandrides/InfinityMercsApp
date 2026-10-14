namespace InfinityMercsApp.Infrastructure.Data;

/// <summary>
/// Reads and writes the typed content of armies (filters, relations, fireteam chart), their units and their
/// spec-ops choices. Content is assembled once per army and cached; replacing an army's content drops its
/// cache entry.
/// </summary>
public interface IArmyContentStore
{
    ArmyContent GetArmyContent(int factionId);

    /// <summary>The content of a unit or spec-ops unit of the army; empty when the unit has none stored.</summary>
    UnitContent GetUnitContent(int factionId, string ownerKey);

    /// <summary>The extras and granted items of a spec-ops choice; empty when it has none stored.</summary>
    SpecopsChoiceLinks GetSpecopsLinks(int factionId, string ownerKey);

    /// <summary>
    /// Deletes all stored content of the army and inserts the given rows. Run it inside the caller's
    /// transaction together with the army's own rows.
    /// </summary>
    void ReplaceFactionContent(int factionId, UnitContentRows units, ArmyContentRows army, SpecopsLinkRows specopsLinks);
}
