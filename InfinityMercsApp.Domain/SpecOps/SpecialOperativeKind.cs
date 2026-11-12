namespace InfinityMercsApp.Domain.SpecOps;

/// <summary>Which kind of unit with an upgrade table a unit is.</summary>
public enum SpecialOperativeKind
{
    /// <summary>An ordinary unit, recruited like any other.</summary>
    None,

    /// <summary>A Spec-Ops: one trooper who can only be a company's captain.</summary>
    SpecOps,

    /// <summary>A Team-Ops entry: the troopers of a Team-Ops Company.</summary>
    TeamOps
}
