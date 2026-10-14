using InfinityMercsApp.Domain.Models.Stores;

namespace InfinityMercsApp.UnitTests.Stores;

public sealed class StoreTests
{
    [Theory]
    [InlineData("Lawful", true, false)]
    [InlineData("chaotic", false, true)]
    [InlineData("Neutral", false, false)]
    public void AlignmentIsReadIgnoringCase(string alignment, bool lawful, bool chaotic)
    {
        var store = new Store { Alignment = alignment };

        Assert.Equal(lawful, store.IsLawful);
        Assert.Equal(chaotic, store.IsChaotic);
    }

    [Fact]
    public void AlwaysAvailableIsItsOwnAlignment()
    {
        Assert.True(new Store { Alignment = "Always Available" }.IsAlwaysAvailable);
        Assert.False(new Store { Alignment = "Neutral" }.IsAlwaysAvailable);
    }

    [Theory]
    [InlineData("Medical Services", true)]
    [InlineData("additional recruitment", true)]
    [InlineData("Base Market", false)]
    public void ServiceStoresAreRecognisedByName(string name, bool isService) =>
        Assert.Equal(isService, new Store { Name = name }.IsServiceStore);
}
