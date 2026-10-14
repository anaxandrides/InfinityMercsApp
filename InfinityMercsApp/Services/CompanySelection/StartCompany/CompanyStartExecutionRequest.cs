namespace InfinityMercsApp.Views.Common;

internal sealed class CompanyStartExecutionRequest<TFaction, TEntry>
    where TFaction : class, ICompanySourceFaction
    where TEntry : class, ICompanyMercsEntry
{
    public required string? CompanyName { get; init; }
    public required Action<bool> SetCompanyNameValidationError { get; init; }
    public required Func<CompanyStartSaveRequest<TFaction, TEntry>> BuildSaveRequest { get; init; }
    public required Func<Exception, Task> HandleFailureAsync { get; init; }
}
