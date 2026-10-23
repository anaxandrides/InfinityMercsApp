using System.Runtime.CompilerServices;
using InfinityMercsApp.Presentation.Layouts;

namespace InfinityMercsApp.Views.Adaptive;

/// <summary>
/// The app's window. It keeps the shared font sizes in step with its size: every size is set from
/// <see cref="FontScale"/> when the window opens and whenever it is resized, and every DynamicResource use follows.
/// It follows its own creation and size through overrides rather than events.
/// </summary>
public sealed class AppWindow(Page page) : Window(page)
{
    private double _appliedScale = -1;

    protected override void OnCreated()
    {
        base.OnCreated();
        ApplyFontScale();
    }

    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == WidthProperty.PropertyName || propertyName == HeightProperty.PropertyName)
        {
            ApplyFontScale();
        }
    }

#if WINDOWS
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        App.HideOverflowButtons(this);
    }
#endif

    private void ApplyFontScale()
    {
        var scale = FontScale.For(Width, Height);
        if (Math.Abs(scale - _appliedScale) < 0.01 || Application.Current?.Resources is not { } resources)
        {
            return;
        }

        _appliedScale = scale;
        foreach (var token in FontScale.Tokens)
        {
            resources[token.ResourceKey] = FontScale.SizeOf(token, scale);
        }
    }
}
