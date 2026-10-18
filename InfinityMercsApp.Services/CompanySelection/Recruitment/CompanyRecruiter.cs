using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.CompanySelection.StartCompany;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.Services.Season;

namespace InfinityMercsApp.Services.CompanySelection.Recruitment;

/// <summary>
/// Hires new recruits mid-season: they join the company file after its last trooper, and their CR cost is
/// charged in the season through the Additional Recruitment store. Troopers already on the roster keep their
/// entries but take the order type the company now gives them, since a recruit may take a fireteam place.
/// </summary>
public sealed class CompanyRecruiter(SavedEntryWriter entries, ICompanyFileService companyFiles, ISeasonFileService seasonFiles)
{
    /// <summary>What a recruit's purchase is called in the season's marketplace.</summary>
    public const string ItemPrefix = "Recruit: ";

    /// <summary>
    /// Adds the recruits to the company and charges them in the season. When the season can't be saved, the
    /// company is put back as it was, so nobody joins without being paid for.
    /// </summary>
    /// <param name="roster">Everyone the recruiting screen lists: troopers already saved, then the recruits.</param>
    public async Task<RecruitmentOutcome> HireAsync(CompanyRecruitment recruitment, IReadOnlyList<ICompanyMercsEntry> roster)
    {
        var recruits = roster.Where(entry => entry.SavedEntryIndex is null).ToList();
        if (recruits.Count == 0)
        {
            return RecruitmentOutcome.NothingToHire;
        }

        var company = await companyFiles.LoadAsync(recruitment.CompanyFilePath) ?? recruitment.Company;
        var firstEntryIndex = company.Entries.Count == 0 ? 0 : company.Entries.Max(entry => entry.EntryIndex) + 1;
        var hired = entries.RecruitEntries(recruits, firstEntryIndex);
        var orderTypesBefore = company.Entries.ToDictionary(entry => entry.EntryIndex, entry => entry.IrregularInCompany);
        foreach (var onRoster in roster)
        {
            if (company.Entries.FirstOrDefault(entry => entry.EntryIndex == onRoster.SavedEntryIndex) is { } saved)
            {
                saved.IrregularInCompany = onRoster.IrregularInCompany;
            }
        }

        company.Entries.AddRange(hired);
        if (!await TrySaveAsync(recruitment.CompanyFilePath, company))
        {
            return RecruitmentOutcome.CompanyNotSaved;
        }

        var charged = await seasonFiles.UpdateAsync(recruitment.SeasonFilePath, season =>
        {
            var marketplace = SeasonRounds.CurrentMarketplace(season);
            foreach (var recruit in recruits)
            {
                marketplace.Transactions.Add(new SeasonTransaction
                {
                    OriginStore = ServiceStoreNames.AdditionalRecruitment,
                    ItemName = ItemPrefix + recruit.Name,
                    CostCr = recruit.CostValue
                });
            }
        });

        if (!charged)
        {
            company.Entries.RemoveAll(entry => entry.EntryIndex >= firstEntryIndex);
            foreach (var entry in company.Entries)
            {
                entry.IrregularInCompany = orderTypesBefore.GetValueOrDefault(entry.EntryIndex);
            }

            await TrySaveAsync(recruitment.CompanyFilePath, company);
            return RecruitmentOutcome.SeasonNotSaved;
        }

        return RecruitmentOutcome.Hired;
    }

    private async Task<bool> TrySaveAsync(string path, SavedCompanyFile company)
    {
        try
        {
            await companyFiles.SaveAsync(path, company);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Saving the company after recruiting failed: {ex.Message}");
            return false;
        }
    }
}
