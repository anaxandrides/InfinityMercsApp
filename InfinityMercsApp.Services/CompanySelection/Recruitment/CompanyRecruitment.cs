using InfinityMercsApp.Domain.Models.Company;

namespace InfinityMercsApp.Services.CompanySelection.Recruitment;

/// <summary>
/// A season company hiring new troopers: its files, the company as saved, the CR it has to spend, and the
/// troopers retired this season, which still count toward AVA.
/// </summary>
public sealed record CompanyRecruitment(
    string CompanyFilePath,
    string SeasonFilePath,
    SavedCompanyFile Company,
    int Credits,
    IReadOnlyCollection<int> RetiredEntries);
