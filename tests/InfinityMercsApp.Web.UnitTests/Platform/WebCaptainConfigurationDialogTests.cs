using InfinityMercsApp.Services.CompanySelection.Captain;
using InfinityMercsApp.Web.Platform;

namespace InfinityMercsApp.Web.UnitTests.Platform;

/// <summary>The captain configuration, shown over the page until the player confirms or backs out.</summary>
public sealed class WebCaptainConfigurationDialogTests
{
    private static readonly CaptainUpgradePopupContext Context = new() { Unit = new CaptainUnitPopupInfo { Name = "Fusilier", Cost = 10, Statline = "-" } };

    [Fact]
    public async Task ShowAsync_NoHostYet_IsNull() => Assert.Null(await new WebCaptainConfigurationDialog().ShowAsync(Context));

    [Fact]
    public async Task ShowAsync_Cancelled_IsNullAndHidesIt()
    {
        var presenter = new RecordingCaptainPresenter();
        var dialog = new WebCaptainConfigurationDialog { Presenter = presenter };

        var shown = dialog.ShowAsync(Context);
        var viewModel = presenter.Shown[0] ?? throw new InvalidOperationException("Nothing was shown.");
        viewModel.CancelCommand.Execute(null);

        Assert.Null(await shown);
        Assert.Null(presenter.Shown[^1]);
        Assert.Equal(2, presenter.Shown.Count);
    }

    [Fact]
    public async Task ShowAsync_Confirmed_IsTheChoice()
    {
        var presenter = new RecordingCaptainPresenter();
        var dialog = new WebCaptainConfigurationDialog { Presenter = presenter };

        var shown = dialog.ShowAsync(Context);
        var viewModel = presenter.Shown[0] ?? throw new InvalidOperationException("Nothing was shown.");
        viewModel.ConfirmCommand.Execute(null);

        Assert.NotNull(await shown);
    }
}
