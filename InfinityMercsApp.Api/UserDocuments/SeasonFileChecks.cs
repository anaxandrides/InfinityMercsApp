using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Validation.Season;

namespace InfinityMercsApp.Api.UserDocuments;

/// <summary>A season must pass <see cref="SeasonFileValidator"/>, and be saved under its own identifier.</summary>
public sealed class SeasonFileChecks : IUserFileChecks<SeasonFile>
{
    /// <inheritdoc/>
    public IReadOnlyList<string> ProblemsWith(string documentId, SeasonFile content)
    {
        var problems = SeasonFileValidator.Instance.Validate(content).Errors.Select(error => error.ErrorMessage).ToList();
        if (content.SeasonIdentifier != documentId)
        {
            problems.Add("The season's identifier isn't the one it is saved under.");
        }

        return problems;
    }
}
