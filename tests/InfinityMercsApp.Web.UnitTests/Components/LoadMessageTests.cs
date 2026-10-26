using Bunit;
using InfinityMercsApp.Web.Components;
using InfinityMercsApp.Web.Files;

namespace InfinityMercsApp.Web.UnitTests.Components;

public sealed class LoadMessageTests : BunitContext
{
    [Theory]
    [InlineData(LoadStatus.SignInNeeded, "Sign in again")]
    [InlineData(LoadStatus.NotFound, "There's no such company")]
    [InlineData(LoadStatus.Unavailable, "Couldn't reach the server")]
    public void Render_Status_SaysWhy(LoadStatus status, string expected) =>
        Assert.Contains(expected, Render<LoadMessage>(parameters => parameters.Add(p => p.Status, status).Add(p => p.What, "company")).Markup, StringComparison.Ordinal);

    [Fact]
    public void Render_Loaded_SaysNothing() =>
        Assert.Empty(Render<LoadMessage>(parameters => parameters.Add(p => p.Status, LoadStatus.Loaded)).Markup.Trim());
}
