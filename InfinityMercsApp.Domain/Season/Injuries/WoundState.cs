namespace InfinityMercsApp.Domain.Season.Injuries;

/// <summary>How badly a trooper is hurt during a game.</summary>
public enum WoundState
{
    Healthy,
    Wounded,

    /// <summary>Down to its last wound but still acting thanks to No Wound Incapacitation.</summary>
    NwiDown,
    KnockedOut,

    /// <summary>A Remote Presence trooper knocked out on its second body.</summary>
    KnockedOutBody2,
    Dead
}
