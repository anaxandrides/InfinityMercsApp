namespace InfinityMercsApp.Domain.Season.Services;

/// <summary>The one-off services a company can pay for between contracts.</summary>
public enum SeasonService
{
    /// <summary>Writes off one injury.</summary>
    AdvancedTherapy,

    /// <summary>Advanced Therapy at double cost: writes off every injury.</summary>
    FullTherapy,

    /// <summary>Anger Management: the trooper loses Frenzy.</summary>
    CalmFrenzy,

    /// <summary>Anger Management at double cost: the trooper loses Impetuous, and No or Partial Cover with it.</summary>
    CalmImpetuous,

    /// <summary>Swaps one perk for another of the same tier; rank 1 or above.</summary>
    ForcedRetraining,

    /// <summary>The trooper becomes regular.</summary>
    RegularTraining
}
