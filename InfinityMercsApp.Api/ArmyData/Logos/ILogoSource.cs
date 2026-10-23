namespace InfinityMercsApp.Api.ArmyData.Logos;

/// <summary>Where the logos are downloaded from.</summary>
public interface ILogoSource
{
    /// <summary>The logo's bytes, or null when it couldn't be downloaded.</summary>
    Task<byte[]?> GetAsync(string url, CancellationToken cancellationToken = default);
}
