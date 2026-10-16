namespace InfinityMercsApp.Views;

/// <summary>Styles from the app's resource dictionaries, for views built in code.</summary>
public static class AppStyles
{
    /// <summary>The style with this key; an empty label style when the app's resources don't have it.</summary>
    public static Style Named(string key) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Style style
            ? style
            : new Style(typeof(Label));
}
