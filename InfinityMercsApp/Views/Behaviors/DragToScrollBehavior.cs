namespace InfinityMercsApp.Views.Behaviors;

/// <summary>Scrolls a vertical list by dragging it with the mouse or a finger; desktop has no touch scrolling.</summary>
public sealed class DragToScrollBehavior : Behavior<ScrollView>
{
    private readonly PanGestureRecognizer _pan = new();
    private ScrollView? _scrollView;
    private double _startY;

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

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _startY = _scrollView.ScrollY;
                break;
            case GestureStatus.Running:
                await _scrollView.ScrollToAsync(0, Math.Max(0, _startY - e.TotalY), false);
                break;
        }
    }
}
