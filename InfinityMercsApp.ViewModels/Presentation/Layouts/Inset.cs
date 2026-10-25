namespace InfinityMercsApp.Presentation.Layouts;

/// <summary>Space inside a view's edges, in device-independent units; the app maps it to a MAUI Thickness.</summary>
public sealed record Inset(double Left, double Top, double Right, double Bottom)
{
    /// <summary>The same space at the left and right, and at the top and bottom.</summary>
    public static Inset Of(double horizontal, double vertical) => new(horizontal, vertical, horizontal, vertical);
}
