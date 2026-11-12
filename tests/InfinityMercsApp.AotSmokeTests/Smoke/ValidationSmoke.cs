using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Validation.Company;
using InfinityMercsApp.Domain.Validation.Season;

namespace InfinityMercsApp.AotSmokeTests.Smoke;

/// <summary>FluentValidation: the rules a company and a season must pass before they are kept online.</summary>
public static class ValidationSmoke
{
    public static void Run(SmokeChecks checks)
    {
        var company = new SavedCompanyFile
        {
            CompanyName = "Blue Lions",
            CompanyType = "Standard Company - Vanilla",
            CompanyIdentifier = "c1",
            SourceFactions = [new SavedCompanyFaction { FactionId = 101 }],
            Entries = [new SavedCompanyEntry { EntryIndex = 0, Name = "Fusilier", SourceUnitId = 1, SourceFactionId = 101, IsLieutenant = true }]
        };
        checks.True("validation: a sound company passes", SavedCompanyFileValidator.Instance.Validate(company).IsValid);

        var broken = new SavedCompanyFile
        {
            CompanyName = string.Empty,
            CompanyType = "Nonsense",
            CompanyIdentifier = "bad id!",
            SourceFactions = [],
            Entries = [new SavedCompanyEntry { EntryIndex = -1 }]
        };
        var problems = SavedCompanyFileValidator.Instance.Validate(broken).Errors;
        checks.Equal("validation: a broken company's problems", 9, problems.Count);
        // Property names come from the rules' expression trees; messages from their lambdas.
        checks.True("validation: problems name the property", problems.Any(error => error.PropertyName == "Entries[0].EntryIndex"));
        foreach (var problem in problems)
        {
            checks.Note("  problem", $"{problem.PropertyName}: {problem.ErrorMessage}");
        }

        var season = SeasonFileValidator.Instance.Validate(new SeasonFile { SeasonIdentifier = "s1" });
        checks.Equal("validation: an empty season's problems", 2, season.Errors.Count);
    }
}
