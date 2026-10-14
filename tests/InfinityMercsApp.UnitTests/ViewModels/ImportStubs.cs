using System.Runtime.CompilerServices;
using InfinityMercsApp.Domain.Models.DataImport;

namespace InfinityMercsApp.UnitTests.ViewModels;

internal static class ImportStubs
{
    public static async IAsyncEnumerable<SuccessWithStringResult> Messages(params IEnumerable<string> messages)
    {
        foreach (var message in messages)
        {
            await Task.Yield();
            yield return new SuccessWithStringResult(true, message);
        }
    }

    public static async IAsyncEnumerable<SuccessWithStringResult> Failing(
        string message, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        throw new InvalidOperationException(message);
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }
}
