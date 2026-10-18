namespace InfinityMercsApp.Services.CompanySelection.Factions;

/// <summary>Which faction slot a selection screen's next faction pick goes to.</summary>
public static class FactionSlots
{
    /// <summary>
    /// Whether putting <paramref name="factionId"/> in <paramref name="slot"/> would repeat the faction already
    /// in the other slot (picking the same faction again for its own slot is fine).
    /// </summary>
    public static bool IsInOtherSlot(bool hasRightSlot, int slot, int? leftFactionId, int? rightFactionId, int factionId)
    {
        if (!hasRightSlot)
        {
            return false;
        }

        return slot == 0
            ? rightFactionId == factionId && leftFactionId != factionId
            : leftFactionId == factionId && rightFactionId != factionId;
    }

    /// <summary>The slot tapped; the left one when there is no right slot.</summary>
    public static int Active(int slot, bool hasRightSlot) => slot == 1 && hasRightSlot ? 1 : 0;

    /// <summary>After filling a slot: the slot still empty when only one is, else the current slot.</summary>
    public static int AfterFilling(bool hasRightSlot, bool leftFilled, bool rightFilled, int currentSlot)
    {
        if (!hasRightSlot)
        {
            return 0;
        }

        if (!leftFilled && rightFilled)
        {
            return 0;
        }

        return leftFilled && !rightFilled ? 1 : currentSlot;
    }
}
