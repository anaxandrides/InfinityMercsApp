using InfinityMercsApp.Presentation.Season.Overview;

namespace InfinityMercsApp.UnitTests.Presentation.Season.Overview;

public sealed class ProfileColumnsTests
{
    [Fact]
    public void FillsLeftThenRight()
    {
        var columns = ProfileColumns.From(["A", "B", "C"]);

        Assert.Equal(["A", "C"], columns.Left.Select(line => line.Text));
        Assert.Equal(["B"], columns.Right.Select(line => line.Text));
    }

    [Fact]
    public void AnEmptyListSaysNone()
    {
        var columns = ProfileColumns.From([]);

        Assert.Equal(ProfileLine.None, columns.Left.Single());
        Assert.True(columns.Left[0].IsPlaceholder);
        Assert.Empty(columns.Right);
    }
}
