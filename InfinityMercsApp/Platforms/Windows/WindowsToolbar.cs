using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace InfinityMercsApp.WinUI;

/// <summary>
/// Hides WinUI's "more" (…) button on the app's toolbar, which the app never puts anything behind. MAUI's Windows
/// toolbar is a <see cref="CommandBar"/> made by <see cref="ToolbarHandler"/>, and MAUI never sets its overflow button,
/// so a mapping on that handler collapses it as each toolbar is made: no timers, no events, and nothing that runs
/// while the window closes.
/// </summary>
internal static class WindowsToolbar
{
    public static void HideOverflowButtons() =>
        ToolbarHandler.Mapper.AppendToMapping("HideOverflowButtons", (handler, _) => Collapse(handler.PlatformView));

    // The toolbar, and the command bar it holds for the page's commands.
    private static void Collapse(CommandBar toolbar)
    {
        toolbar.OverflowButtonVisibility = CommandBarOverflowButtonVisibility.Collapsed;
        CollapseWithin(toolbar.Content as UIElement);
    }

    private static void CollapseWithin(UIElement? element)
    {
        switch (element)
        {
            case CommandBar bar:
                bar.OverflowButtonVisibility = CommandBarOverflowButtonVisibility.Collapsed;
                CollapseWithin(bar.Content as UIElement);
                break;
            case Panel panel:
                foreach (var child in panel.Children)
                {
                    CollapseWithin(child);
                }

                break;
            case Border border:
                CollapseWithin(border.Child);
                break;
            case ContentControl control:
                CollapseWithin(control.Content as UIElement);
                break;
        }
    }
}
