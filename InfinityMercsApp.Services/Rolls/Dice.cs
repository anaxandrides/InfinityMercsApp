using InfinityMercsApp.Domain.Season.Rolls;

namespace InfinityMercsApp.Services.Rolls;

internal sealed class Dice : IDice
{
    public int RollD20() => Random.Shared.Next(1, 21);

    public int RollD6() => Random.Shared.Next(1, 7);
}
