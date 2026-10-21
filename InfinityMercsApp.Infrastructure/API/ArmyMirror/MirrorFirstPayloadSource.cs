using System.Net;
using InfinityMercsApp.Infrastructure.API.InfinityArmy;

namespace InfinityMercsApp.Infrastructure.API.ArmyMirror;

/// <summary>
/// Reads each payload from the app's own copy of the army data, and from Corvus Belli when the copy can't serve it.
/// After the copy fails to answer, Corvus Belli is used for a while without trying the copy, so a check isn't slowed
/// by waiting on it for every army.
/// </summary>
public sealed class MirrorFirstPayloadSource(IArmyPayloadSource mirror, IArmyPayloadSource corvusBelli, TimeProvider time) : IArmyPayloadSource
{
    /// <summary>How long Corvus Belli is used alone after the copy fails.</summary>
    public static readonly TimeSpan MirrorRetryDelay = TimeSpan.FromMinutes(10);

    private DateTimeOffset _mirrorDownUntil = DateTimeOffset.MinValue;

    /// <inheritdoc/>
    public Task<string> GetMetadataAsync(CancellationToken cancellationToken = default) =>
        ReadAsync(PayloadRequest.Metadata, cancellationToken);

    /// <inheritdoc/>
    public Task<string> GetArmyAsync(int factionId, CancellationToken cancellationToken = default) =>
        ReadAsync(PayloadRequest.Army(factionId), cancellationToken);

    private async Task<string> ReadAsync(PayloadRequest request, CancellationToken cancellationToken)
    {
        if (time.GetUtcNow() >= _mirrorDownUntil)
        {
            try
            {
                return await request.ReadFromAsync(mirror, cancellationToken);
            }
            catch (Exception ex) when ((ex is HttpRequestException or TaskCanceledException) && !cancellationToken.IsCancellationRequested)
            {
                Console.Error.WriteLine($"The army data copy couldn't serve {request}, so Corvus Belli is used: {ex.Message}");
                // A payload the copy doesn't hold yet, such as a new faction's, says nothing about the others.
                if (ex is not HttpRequestException { StatusCode: HttpStatusCode.NotFound })
                {
                    _mirrorDownUntil = time.GetUtcNow() + MirrorRetryDelay;
                }
            }
        }

        return await request.ReadFromAsync(corvusBelli, cancellationToken);
    }
}
