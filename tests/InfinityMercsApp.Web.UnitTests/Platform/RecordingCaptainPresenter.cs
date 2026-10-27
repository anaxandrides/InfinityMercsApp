using InfinityMercsApp.ViewModels.Captain;
using InfinityMercsApp.Web.Platform;

namespace InfinityMercsApp.Web.UnitTests.Platform;

/// <summary>Keeps each captain configuration shown, and null for each time it was hidden.</summary>
internal sealed class RecordingCaptainPresenter : ICaptainPresenter
{
    public List<ConfigureCaptainViewModel?> Shown { get; } = [];

    public void Show(ConfigureCaptainViewModel? viewModel) => Shown.Add(viewModel);
}
