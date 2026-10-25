namespace InfinityMercsApp.Services.Links;

/// <summary>Opens a link (a web page, mail or another app) outside the app: the system's browser or handler.</summary>
public interface ILinkLauncher
{
    /// <summary>True when something opened it; throws when the platform can't open that kind of link.</summary>
    Task<bool> OpenAsync(string uri);
}
