namespace InfinityMercsApp.Services.CompanySelection.StartCompany;

/// <summary>How founding a company ended.</summary>
public enum FoundingOutcome
{
    Founded,

    /// <summary>The company had nobody in it.</summary>
    NoTroopers,

    /// <summary>The player backed out of the captain popup; nothing was saved.</summary>
    CaptainCancelled
}
