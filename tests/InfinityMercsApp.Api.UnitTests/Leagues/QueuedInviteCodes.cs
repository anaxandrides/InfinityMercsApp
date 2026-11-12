using InfinityMercsApp.Api.Leagues;
using InfinityMercsApp.Domain.Leagues;

namespace InfinityMercsApp.Api.UnitTests.Leagues;

/// <summary>Hands out the codes it was given, in order, then ZZZZZ2, ZZZZZ3 and so on.</summary>
internal sealed class QueuedInviteCodes(params IEnumerable<string> codes) : IInviteCodeSource
{
    private readonly Queue<string> _codes = new(codes);
    private int _made;

    public string Next() => _codes.TryDequeue(out var code) ? code : "ZZZZZ" + InviteCodes.Alphabet[_made++ % InviteCodes.Alphabet.Length];
}
