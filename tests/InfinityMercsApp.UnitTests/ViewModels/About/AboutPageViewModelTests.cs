using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.About;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace InfinityMercsApp.UnitTests.ViewModels.About;

public sealed class AboutPageViewModelTests
{
    private readonly ILauncher _launcher = Substitute.For<ILauncher>();

    private AboutPageViewModel Create() => new(Substitute.For<INavigationService>(), _launcher);

    [Theory]
    [InlineData(400, -1)]
    [InlineData(500, -1)]
    [InlineData(664, 300)]
    [InlineData(50, -1)]
    public void FitsTwoAttributionColumnsOnScreensWiderThan500(double pageWidth, double entryWidth)
    {
        var viewModel = Create();

        viewModel.FitAttributionColumnsCommand.Execute(pageWidth);

        Assert.Equal(entryWidth, viewModel.AttributionEntryWidth);
    }

    [Fact]
    public void StartsWithEntriesAtTheirOwnWidth() => Assert.Equal(-1, Create().AttributionEntryWidth);

    [Fact]
    public async Task OpensTheAuthorEmail()
    {
        await Create().OpenEmailCommand.ExecuteAsync(null);

        await _launcher.Received(1).OpenAsync(Arg.Is<Uri>(uri => uri.ToString() == AboutPageViewModel.AuthorEmailUri));
    }

    [Fact]
    public async Task FallsBackToDiscordOnTheWeb()
    {
        _launcher.OpenAsync(Arg.Is<Uri>(uri => uri.Scheme == "discord")).ThrowsAsync(new InvalidOperationException());

        await Create().OpenDiscordCommand.ExecuteAsync(null);

        await _launcher.Received(1).OpenAsync(Arg.Is<Uri>(uri => uri.ToString().StartsWith(AboutPageViewModel.DiscordWebUri)));
    }

    [Fact]
    public async Task IgnoresABlankLink()
    {
        await Create().OpenLinkCommand.ExecuteAsync(" ");

        await _launcher.DidNotReceiveWithAnyArgs().OpenAsync(Arg.Any<Uri>());
    }
}
