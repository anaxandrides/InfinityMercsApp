using System.Runtime.CompilerServices;

namespace InfinityMercsApp.Views.Controls;

/// <summary>
/// "Loading..." for a control whose data hasn't arrived yet. While it is shown it fades out and back in, so it
/// reads as working rather than stuck; bind <see cref="VisualElement.IsVisible"/> to the loading state. The pulse
/// runs only while the label is visible and in a window, which it follows through <see cref="OnPropertyChanged"/>
/// rather than events, so a page that has gone leaves no animation running.
/// </summary>
public sealed class LoadingLabel : Label
{
    private const string PulseAnimation = "LoadingPulse";
    private const uint PulseMilliseconds = 1400;

    public LoadingLabel()
    {
        Text = "Loading...";
        HorizontalOptions = LayoutOptions.Center;
        VerticalOptions = LayoutOptions.Center;
        HorizontalTextAlignment = TextAlignment.Center;
        Margin = new Thickness(12);
        SetDynamicResource(TextColorProperty, "TextMuted");
        SetDynamicResource(FontSizeProperty, "FontSizeSectionHeader");
    }

    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(IsVisible) || propertyName == nameof(Window))
        {
            UpdatePulse();
        }
    }

    private bool IsPulsing => IsVisible && Window is not null;

    private void UpdatePulse()
    {
        this.AbortAnimation(PulseAnimation);
        Opacity = 1;
        if (!IsPulsing)
        {
            return;
        }

        new Animation
        {
            { 0, 0.5, new Animation(opacity => Opacity = opacity, 1, 0.25, Easing.SinInOut) },
            { 0.5, 1, new Animation(opacity => Opacity = opacity, 0.25, 1, Easing.SinInOut) }
        }.Commit(this, PulseAnimation, length: PulseMilliseconds, repeat: () => IsPulsing);
    }
}
