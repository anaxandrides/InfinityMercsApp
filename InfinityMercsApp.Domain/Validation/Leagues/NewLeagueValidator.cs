using FluentValidation;
using InfinityMercsApp.Domain.Leagues;
using InfinityMercsApp.Domain.Models.Leagues;

namespace InfinityMercsApp.Domain.Validation.Leagues;

/// <summary>A new league needs a name, its organizer a name to be shown by, and army data named as snapshots are.</summary>
public sealed class NewLeagueValidator : AbstractValidator<NewLeague>
{
    public static NewLeagueValidator Instance { get; } = new();

    public NewLeagueValidator()
    {
        RuleFor(league => league.Name).Must(name => LeagueText.IsName(name, LeagueLimits.NameLength))
            .WithMessage($"The league's name must be 1 to {LeagueLimits.NameLength} characters.");
        RuleFor(league => league.DisplayName).Must(name => LeagueText.IsName(name, LeagueLimits.DisplayNameLength))
            .WithMessage($"Your name must be 1 to {LeagueLimits.DisplayNameLength} characters.");
        RuleFor(league => league.ArmySnapshotId).Must(LeagueText.IsSnapshotId)
            .WithMessage("The army data must be named by its snapshot id.");
    }
}
