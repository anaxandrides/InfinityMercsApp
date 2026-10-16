namespace InfinityMercsApp.Domain.Captain;

/// <summary>The ways a captain build can break the rules.</summary>
public enum CaptainBuildProblemKind
{
    /// <summary>More experience is spent than the captain has.</summary>
    Overspent,

    /// <summary>A stat has an upgrade tier the rules don't offer it, such as one past its cap.</summary>
    StatTierNotOffered,

    /// <summary>A slot holds a weapon, skill or equipment that isn't on offer.</summary>
    ChoiceNotOffered,

    /// <summary>The same weapon, skill or equipment is chosen in more than one slot.</summary>
    DuplicateChoice
}
