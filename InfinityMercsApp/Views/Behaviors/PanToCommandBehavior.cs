using System.Windows.Input;

namespace InfinityMercsApp.Views.Behaviors;

/// <summary>
/// Runs <see cref="Command"/> with the horizontal distance dragged since the last update, while the view is dragged,
/// e.g. to scroll the strip a tile sits in. A pan gesture has no command of its own, so the behaviour holds the
/// gesture and its one subscription, and drops both when it is detached.
/// </summary>
public sealed class PanToCommandBehavior : Behavior<View>
{
    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(PanToCommandBehavior));

    private readonly PanGestureRecognizer _pan = new();
    private double _lastTotalX;

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    protected override void OnAttachedTo(View view)
    {
        base.OnAttachedTo(view);
        _pan.PanUpdated += OnPanUpdated;
        view.GestureRecognizers.Add(_pan);
    }

    protected override void OnDetachingFrom(View view)
    {
        view.GestureRecognizers.Remove(_pan);
        _pan.PanUpdated -= OnPanUpdated;
        base.OnDetachingFrom(view);
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Running:
                var deltaX = e.TotalX - _lastTotalX;
                _lastTotalX = e.TotalX;
                if (Command?.CanExecute(deltaX) == true)
                {
                    Command.Execute(deltaX);
                }

                break;
            default:
                _lastTotalX = 0d;
                break;
        }
    }
}
