using FluentValidation;
using InfinityMercsApp.Domain.Leagues;
using InfinityMercsApp.Domain.Models.Leagues;

namespace InfinityMercsApp.Domain.Validation.Leagues;

/// <summary>Army data is named by its snapshot id.</summary>
public sealed class LeagueArmyUpdateValidator : AbstractValidator<LeagueArmyUpdate>
{
    public static LeagueArmyUpdateValidator Instance { get; } = new();

    public LeagueArmyUpdateValidator()
    {
        RuleFor(update => update.ArmySnapshotId).Must(LeagueText.IsSnapshotId)
            .WithMessage("The army data must be named by its snapshot id.");
    }
}
