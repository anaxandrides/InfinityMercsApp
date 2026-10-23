using System.Runtime.CompilerServices;

namespace InfinityMercsApp.Views.Controls.FitText;

/// <summary>
/// A picker for a fit-text row (<see cref="FitTextFlexLayout"/>, <see cref="FitTextStackLayout"/>). MAUI doesn't
/// re-measure a picker when its choice changes, so a longer choice would never refit the row; this one asks to be
/// measured again, which arranges the row again, through <see cref="OnPropertyChanged"/> rather than an event.
/// </summary>
public sealed class FitTextPicker : Picker
{
    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == SelectedIndexProperty.PropertyName)
        {
            InvalidateMeasure();
        }
    }
}
