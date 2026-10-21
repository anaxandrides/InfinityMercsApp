namespace InfinityMercsApp.Views.Behaviors;

/// <summary>Keeps a scroll view at its end as its content grows, so newly added steps come into view.</summary>
public sealed class ScrollToEndBehavior : Behavior<ScrollView>
{
    protected override void OnAttachedTo(ScrollView scrollView)
    {
        base.OnAttachedTo(scrollView);
        scrollView.PropertyChanged += OnScrollViewPropertyChanged;
    }

    protected override void OnDetachingFrom(ScrollView scrollView)
    {
        scrollView.PropertyChanged -= OnScrollViewPropertyChanged;
        base.OnDetachingFrom(scrollView);
    }

    private static void OnScrollViewPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != ScrollView.ContentSizeProperty.PropertyName || sender is not ScrollView scrollView)
        {
            return;
        }

        var bottom = Math.Max(0, scrollView.ContentSize.Height - scrollView.Height);
        if (bottom > scrollView.ScrollY)
        {
            scrollView.Dispatcher.Dispatch(async () => await scrollView.ScrollToAsync(0, bottom, animated: true));
        }
    }
}
