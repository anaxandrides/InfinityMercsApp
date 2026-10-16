namespace InfinityMercsApp.Domain.Captain;

/// <summary>Which chart a captain's stat upgrades come from.</summary>
public enum CaptainStatChart
{
    /// <summary>The Infinity Spec-Ops chart, for a Spec Ops captain.</summary>
    SpecOps,

    /// <summary>The Mercenaries chart, relative to the profile, for a captain that is not a Spec Ops unit.</summary>
    Mercenaries
}
