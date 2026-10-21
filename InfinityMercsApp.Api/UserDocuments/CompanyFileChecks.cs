using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Domain.Validation.Company;

namespace InfinityMercsApp.Api.UserDocuments;

/// <summary>A company must pass <see cref="SavedCompanyFileValidator"/>, and be saved under its own identifier.</summary>
public sealed class CompanyFileChecks : IUserFileChecks<SavedCompanyFile>
{
    /// <inheritdoc/>
    public IReadOnlyList<string> ProblemsWith(string documentId, SavedCompanyFile content)
    {
        var problems = SavedCompanyFileValidator.Instance.Validate(content).Errors.Select(error => error.ErrorMessage).ToList();
        if (content.CompanyIdentifier != documentId)
        {
            problems.Add("The company's identifier isn't the one it is saved under.");
        }

        return problems;
    }
}
