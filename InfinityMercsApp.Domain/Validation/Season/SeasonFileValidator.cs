using FluentValidation;
using InfinityMercsApp.Domain.Models.Season;

namespace InfinityMercsApp.Domain.Validation.Season;

/// <summary>
/// What a season must be before it is kept online: its own id, its company's name and identifier, and records that
/// make sense on their own. Whether its troopers are in the company is checked against the company, not here.
/// </summary>
public sealed class SeasonFileValidator : AbstractValidator<SeasonFile>
{
    public static SeasonFileValidator Instance { get; } = new();

    public SeasonFileValidator()
    {
        RuleFor(season => season.SeasonIdentifier).Must(DocumentIdentifiers.IsValid)
            .WithMessage("The season's identifier must be 1 to 64 letters, digits, '-' or '_'.");
        RuleFor(season => season.CompanyName).NotEmpty().WithMessage("The season has no company name.");
        RuleFor(season => season.CompanyIdentifier).Must(DocumentIdentifiers.IsValid)
            .WithMessage("The season's company identifier must be 1 to 64 letters, digits, '-' or '_'.");

        RuleFor(season => season.Rounds).Must(rounds => rounds.DistinctBy(round => round.RoundIndex).Count() == rounds.Count)
            .WithMessage("Two of the season's contracts share a number.");
        RuleForEach(season => season.Rounds).ChildRules(round =>
            round.RuleForEach(r => r.MissionResults.UnitResults).ChildRules(result =>
            {
                result.RuleFor(r => r.XpGained).GreaterThanOrEqualTo(0).WithMessage(r => $"{r.UnitName} gained negative XP.");
                result.RuleFor(r => r.EntryIndex).GreaterThanOrEqualTo(0).WithMessage(r => $"{r.UnitName} has a negative entry index.");
            }));

        RuleFor(season => season.Troopers.Keys).Must(keys => keys.All(key => key >= 0))
            .WithMessage("A trooper's season record has a negative entry index.");
        RuleFor(season => season.UnitGear.Keys).Must(keys => keys.All(key => key >= 0))
            .WithMessage("A trooper's gear has a negative entry index.");

        RuleFor(season => season.Retired).Must(retired => retired.DistinctBy(trooper => trooper.EntryIndex).Count() == retired.Count)
            .WithMessage("A trooper retired twice.");
        RuleForEach(season => season.Retired).ChildRules(trooper =>
        {
            trooper.RuleFor(t => t.EntryIndex).GreaterThanOrEqualTo(0).WithMessage(t => $"{t.Name} has a negative entry index.");
            trooper.RuleFor(t => t.Renown).GreaterThanOrEqualTo(0).WithMessage(t => $"{t.Name} kept negative renown.");
        });
        RuleForEach(season => season.Retired).Must((season, trooper) => trooper.AfterRound >= 0 && trooper.AfterRound <= season.Rounds.Count)
            .WithMessage((_, trooper) => $"{trooper.Name} retired after a contract the season hasn't played.");
    }
}
