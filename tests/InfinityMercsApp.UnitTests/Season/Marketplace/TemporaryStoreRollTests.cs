using InfinityMercsApp.Domain.Season.Marketplace;

namespace InfinityMercsApp.UnitTests.Season.Marketplace;

public sealed class TemporaryStoreRollTests
{
    private static readonly IReadOnlySet<string> OwnsNumberOne = new HashSet<string>(["Number One"], StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void ARollNamesItsStore()
    {
        Assert.Equal(new TemporaryStoreRoll(3, TemporaryStoreRollKind.Found, "Jade Temu"), TemporaryStoreRoll.For(3, OwnsNumberOne));
    }

    [Fact]
    public void AStoreTheCompanyHasNeedsAnother()
    {
        Assert.Equal(new TemporaryStoreRoll(1, TemporaryStoreRollKind.AlreadyOwned, "Number One"), TemporaryStoreRoll.For(1, OwnsNumberOne));
    }

    [Theory]
    [InlineData(19)]
    [InlineData(20)]
    public void NineteenOrTwentyChoosesAnyStore(int roll)
    {
        Assert.Equal(TemporaryStoreRollKind.ChooseAny, TemporaryStoreRoll.For(roll, OwnsNumberOne).Kind);
    }

    [Fact]
    public void ARollOffTheTableIsInvalid()
    {
        Assert.Equal(TemporaryStoreRollKind.Invalid, TemporaryStoreRoll.For(0, OwnsNumberOne).Kind);
    }

    [Fact]
    public void SearchingRerollsStoresTheCompanyHas()
    {
        var rolls = new Queue<int>([1, 2, 5]);

        var result = TemporaryStoreRoll.Search(rolls.Dequeue, OwnsNumberOne);

        Assert.Equal(new TemporaryStoreRoll(5, TemporaryStoreRollKind.Found, "Arachne Req"), result);
    }

    [Fact]
    public void SearchingStopsOnAFreeChoice()
    {
        var rolls = new Queue<int>([1, 20]);

        Assert.Equal(TemporaryStoreRollKind.ChooseAny, TemporaryStoreRoll.Search(rolls.Dequeue, OwnsNumberOne).Kind);
    }

    [Fact]
    public void SearchingGivesUpAfterTooManyRerolls()
    {
        var attempts = 0;

        var result = TemporaryStoreRoll.Search(() => { attempts++; return 1; }, OwnsNumberOne);

        Assert.Equal(TemporaryStoreRollKind.Exhausted, result.Kind);
        Assert.Equal(TemporaryStoreRoll.MaxAttempts, attempts);
    }
}
