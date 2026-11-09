using FluentValidation;
using InfinityMercsApp.Domain.Leagues;
using InfinityMercsApp.Domain.Models.Leagues;

namespace InfinityMercsApp.Domain.Validation.Leagues;

/// <summary>
/// A game has two to four different companies, each with objective points a game can score, at most one winner, and
/// a draw only when nobody won. Whether the companies are in the league is checked against the league.
/// </summary>
public sealed class NewLeagueGameValidator : AbstractValidator<NewLeagueGame>
{
    public static NewLeagueGameValidator Instance { get; } = new();

    public NewLeagueGameValidator()
    {
        RuleFor(game => game.Mission).MaximumLength(LeagueLimits.MissionLength)
            .WithMessage($"The mission's name must be at most {LeagueLimits.MissionLength} characters.");
        RuleFor(game => game.PlayedOn).NotEqual(default(DateOnly)).WithMessage("Say when the game was played.");
        RuleFor(game => game.Sides.Count).InclusiveBetween(LeagueLimits.MinSides, LeagueLimits.MaxSides)
            .WithMessage($"A game has {LeagueLimits.MinSides} to {LeagueLimits.MaxSides} companies.");
        RuleFor(game => game.Sides).Must(sides => sides.DistinctBy(side => side.SeasonId).Count() == sides.Count)
            .WithMessage("A company can't play itself.");
        RuleFor(game => game.Sides).Must(sides => sides.Count(side => side.Result == GameResult.Win) <= 1)
            .WithMessage("Only one company can win a game.");
        RuleFor(game => game.Sides).Must(sides => !sides.Any(side => side.Result == GameResult.Win) || !sides.Any(side => side.Result == GameResult.Draw))
            .WithMessage("A game someone won wasn't a draw.");
        RuleForEach(game => game.Sides).ChildRules(side =>
        {
            side.RuleFor(s => s.SeasonId).Must(DocumentIdentifiers.IsValid).WithMessage("A company in the game isn't named by its season.");
            side.RuleFor(s => s.Result).IsInEnum().WithMessage("A company's result must be a win, a draw or a loss.");
            side.RuleFor(s => s.ObjectivePoints).InclusiveBetween(0, LeagueLimits.ObjectivePoints)
                .WithMessage($"Objective points must be from 0 to {LeagueLimits.ObjectivePoints}.");
        });
    }
}
