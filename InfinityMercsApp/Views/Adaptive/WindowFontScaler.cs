using InfinityMercsApp.Presentation.Layouts;
namespace InfinityMercsApp.Views.Adaptive;

/// <summary>
/// Keeps the app's shared font sizes in step with the window: every size is set from <see cref="FontScale"/> when
/// the window opens and whenever it is resized, and every DynamicResource use follows.
/// </summary>
public static class WindowFontScaler
{
    /// <summary>Starts following the window's size.</summary>
    public static void Follow(Window window)
    {
        double appliedScale = -1;
        void Apply()
        {
            var scale = FontScale.For(window.Width, window.Height);
            if (Math.Abs(scale - appliedScale) < 0.01 || Application.Current?.Resources is not { } resources)
            {
                return;
            }

            appliedScale = scale;
            foreach (var token in FontScale.Tokens)
            {
                resources[token.ResourceKey] = FontScale.SizeOf(token, scale);
            }
        }

        window.SizeChanged += (_, _) => Apply();
        window.Created += (_, _) => Apply();
    }
}
