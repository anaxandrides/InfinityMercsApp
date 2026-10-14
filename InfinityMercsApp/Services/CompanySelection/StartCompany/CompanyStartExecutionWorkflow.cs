namespace InfinityMercsApp.Views.Common;

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
