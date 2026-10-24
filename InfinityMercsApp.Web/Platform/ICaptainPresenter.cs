using InfinityMercsApp.ViewModels.Captain;

namespace InfinityMercsApp.Web.Platform;

/// <summary>The component that shows the captain configuration over the page.</summary>
public interface ICaptainPresenter
{
    /// <summary>Shows it, or hides it when <paramref name="viewModel"/> is null.</summary>
    void Show(ConfigureCaptainViewModel? viewModel);
}
