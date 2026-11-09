using FluentValidation;
using InfinityMercsApp.Domain.Leagues;
using InfinityMercsApp.Domain.Models.Leagues;

namespace InfinityMercsApp.Domain.Validation.Leagues;

/// <summary>A new member needs a name to be shown by.</summary>
public sealed class JoinLeagueValidator : AbstractValidator<JoinLeague>
{
    public static JoinLeagueValidator Instance { get; } = new();

    public JoinLeagueValidator()
    {
        RuleFor(join => join.DisplayName).Must(name => LeagueText.IsName(name, LeagueLimits.DisplayNameLength))
            .WithMessage($"Your name must be 1 to {LeagueLimits.DisplayNameLength} characters.");
    }
}
