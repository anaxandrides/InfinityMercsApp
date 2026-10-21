namespace InfinityMercsApp.Web.Files;

/// <summary>The signed-in user's token for the API.</summary>
public interface IAccessTokens
{
    /// <summary>The token, or null when the user has to sign in again.</summary>
    Task<string?> GetAsync();
}
