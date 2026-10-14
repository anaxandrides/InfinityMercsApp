namespace InfinityMercsApp.Infrastructure.Models.Database.Army;


// The typed content of a unit (army units and spec-ops units alike), one row per item.
// Every row carries the owning army (FactionId) so a whole army can be loaded or deleted in one query,
// and the owning unit (OwnerKey: the unit's UnitKey or SpecopsUnitKey). Positions keep payload order.

/// <summary>Distinguishes which list a <see cref="UnitEntryRow"/> or option row hangs off.</summary>
public enum UnitEntryOwner
{
    /// <summary>A profile (statline) of a profile group.</summary>
    Profile = 0,

    /// <summary>An option of a profile group.</summary>
    Option = 1,

    /// <summary>An option declared on the unit itself rather than in a profile group.</summary>
    UnitOption = 2
}
