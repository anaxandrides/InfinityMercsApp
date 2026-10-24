using InfinityMercsApp.ViewModels.Company.Selection;

namespace InfinityMercsApp.Web.Platform;

/// <summary>The selection screen being built, handed from the page that opened it to the selection page.</summary>
public sealed class CompanySelectionSession
{
    public CompanySelectionViewModel? Current { get; set; }
}
