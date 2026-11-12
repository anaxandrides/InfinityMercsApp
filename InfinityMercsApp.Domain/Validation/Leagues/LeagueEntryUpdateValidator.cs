using FluentValidation;
using InfinityMercsApp.Domain.Leagues;
using InfinityMercsApp.Domain.Models.Leagues;

namespace InfinityMercsApp.Domain.Validation.Leagues;

/// <summary>Renown is never negative, and never absurdly large.</summary>
public sealed class LeagueEntryUpdateValidator : AbstractValidator<LeagueEntryUpdate>
{
    public static LeagueEntryUpdateValidator Instance { get; } = new();

    public LeagueEntryUpdateValidator()
    {
        RuleFor(entry => entry.Renown).InclusiveBetween(0, LeagueLimits.Renown)
            .WithMessage($"Renown must be from 0 to {LeagueLimits.Renown}.");
    }
}
