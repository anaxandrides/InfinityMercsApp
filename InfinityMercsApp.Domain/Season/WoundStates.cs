namespace InfinityMercsApp.Domain.Season;

public static class WoundStates
{
    /// <summary>
    /// The state after <paramref name="woundsReceived"/> wounds: wounded below the trooper's vitality, knocked
    /// out (or still up with No Wound Incapacitation) at it, a second body with Remote Presence, then dead.
    /// </summary>
    public static WoundState For(int woundsReceived, int startingVitality, bool hasNwi, bool hasRemotePresence)
    {
        if (woundsReceived <= 0)
        {
            return WoundState.Healthy;
        }

        if (woundsReceived < startingVitality)
        {
            return WoundState.Wounded;
        }

        if (woundsReceived == startingVitality)
        {
            return hasNwi ? WoundState.NwiDown : WoundState.KnockedOut;
        }

        return woundsReceived == startingVitality + 1 && hasRemotePresence ? WoundState.KnockedOutBody2 : WoundState.Dead;
    }

    /// <summary>Healthy, wounded or still up with No Wound Incapacitation: the trooper can act and gives orders.</summary>
    public static bool IsConscious(this WoundState state) => state is WoundState.Healthy or WoundState.Wounded or WoundState.NwiDown;
}
