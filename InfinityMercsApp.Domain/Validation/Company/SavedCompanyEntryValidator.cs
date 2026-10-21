using FluentValidation;
using InfinityMercsApp.Domain.Models.Company;

namespace InfinityMercsApp.Domain.Validation.Company;

/// <summary>What one trooper or peripheral must be on its own; how troopers fit together is the company's check.</summary>
public sealed class SavedCompanyEntryValidator : AbstractValidator<SavedCompanyEntry>
{
    public SavedCompanyEntryValidator()
    {
        RuleFor(entry => entry.EntryIndex).GreaterThanOrEqualTo(0).WithMessage("A trooper has a negative entry index.");
        RuleFor(entry => entry.Name).NotEmpty().WithMessage(entry => $"Trooper {entry.EntryIndex} has no name.");
        RuleFor(entry => entry.SourceUnitId).GreaterThan(0).WithMessage(entry => $"{entry.Name} names no unit.");
        RuleFor(entry => entry.SourceFactionId).GreaterThan(0).WithMessage(entry => $"{entry.Name} names no army.");
        RuleFor(entry => entry.Cost).GreaterThanOrEqualTo(0).WithMessage(entry => $"{entry.Name} has a negative cost.");
        RuleFor(entry => entry.ExperiencePoints).GreaterThanOrEqualTo(0).WithMessage(entry => $"{entry.Name} has negative XP.");

        RuleFor(entry => entry.IsLieutenant).Equal(false).When(entry => entry.IsPeripheralUnit)
            .WithMessage(entry => $"{entry.Name} is a peripheral, so it can't be the Captain.");
        RuleFor(entry => entry.ParentEntryIndex).Null().When(entry => !entry.IsPeripheralUnit)
            .WithMessage(entry => $"{entry.Name} isn't a peripheral, so it can't be connected to a trooper.");
        RuleFor(entry => entry.ParentEntryIndex).NotNull().When(entry => entry.IsPeripheralUnit && !entry.FromControllerAugment)
            .WithMessage(entry => $"{entry.Name} is a peripheral connected to no trooper.");
        RuleFor(entry => entry.ParentEntryIndex).NotEqual(entry => entry.EntryIndex)
            .WithMessage(entry => $"{entry.Name} is connected to itself.");

        RuleFor(entry => entry.AspectOf).NotEqual(entry => entry.EntryIndex)
            .WithMessage(entry => $"{entry.Name} is an Aspect of itself.");
        RuleFor(entry => entry.Cost).Equal(0).When(entry => entry.AspectOf is not null)
            .WithMessage(entry => $"{entry.Name} is an Aspect, so it costs nothing.");
        RuleFor(entry => entry.IsLieutenant).Equal(false).When(entry => entry.AspectOf is not null)
            .WithMessage(entry => $"{entry.Name} is an Aspect, so it can't be the Captain.");
        RuleFor(entry => entry.IsLoneWolf).Equal(false).When(entry => entry.AspectOf is not null)
            .WithMessage(entry => $"{entry.Name} is an Aspect, not the Lone Wolf itself.");
    }
}
