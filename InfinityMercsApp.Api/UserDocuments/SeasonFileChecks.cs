using InfinityMercsApp.Domain.Models.Season;

namespace InfinityMercsApp.Api.UserDocuments;

/// <summary>A season needs the name and identifier of the company it belongs to.</summary>
public sealed class SeasonFileChecks : IUserFileChecks<SeasonFile>
{
    /// <inheritdoc/>
    public IReadOnlyList<string> ProblemsWith(SeasonFile content)
    {
        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(content.CompanyName))
        {
            problems.Add("The season has no company name.");
        }

        if (string.IsNullOrWhiteSpace(content.CompanyIdentifier))
        {
            problems.Add("The season has no company identifier.");
        }

        return problems;
    }
}
