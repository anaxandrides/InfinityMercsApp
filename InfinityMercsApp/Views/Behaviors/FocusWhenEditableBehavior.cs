namespace InfinityMercsApp.Views.Behaviors;

/// <summary>Focuses an entry as soon as it stops being read-only, so editing can start straight away.</summary>
public sealed class FocusWhenEditableBehavior : Behavior<Entry>
{
    protected override void OnAttachedTo(Entry entry)
    {
        base.OnAttachedTo(entry);
        entry.PropertyChanged += OnEntryPropertyChanged;
    }

    protected override void OnDetachingFrom(Entry entry)
    {
        entry.PropertyChanged -= OnEntryPropertyChanged;
        base.OnDetachingFrom(entry);
    }

    private static void OnEntryPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == InputView.IsReadOnlyProperty.PropertyName && sender is Entry { IsReadOnly: false } entry)
        {
            entry.Focus();
        }
    }
}
