using InfinityMercsApp.Services.CompanySelection.Models.Saved;

namespace InfinityMercsApp.Services.CompanySelection.Recruitment;

/// <summary>
/// A season company hiring new troopers: its files, the company as saved, and the CR it has to spend.
/// </summary>
public sealed record CompanyRecruitment(string CompanyFilePath, string SeasonFilePath, SavedCompanyFile Company, int Credits);
