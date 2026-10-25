using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.About;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using InfinityMercsApp.Services.Links;

namespace InfinityMercsApp.UnitTests.ViewModels.About;

public sealed class AboutPageViewModelTests
{
    private readonly ILinkLauncher _launcher = Substitute.For<ILinkLauncher>();

    private AboutPageViewModel Create() => new(Substitute.For<INavigationService>(), _launcher);

    [Theory]
    [InlineData(400, -1)]
    [InlineData(500, -1)]
    [InlineData(664, 300)]
    [InlineData(50, -1)]
    public void FitAttributionColumnsCommand_Width_FitsTwoColumnsOver500(double pageWidth, double entryWidth)
    {
        var viewModel = Create();

        viewModel.FitAttributionColumnsCommand.Execute(pageWidth);

        Assert.Equal(entryWidth, viewModel.AttributionEntryWidth);
    }

    [Fact]
    public void AttributionEntryWidth_New_IsTheEntriesOwnWidth() => Assert.Equal(-1, Create().AttributionEntryWidth);

    [Fact]
    public async Task OpenEmailCommand_Executed_OpensTheAuthorEmail()
    {
        await Create().OpenEmailCommand.ExecuteAsync(null);

        await _launcher.Received(1).OpenAsync(Arg.Is<string>(uri => uri == AboutPageViewModel.AuthorEmailUri));
    }

    [Fact]
    public async Task OpenDiscordCommand_NoDiscordApp_FallsBackToTheWeb()
    {
        _launcher.OpenAsync(Arg.Is<string>(uri => uri.StartsWith("discord:", StringComparison.Ordinal))).ThrowsAsync(new InvalidOperationException());

        await Create().OpenDiscordCommand.ExecuteAsync(null);

        await _launcher.Received(1).OpenAsync(Arg.Is<string>(uri => uri.StartsWith(AboutPageViewModel.DiscordWebUri, StringComparison.Ordinal)));
    }

    [Fact]
    public async Task OpenLinkCommand_BlankLink_IgnoresIt()
    {
        await Create().OpenLinkCommand.ExecuteAsync(" ");

        await _launcher.DidNotReceiveWithAnyArgs().OpenAsync(Arg.Any<string>());
    }
}
