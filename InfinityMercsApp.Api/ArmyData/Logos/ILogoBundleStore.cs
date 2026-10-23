namespace InfinityMercsApp.Api.ArmyData.Logos;

/// <summary>Where logo bundles are kept, each under the hash of the logos in it; a bundle is never replaced.</summary>
public interface ILogoBundleStore
{
    /// <summary>Keeps the zip under <paramref name="hash"/>, unless one is already kept there.</summary>
    Task SaveAsync(string hash, Stream zip, CancellationToken cancellationToken = default);

    /// <summary>The zip kept under <paramref name="hash"/>, or null when there is none.</summary>
    Task<Stream?> OpenAsync(string hash, CancellationToken cancellationToken = default);
}
