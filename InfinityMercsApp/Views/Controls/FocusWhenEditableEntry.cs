using System.Runtime.CompilerServices;

namespace InfinityMercsApp.Views.Controls;

/// <summary>
/// An entry that takes the focus as soon as it stops being read-only, so editing can start straight away. It follows
/// <see cref="InputView.IsReadOnly"/> through <see cref="OnPropertyChanged"/> rather than an event.
/// </summary>
public sealed class FocusWhenEditableEntry : Entry
{
    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == IsReadOnlyProperty.PropertyName && !IsReadOnly)
        {
            Focus();
        }
    }
}
