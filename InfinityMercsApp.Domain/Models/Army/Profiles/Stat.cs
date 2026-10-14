namespace InfinityMercsApp.Domain.Models.Army.Profiles;

/// <summary>A trooper attribute shown on a statline ("MOV 10-5 | CC 14 | BS 12 | ...").</summary>
public enum Stat
{
    Mov,
    Cc,
    Bs,
    Ph,
    Wip,
    Arm,
    Bts,

    /// <summary>Wounds of a living trooper.</summary>
    Vita,

    /// <summary>Structure of a TAG, remote or peripheral; takes VITA's place on the statline.</summary>
    Str,

    /// <summary>The viewer's wounds column, read as VITA.</summary>
    W,

    /// <summary>Silhouette.</summary>
    S,
    Ava
}
