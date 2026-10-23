using FluentValidation;
using InfinityMercsApp.Domain.Company;
using InfinityMercsApp.Domain.Models.Company;

namespace InfinityMercsApp.Domain.Validation.Company;

/// <summary>
/// What a saved company must be before it is kept online: named, of a known type, from one or two armies, with one
/// Captain, and troopers that fit together (peripherals connected to troopers, a Proxy Pack's Aspects to its Lone Wolf).
/// </summary>
public sealed class SavedCompanyFileValidator : AbstractValidator<SavedCompanyFile>
{
    public const int MaxNameLength = 100;

    public static SavedCompanyFileValidator Instance { get; } = new();

    public SavedCompanyFileValidator()
    {
        RuleFor(company => company.CompanyName).NotEmpty().WithMessage("The company has no name.")
            .MaximumLength(MaxNameLength).WithMessage($"The company's name is longer than {MaxNameLength} characters.");
        RuleFor(company => company.CompanyType).Must(type => CompanyTypes.All.Contains(type))
            .WithMessage(company => $"\"{company.CompanyType}\" isn't a company type.");
        RuleFor(company => company.CompanyIdentifier).Must(DocumentIdentifiers.IsValid)
            .WithMessage("The company's identifier must be 1 to 64 letters, digits, '-' or '_'.");
        RuleFor(company => company.StartSeasonPoints).GreaterThanOrEqualTo(0).WithMessage("The company's starting points are negative.");
        RuleFor(company => company.PointsLimit).GreaterThanOrEqualTo(0).WithMessage("The company's points limit is negative.");
        RuleFor(company => company.CurrentPoints).GreaterThanOrEqualTo(0).WithMessage("The company's recruiting spend is negative.");

        RuleFor(company => company.SourceFactions.Count).InclusiveBetween(1, 2).WithMessage("The company must come from one or two armies.");
        RuleForEach(company => company.SourceFactions).Must(faction => faction.FactionId > 0).WithMessage("One of the company's armies has no id.");
        RuleFor(company => company.SourceFactions).Must(factions => factions.DistinctBy(faction => faction.FactionId).Count() == factions.Count)
            .WithMessage("The company names the same army twice.");

        RuleFor(company => company.CoreFireteam).NotEmpty().When(company => company.CompanyType == CompanyTypes.Cohesive)
            .WithMessage("A Cohesive Company needs its core fireteam.");
        RuleFor(company => company.CoreFireteam).Empty().When(company => company.CompanyType != CompanyTypes.Cohesive)
            .WithMessage("Only a Cohesive Company has a core fireteam.");
        RuleFor(company => company.CoreFireteamSlots).Empty().When(company => company.CompanyType != CompanyTypes.Cohesive)
            .WithMessage("Only a Cohesive Company has core fireteam places.");

        RuleFor(company => company.Entries).NotEmpty().WithMessage("The company has no troopers.");
        RuleForEach(company => company.Entries).SetValidator(new SavedCompanyEntryValidator());
        RuleFor(company => company.Entries).Must(entries => entries.Count(entry => entry.IsLieutenant) == 1)
            .WithMessage("The company must have exactly one Captain.");
        RuleFor(company => company.Entries).Must(entries => entries.DistinctBy(entry => entry.EntryIndex).Count() == entries.Count)
            .WithMessage("Two troopers share an entry index.");
        // Aspects are copies of the Lone Wolf, founding upgrades included.
        RuleFor(company => company.Entries).Must(entries => entries.Count(entry => entry.CarriesCaptainUpgrades && entry.AspectOf is null) <= 1)
            .WithMessage("Only one trooper can carry the founding Captain's upgrades.");
        RuleForEach(company => company.Entries).Must(ConnectsToATrooper)
            .WithMessage((_, entry) => $"{entry.Name} is connected to a trooper the company doesn't have.");

        RuleFor(company => company.Entries).Must(entries => entries.Count(entry => entry.IsLoneWolf) == 1)
            .When(company => company.CompanyType == ProxyPack.CompanyType)
            .WithMessage("A Proxy Pack must have exactly one Lone Wolf.");
        RuleFor(company => company.Entries).Must(entries => entries.All(entry => !entry.IsLoneWolf && entry.AspectOf is null))
            .When(company => company.CompanyType != ProxyPack.CompanyType)
            .WithMessage("Only a Proxy Pack has a Lone Wolf and Aspects.");
        RuleFor(company => company.Entries).Must(entries => entries.All(entry => entry.IsLoneWolf || entry.AspectOf is not null || entry.IsPeripheralUnit))
            .When(company => company.CompanyType == ProxyPack.CompanyType)
            .WithMessage("A Proxy Pack has only its Lone Wolf, its Aspects and their peripherals.");
        RuleFor(company => company.Entries).Must(entries => entries.Count(entry => entry.AspectOf is not null) <= ProxyPack.MaxAspects - 1)
            .WithMessage($"The Lone Wolf has more than {ProxyPack.MaxAspects} Aspects, its own included.");
        RuleForEach(company => company.Entries).Must(IsAnAspectOfTheLoneWolf)
            .When(company => company.CompanyType == ProxyPack.CompanyType)
            .WithMessage((_, entry) => $"{entry.Name} is an Aspect of a trooper that isn't the Lone Wolf.");
    }

    // A peripheral's trooper is in the company and isn't a peripheral itself.
    private static bool ConnectsToATrooper(SavedCompanyFile company, SavedCompanyEntry entry) =>
        entry.ParentEntryIndex is not { } parent ||
        company.Entries.Any(other => other.EntryIndex == parent && !other.IsPeripheralUnit);

    private static bool IsAnAspectOfTheLoneWolf(SavedCompanyFile company, SavedCompanyEntry entry) =>
        entry.AspectOf is not { } wolf || company.Entries.Any(other => other.EntryIndex == wolf && other.IsLoneWolf);
}
