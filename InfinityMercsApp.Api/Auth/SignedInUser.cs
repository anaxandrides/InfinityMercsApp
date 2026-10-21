namespace InfinityMercsApp.Api.Auth;

/// <summary>The user a request's sign-in token belongs to.</summary>
/// <param name="UserId">The user's object id in the sign-in tenant, which never changes for them.</param>
public sealed record SignedInUser(string UserId);
