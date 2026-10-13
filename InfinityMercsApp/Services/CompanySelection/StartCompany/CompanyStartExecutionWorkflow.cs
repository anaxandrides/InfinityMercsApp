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

internal static class CompanyStartExecutionWorkflow
{
    internal static async Task ExecuteAsync<TFaction, TEntry>(CompanyStartExecutionRequest<TFaction, TEntry> request)
        where TFaction : class, ICompanySourceFaction
        where TEntry : class, ICompanyMercsEntry
    {
        if (!CompanyStartSharedState.IsCompanyNameValid(request.CompanyName))
        {
            request.SetCompanyNameValidationError(true);
            return;
        }

        request.SetCompanyNameValidationError(false);

        try
        {
            await CompanyStartSaveWorkflow.RunAsync(request.BuildSaveRequest());
        }
        catch (Exception ex)
        {
            await request.HandleFailureAsync(ex);
        }
    }
}


