namespace InfinityMercsApp.Web.Files;

/// <summary>What was fetched, or why nothing was.</summary>
public sealed record Loaded<TValue>(LoadStatus Status, TValue? Value)
    where TValue : class
{
    public static Loaded<TValue> Of(LoadStatus status) => new(status, null);
}
