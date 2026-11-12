using System.Security.Cryptography;
using InfinityMercsApp.Domain.Leagues;

namespace InfinityMercsApp.Api.Leagues;

/// <inheritdoc/>
/// <remarks>Picks each character with a cryptographic random number, from about 900 million codes.</remarks>
public sealed class RandomInviteCodes : IInviteCodeSource
{
    /// <inheritdoc/>
    public string Next() => RandomNumberGenerator.GetString(InviteCodes.Alphabet, InviteCodes.Length);
}
