using System.Runtime.CompilerServices;

namespace InfinityMercsApp.Views.Controls;

/// <summary>
/// A scroll view that stays at its end as its content grows, so newly added steps come into view. It follows its
/// content size through <see cref="OnPropertyChanged"/> rather than an event.
/// </summary>
public sealed class ScrollToEndView : ScrollView
{
    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName != ContentSizeProperty.PropertyName)
        {
            return;
        }

        var bottom = Math.Max(0, ContentSize.Height - Height);
        if (bottom > ScrollY)
        {
            Dispatcher.Dispatch(async () => await ScrollToAsync(0, bottom, animated: true));
        }
    }
}
