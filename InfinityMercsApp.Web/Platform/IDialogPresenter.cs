namespace InfinityMercsApp.Web.Platform;

/// <summary>The component that shows dialogs: the site's dialog host, told when one is waiting.</summary>
public interface IDialogPresenter
{
    void Show(DialogRequest request);
}
