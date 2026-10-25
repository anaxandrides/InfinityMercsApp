using InfinityMercsApp.Infrastructure.Repositories.InMemory;

namespace InfinityMercsApp.UnitTests.Infrastructure.Repositories.InMemory;

public sealed class KeyOrderTests
{
    [Fact]
    public void Compare_Nulls_SortFirst()
    {
        Assert.Equal(0, KeyOrder.Instance.Compare(null, null));
        Assert.True(KeyOrder.Instance.Compare(null, 1) < 0);
        Assert.True(KeyOrder.Instance.Compare(1, null) > 0);
    }

    [Fact]
    public void Compare_Text_OrdersByCharacterCode() => Assert.True(KeyOrder.Instance.Compare("Z", "a") < 0);

    [Fact]
    public void Compare_Numbers_OrdersByValue() => Assert.True(KeyOrder.Instance.Compare(2, 10) < 0);

    [Fact]
    public void Compare_NotComparable_UsesTheDefaultComparer() =>
        Assert.Throws<ArgumentException>(() => KeyOrder.Instance.Compare(new object(), new object()));
}
