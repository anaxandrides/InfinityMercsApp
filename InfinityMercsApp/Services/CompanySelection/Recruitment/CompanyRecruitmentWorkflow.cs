using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.CompanySelection.StartCompany;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels.Company.Selection.Items;

namespace InfinityMercsApp.Services.CompanySelection.Recruitment;

/// <summary>
/// Hires new recruits mid-season: they join the company file after its last trooper, and their CR cost is
/// charged in the season through the Additional Recruitment store.
/// </summary>
public static class CompanyRecruitmentWorkflow
{
    /// <summary>What a recruit's purchase is called in the season's marketplace.</summary>
    public const string ItemPrefix = "Recruit: ";

    /// <summary>
    /// Adds the recruits to the company and charges them in the season. When the season can't be saved, the
    /// company is put back as it was, so nobody joins without being paid for.
    /// </summary>
    public static async Task<RecruitmentOutcome> HireAsync(
        CompanyRecruitment recruitment,
        IReadOnlyList<MercsCompanyEntry> recruits,
        IArmyDataService armyData,
        ICompanyFileService companyFiles,
        ISeasonFileService seasonFiles)
    {
        if (recruits.Count == 0)
        {
            return RecruitmentOutcome.NothingToHire;
        }

        var company = await companyFiles.LoadAsync(recruitment.CompanyFilePath) ?? recruitment.Company;
        var firstEntryIndex = company.Entries.Count == 0 ? 0 : company.Entries.Max(entry => entry.EntryIndex) + 1;
        var hired = CompanyStartSaveWorkflow.RecruitEntries(recruits.ToList(), firstEntryIndex, armyData);

        company.Entries.AddRange(hired);
        if (!await TrySaveAsync(companyFiles, recruitment.CompanyFilePath, company))
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
            await TrySaveAsync(companyFiles, recruitment.CompanyFilePath, company);
            return RecruitmentOutcome.SeasonNotSaved;
        }

        return RecruitmentOutcome.Hired;
    }

    private static async Task<bool> TrySaveAsync(ICompanyFileService companyFiles, string path, SavedCompanyFile company)
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
