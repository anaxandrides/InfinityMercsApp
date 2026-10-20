using InfinityMercsApp.Domain.Models.Company;

namespace InfinityMercsApp.Api.UserDocuments;

/// <summary>A company needs a name and identifier, its factions, and its troopers with one Captain among them.</summary>
public sealed class CompanyFileChecks : IUserFileChecks<SavedCompanyFile>
{
    /// <inheritdoc/>
    public IReadOnlyList<string> ProblemsWith(SavedCompanyFile content)
    {
        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(content.CompanyName))
        {
            problems.Add("The company has no name.");
        }

        if (string.IsNullOrWhiteSpace(content.CompanyIdentifier))
        {
            problems.Add("The company has no identifier.");
        }

        if (content.SourceFactions.Count == 0)
        {
            problems.Add("The company has no factions.");
        }

        if (content.Entries.Count(entry => entry.IsLieutenant) != 1)
        {
            problems.Add("The company must have exactly one Captain.");
        }

        if (content.Entries.DistinctBy(entry => entry.EntryIndex).Count() != content.Entries.Count)
        {
            problems.Add("Two troopers share an entry index.");
        }

        return problems;
    }
}
