namespace InfinityMercsApp.Views.Behaviors;

/// <summary>
/// Scrolls a list by dragging it with the mouse or a finger; desktop has no touch scrolling.
/// Set <see cref="Orientation"/> to Horizontal for a sideways strip.
/// </summary>
public sealed class DragToScrollBehavior : Behavior<ScrollView>
{
    private readonly PanGestureRecognizer _pan = new();
    private ScrollView? _scrollView;
    private double _start;

    public ScrollOrientation Orientation { get; set; } = ScrollOrientation.Vertical;

    protected override void OnAttachedTo(ScrollView scrollView)
    {
        base.OnAttachedTo(scrollView);
        _scrollView = scrollView;
        _pan.PanUpdated += OnPanUpdated;
        scrollView.GestureRecognizers.Add(_pan);
    }

    protected override void OnDetachingFrom(ScrollView scrollView)
    {
        scrollView.GestureRecognizers.Remove(_pan);
        _pan.PanUpdated -= OnPanUpdated;
        _scrollView = null;
        base.OnDetachingFrom(scrollView);
    }

    private async void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (_scrollView is null)
        {
            return;
        }

        var horizontal = Orientation == ScrollOrientation.Horizontal;
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _start = horizontal ? _scrollView.ScrollX : _scrollView.ScrollY;
                break;
            case GestureStatus.Running when horizontal:
                await _scrollView.ScrollToAsync(Math.Max(0, _start - e.TotalX), _scrollView.ScrollY, false);
                break;
            case GestureStatus.Running:
                await _scrollView.ScrollToAsync(_scrollView.ScrollX, Math.Max(0, _start - e.TotalY), false);
                break;
        }
    }
}
